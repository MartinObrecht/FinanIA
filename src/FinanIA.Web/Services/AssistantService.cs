using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanIA.Web.Services;

public record ConversationTurn(string Role, string Content);

public record AssistantResponse(string Reply);

public class AssistantService
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;

    public AssistantService(HttpClient http, AuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<(AssistantResponse? Result, string? Error)> AskAsync(
        string message,
        IReadOnlyList<ConversationTurn> history,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/assistant/ask");
        if (!string.IsNullOrEmpty(_authService.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.AccessToken);

        request.Content = JsonContent.Create(new
        {
            message,
            previousMessages = history.Select(t => new { role = t.Role, content = t.Content }).ToList()
        });

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return (null, "Não foi possível obter resposta do assistente. Tente novamente.");

        var result = await response.Content.ReadFromJsonAsync<AssistantResponse>(ct);
        return (result, null);
    }
}
