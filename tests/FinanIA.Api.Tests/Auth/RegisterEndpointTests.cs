using System.Net;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FluentAssertions;

namespace FinanIA.Api.Tests.Auth;

public class RegisterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegisterEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201WithTokens()
    {
        var request = new { email = $"user_{Guid.NewGuid()}@test.com", password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"dup_{Guid.NewGuid()}@test.com";
        var request = new { email, password = "password123" };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var request = new { email = $"short_{Guid.NewGuid()}@test.com", password = "abc" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var request = new { email = "not-an-email", password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
