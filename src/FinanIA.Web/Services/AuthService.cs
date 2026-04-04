using System.Net.Http.Headers;
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
            return (false, "Falha no registro. Verifique os dados e tente novamente.");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
            return (false, "Resposta inválida do servidor.");

        await StoreTokensAsync(authResponse);
        _navigationManager.NavigateTo("/");
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string email, string password)
    {
        var request = new { email, password };
        var response = await _http.PostAsJsonAsync("api/auth/login", request);

        if (!response.IsSuccessStatusCode)
            return (false, "E-mail ou senha incorretos.");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
            return (false, "Resposta inválida do servidor.");

        await StoreTokensAsync(authResponse);
        _navigationManager.NavigateTo("/");
        return (true, null);
    }

    public async Task<bool> RefreshAsync()
    {
        var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(_accessToken))
            return false;

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken) }
        };

        var response = await _http.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode)
        {
            await ClearTokensAsync();
            return false;
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
        {
            await ClearTokensAsync();
            return false;
        }

        await StoreTokensAsync(authResponse);
        return true;
    }

    public async Task LogoutAsync()
    {
        await ClearTokensAsync();
        _navigationManager.NavigateTo("/login");
    }

    private async Task StoreTokensAsync(AuthTokenResponse authResponse)
    {
        _accessToken = authResponse.AccessToken;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", authResponse.RefreshToken);
    }

    private async Task ClearTokensAsync()
    {
        _accessToken = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
    }
}
