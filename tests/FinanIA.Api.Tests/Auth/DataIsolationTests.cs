using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FluentAssertions;

namespace FinanIA.Api.Tests.Auth;

public class DataIsolationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DataIsolationTests(CustomWebApplicationFactory factory)
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

    // ── data isolation tests ─────────────────────────────────────────────────

    [Fact]
    public async Task UserB_CannotUseUserA_RefreshToken_WithOwnAccessToken()
    {
        // Arrange — register two independent users
        var emailA = $"usera_{Guid.NewGuid()}@test.com";
        var emailB = $"userb_{Guid.NewGuid()}@test.com";

        var tokensA = await RegisterUserAsync(emailA);
        var tokensB = await RegisterUserAsync(emailB);

        // Act — user B sends their own access token but user A's refresh token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokensA.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        // Assert — must be rejected because tokenA belongs to user A, not user B
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserA_CannotUseUserB_RefreshToken_WithOwnAccessToken()
    {
        // Arrange
        var emailA = $"iso_a_{Guid.NewGuid()}@test.com";
        var emailB = $"iso_b_{Guid.NewGuid()}@test.com";

        var tokensA = await RegisterUserAsync(emailA);
        var tokensB = await RegisterUserAsync(emailB);

        // Act — user A sends their own access token but user B's refresh token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokensB.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensA.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserA_CanRefresh_WithOwnTokens()
    {
        // Sanity check — user A can refresh with their own valid tokens
        var email = $"own_refresh_{Guid.NewGuid()}@test.com";
        var tokens = await RegisterUserAsync(email);

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
    }

    [Fact]
    public async Task BothUsers_CanLoginAndRefresh_Independently()
    {
        // Two completely independent users can each register, login, and refresh
        var emailA = $"ind_a_{Guid.NewGuid()}@test.com";
        var emailB = $"ind_b_{Guid.NewGuid()}@test.com";

        var tokensA = await RegisterUserAsync(emailA);
        var tokensB = await RegisterUserAsync(emailB);

        // User A refreshes
        var requestA = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokensA.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensA.AccessToken) }
        };
        var responseA = await _client.SendAsync(requestA);

        // User B refreshes
        var requestB = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = tokensB.RefreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };
        var responseB = await _client.SendAsync(requestB);

        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
