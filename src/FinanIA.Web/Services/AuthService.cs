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

    public event Action? OnTokenChanged;

    public AuthService(HttpClient http, IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _http = http;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
    }

    public async Task InitializeAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "accessToken");
        if (!string.IsNullOrEmpty(token))
        {
            _accessToken = token;
            OnTokenChanged?.Invoke();
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(string email, string password, string? returnUrl = null)
    {
        var request = new { email, password };
        var response = await _http.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
            return (false, "Falha no registro. Verifique os dados e tente novamente.");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
            return (false, "Resposta inválida do servidor.");

        await StoreTokensAsync(authResponse);
        _navigationManager.NavigateTo(returnUrl ?? "/");
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string email, string password, string? returnUrl = null)
    {
        var request = new { email, password };
        var response = await _http.PostAsJsonAsync("api/auth/login", request);

        if (!response.IsSuccessStatusCode)
            return (false, "E-mail ou senha incorretos.");

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (authResponse is null)
            return (false, "Resposta inválida do servidor.");

        await StoreTokensAsync(authResponse);
        _navigationManager.NavigateTo(returnUrl ?? "/");
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
        if (!string.IsNullOrEmpty(_accessToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            // Best-effort: ignore server errors — tokens are cleared locally regardless
            await _http.SendAsync(request);
        }

        await ClearTokensAsync();
        _navigationManager.NavigateTo("/login");
    }

    private async Task StoreTokensAsync(AuthTokenResponse authResponse)
    {
        _accessToken = authResponse.AccessToken;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", authResponse.RefreshToken);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "accessToken", authResponse.AccessToken);
        OnTokenChanged?.Invoke();
    }

    private async Task ClearTokensAsync()
    {
        _accessToken = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "accessToken");
        OnTokenChanged?.Invoke();
    }
}
