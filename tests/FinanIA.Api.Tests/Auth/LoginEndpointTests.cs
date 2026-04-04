using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FluentAssertions;

namespace FinanIA.Api.Tests.Auth;

public class LoginEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AuthTokenDto> RegisterUserAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    // ── POST /api/auth/login ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        await RegisterUserAsync(email, "password123");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrongpw_{Guid.NewGuid()}@test.com";
        await RegisterUserAsync(email, "correct-password");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@test.com", password = "password123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/auth/refresh ───────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var email = $"refresh_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email, "password123");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokens.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(tokens.RefreshToken, "token should have been rotated");
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var email = $"badrefresh_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email, "password123");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = "invalid-token-value" }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_MissingAuthorizationHeader_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = "some-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401()
    {
        var email = $"expired_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email, "password123");

        // Use a valid token once to rotate it (making the original "expired/revoked" from the DB perspective)
        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokens.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };
        await _client.SendAsync(firstRequest);

        // Try to reuse the already-rotated refresh token
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokens.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };
        var response = await _client.SendAsync(secondRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
