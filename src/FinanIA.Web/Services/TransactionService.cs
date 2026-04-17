using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FinanIA.Web.Services;

public record TransactionResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    string Type,
    DateTime CreatedAt);

public record BalanceResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);

public record TransactionFormModel
{
    public Guid? Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? Date { get; set; }
    public string Type { get; set; } = "Income";
}

public class TransactionService
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;

    public TransactionService(HttpClient http, AuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<(TransactionResponse? Result, string? Error)> CreateTransactionAsync(
        string description, decimal amount, DateOnly date, string type)
    {
        var request = BuildRequest(HttpMethod.Post, "api/transactions");
        request.Content = JsonContent.Create(new { description, amount, date, type });

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return (null, "Falha ao registrar transação.");

        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        return (result, null);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync()
    {
        var request = BuildRequest(HttpMethod.Get, "api/transactions");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<TransactionResponse>>() ?? [];
    }

    public async Task<BalanceResponse?> GetBalanceAsync()
    {
        var request = BuildRequest(HttpMethod.Get, "api/balance");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<BalanceResponse>();
    }

    public async Task<(TransactionResponse? Result, string? Error)> UpdateTransactionAsync(
        Guid id, string description, decimal amount, DateOnly date, string type)
    {
        var request = BuildRequest(HttpMethod.Put, $"api/transactions/{id}");
        request.Content = JsonContent.Create(new { description, amount, date, type });

        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (null, "Transação não encontrada.");
        if (!response.IsSuccessStatusCode)
            return (null, "Falha ao atualizar transação.");

        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        return (result, null);
    }

    public async Task<(bool Success, string? Error)> DeleteTransactionAsync(Guid id)
    {
        var request = BuildRequest(HttpMethod.Delete, $"api/transactions/{id}");
        var response = await _http.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (false, "Transação não encontrada.");
        if (!response.IsSuccessStatusCode)
            return (false, "Falha ao excluir transação.");

        return (true, null);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_authService.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.AccessToken);
        return request;
    }
}
