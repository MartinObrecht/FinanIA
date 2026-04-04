using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FinanIA.Web.Services;

internal record AuthTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;

    private string? _accessToken;

    public string? AccessToken => _accessToken;

    public AuthService(HttpClient http, IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _http = http;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(string email, string password)
    {
        var request = new { email, password };
        var response = await _http.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
        {
            return (false, "Falha no registro. Verifique os dados e tente novamente.");
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
            return (false, "Resposta inválida do servidor.");

        _accessToken = authResponse.AccessToken;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", authResponse.RefreshToken);

        _navigationManager.NavigateTo("/");
        return (true, null);
    }
}
