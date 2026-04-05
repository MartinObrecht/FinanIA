using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FinanIA.Domain.Interfaces;
using FinanIA.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinanIA.Api.Tests.Controllers;

public class AssistantControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AssistantControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AuthTokenDto> RegisterUserAsync(HttpClient client, string? email = null)
    {
        email ??= $"user_{Guid.NewGuid()}@test.com";
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "password123" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    private HttpClient CreateClientWithAssistant(IFinancialAssistant assistant) =>
        _factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(
                services => services.AddScoped(_ => assistant)))
            .CreateClient();

    // ── POST /api/assistant/ask — 200 ────────────────────────────────────────

    [Fact]
    public async Task Ask_ValidRequest_Returns200WithReplyField()
    {
        const string expectedReply = "Saldo: R$ 1.000,00.\n\nEsta resposta não substitui aconselhamento financeiro profissional.";
        var mockAssistant = Substitute.For<IFinancialAssistant>();
        mockAssistant
            .AskAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ConversationTurnDto>>(), Arg.Any<CancellationToken>())
            .Returns(expectedReply);

        var client = CreateClientWithAssistant(mockAssistant);
        var tokens = await RegisterUserAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/ask")
        {
            Content = JsonContent.Create(new
            {
                message = "Qual é o meu saldo?",
                previousMessages = Array.Empty<object>()
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AssistantResponseDto>();
        body.Should().NotBeNull();
        body!.Reply.Should().Be(expectedReply);
    }

    // ── POST /api/assistant/ask — 400 ────────────────────────────────────────

    [Fact]
    public async Task Ask_EmptyMessage_Returns400()
    {
        var mockAssistant = Substitute.For<IFinancialAssistant>();
        var client = CreateClientWithAssistant(mockAssistant);
        var tokens = await RegisterUserAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/ask")
        {
            Content = JsonContent.Create(new
            {
                message = "",
                previousMessages = Array.Empty<object>()
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ask_WhitespaceOnlyMessage_Returns400()
    {
        var mockAssistant = Substitute.For<IFinancialAssistant>();
        var client = CreateClientWithAssistant(mockAssistant);
        var tokens = await RegisterUserAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/ask")
        {
            Content = JsonContent.Create(new
            {
                message = "   ",
                previousMessages = Array.Empty<object>()
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/assistant/ask — 401 ────────────────────────────────────────

    [Fact]
    public async Task Ask_MissingJwt_Returns401()
    {
        var mockAssistant = Substitute.For<IFinancialAssistant>();
        var client = CreateClientWithAssistant(mockAssistant);

        var response = await client.PostAsJsonAsync("/api/assistant/ask", new
        {
            message = "Qual é o meu saldo?",
            previousMessages = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/assistant/ask — 503 ────────────────────────────────────────

    [Fact]
    public async Task Ask_AssistantThrows_Returns503WithoutStackTrace()
    {
        var mockAssistant = Substitute.For<IFinancialAssistant>();
        mockAssistant
            .AskAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ConversationTurnDto>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Internal AI provider error"));

        var client = CreateClientWithAssistant(mockAssistant);
        var tokens = await RegisterUserAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/ask")
        {
            Content = JsonContent.Create(new
            {
                message = "Qual é o meu saldo?",
                previousMessages = Array.Empty<object>()
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("InvalidOperationException");
        body.Should().NotContain("at ");  // no stack trace
        body.Should().NotContain("Internal AI provider error");
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    private sealed record AssistantResponseDto(string Reply);
}
