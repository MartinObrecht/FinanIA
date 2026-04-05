using System.Security.Claims;
using System.Text.Json;
using FinanIA.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace FinanIA.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public CustomAuthStateProvider(AuthService authService)
    {
        _authService = authService;
        _authService.OnTokenChanged += NotifyAuthStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = _authService.AccessToken;

        if (string.IsNullOrEmpty(accessToken))
            return Task.FromResult(AnonymousState());

        try
        {
            var claims = ParseClaimsFromJwt(accessToken);

            // Check expiry claim
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim is not null && long.TryParse(expClaim.Value, out var exp))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                if (expiry < DateTime.UtcNow)
                    return Task.FromResult(AnonymousState());
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
        catch
        {
            return Task.FromResult(AnonymousState());
        }
    }

    public void NotifyAuthStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static AuthenticationState AnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return [];

        var payload = parts[1];
        // Pad base64url to standard base64
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };

        var bytes = Convert.FromBase64String(payload);
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes);
        if (json is null)
            return [];

        return json.Select(kvp => new Claim(kvp.Key, kvp.Value.ValueKind == JsonValueKind.String
            ? kvp.Value.GetString()!
            : kvp.Value.ToString()));
    }
}

