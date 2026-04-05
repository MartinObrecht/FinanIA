using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FluentAssertions;

namespace FinanIA.Api.Tests.Auth;

public class LogoutEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LogoutEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AuthTokenDto> RegisterUserAsync(string email, string password = "password123")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    private HttpRequestMessage BuildAuthorizedRequest(HttpMethod method, string url, object? body, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithValidBearerToken_Returns204()
    {
        var email = $"logout_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email);

        var request = BuildAuthorizedRequest(HttpMethod.Post, "/api/auth/logout", null, tokens.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutBearerToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_AfterLogout_RefreshTokenReturns401()
    {
        var email = $"logout_revoke_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email);

        // Perform logout
        var logoutRequest = BuildAuthorizedRequest(HttpMethod.Post, "/api/auth/logout", null, tokens.AccessToken);
        var logoutResponse = await _client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Attempt to use the refresh token after logout — should be revoked
        var refreshRequest = BuildAuthorizedRequest(
            HttpMethod.Post,
            "/api/auth/refresh",
            new { refreshToken = tokens.RefreshToken },
            tokens.AccessToken);

        var refreshResponse = await _client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── helper record ─────────────────────────────────────────────────────────

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
