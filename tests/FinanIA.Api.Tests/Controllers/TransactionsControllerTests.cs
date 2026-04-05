using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanIA.Api.Tests.Helpers;
using FluentAssertions;

namespace FinanIA.Api.Tests.Controllers;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AuthTokenDto> RegisterUserAsync(string? email = null, string password = "password123")
    {
        email ??= $"user_{Guid.NewGuid()}@test.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    private async Task<TransactionDto> CreateTransactionAsync(
        string token,
        string description = "Salário de março",
        decimal amount = 5000m,
        string date = "2026-03-31",
        string type = "Income")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new { description, amount, date, type }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
    }

    // ── POST /api/transactions ────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithTransactionBody()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "Salário de março",
                amount = 5000.00m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TransactionDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Description.Should().Be("Salário de março");
        body.Amount.Should().Be(5000.00m);
        body.Type.Should().Be("Income");
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            description = "Salário",
            amount = 1000m,
            date = "2026-03-31",
            type = "Income"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ZeroAmount_Returns400WithProblemDetails()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "Test",
                amount = 0m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("Amount");
    }

    [Fact]
    public async Task Create_NegativeAmount_Returns400WithProblemDetails()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "Test",
                amount = -100m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("Amount");
    }

    [Fact]
    public async Task Create_EmptyDescription_Returns400WithProblemDetails()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "",
                amount = 1000m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("Description");
    }

    // ── GET /api/transactions ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_EmptyList_Returns200WithEmptyArray()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        body.Should().NotBeNull().And.BeEmpty();
    }

    // ── PUT /api/transactions/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/api/transactions/{Guid.NewGuid()}", new
        {
            description = "Updated",
            amount = 200m,
            date = "2026-03-31",
            type = "Expense"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_TransactionOfOtherUser_Returns404Opaque()
    {
        var tokensA = await RegisterUserAsync();
        var tokensB = await RegisterUserAsync();

        // User A creates a transaction
        var tx = await CreateTransactionAsync(tokensA.AccessToken);

        // User B tries to update User A's transaction
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/transactions/{tx.Id}")
        {
            Content = JsonContent.Create(new
            {
                description = "Attacked!",
                amount = 1m,
                date = "2026-03-31",
                type = "Expense"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/transactions/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new
            {
                description = "Updated",
                amount = 200m,
                date = "2026-03-31",
                type = "Expense"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_InvalidAmount_Returns400WithProblemDetails()
    {
        var tokens = await RegisterUserAsync();
        var tx = await CreateTransactionAsync(tokens.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/transactions/{tx.Id}")
        {
            Content = JsonContent.Create(new
            {
                description = "Updated",
                amount = 0m,
                date = "2026-03-31",
                type = "Expense"
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        body.Should().NotBeNull();
        body!.Errors.Should().ContainKey("Amount");
    }

    // ── DELETE /api/transactions/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_TransactionOfOtherUser_Returns404Opaque()
    {
        var tokensA = await RegisterUserAsync();
        var tokensB = await RegisterUserAsync();

        var tx = await CreateTransactionAsync(tokensA.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/transactions/{tx.Id}")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/transactions/{Guid.NewGuid()}")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/balance ──────────────────────────────────────────────────────

    [Fact]
    public async Task Balance_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/balance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Balance_NoTransactions_ReturnsZeros()
    {
        var tokens = await RegisterUserAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/balance")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken) }
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BalanceDto>();
        body.Should().NotBeNull();
        body!.TotalIncome.Should().Be(0m);
        body.TotalExpense.Should().Be(0m);
        body.Balance.Should().Be(0m);
    }

    // ── Full POST → GET → PUT → DELETE flow ──────────────────────────────────

    [Fact]
    public async Task FullCrudFlow_CreatesUpdatesRetrievesAndDeletes()
    {
        var tokens = await RegisterUserAsync();
        var authHeader = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // POST — create income
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "Salário",
                amount = 5000m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = authHeader }
        };
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<TransactionDto>())!;
        created.Id.Should().NotBeEmpty();

        // POST — create expense
        var createExpenseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(new
            {
                description = "Aluguel",
                amount = 1500m,
                date = "2026-04-01",
                type = "Expense"
            }),
            Headers = { Authorization = authHeader }
        };
        var createExpenseResponse = await _client.SendAsync(createExpenseRequest);
        createExpenseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // GET — list returns both transactions
        var getListRequest = new HttpRequestMessage(HttpMethod.Get, "/api/transactions")
        {
            Headers = { Authorization = authHeader }
        };
        var getListResponse = await _client.SendAsync(getListRequest);
        getListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await getListResponse.Content.ReadFromJsonAsync<List<TransactionDto>>())!;
        list.Should().HaveCount(2);

        // GET /api/balance — verifies correct calculation
        var balanceRequest = new HttpRequestMessage(HttpMethod.Get, "/api/balance")
        {
            Headers = { Authorization = authHeader }
        };
        var balanceResponse = await _client.SendAsync(balanceRequest);
        balanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = (await balanceResponse.Content.ReadFromJsonAsync<BalanceDto>())!;
        balance.TotalIncome.Should().Be(5000m);
        balance.TotalExpense.Should().Be(1500m);
        balance.Balance.Should().Be(3500m);

        // PUT — update the income transaction
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/transactions/{created.Id}")
        {
            Content = JsonContent.Create(new
            {
                description = "Salário atualizado",
                amount = 6000m,
                date = "2026-03-31",
                type = "Income"
            }),
            Headers = { Authorization = authHeader }
        };
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<TransactionDto>())!;
        updated.Description.Should().Be("Salário atualizado");
        updated.Amount.Should().Be(6000m);

        // DELETE — remove the income transaction
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/transactions/{created.Id}")
        {
            Headers = { Authorization = authHeader }
        };
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET — list now shows only 1 transaction
        var finalListRequest = new HttpRequestMessage(HttpMethod.Get, "/api/transactions")
        {
            Headers = { Authorization = authHeader }
        };
        var finalListResponse = await _client.SendAsync(finalListRequest);
        var finalList = (await finalListResponse.Content.ReadFromJsonAsync<List<TransactionDto>>())!;
        finalList.Should().HaveCount(1);
        finalList[0].Description.Should().Be("Aluguel");

        // GET /api/balance — balance recalculated after delete
        var finalBalanceRequest = new HttpRequestMessage(HttpMethod.Get, "/api/balance")
        {
            Headers = { Authorization = authHeader }
        };
        var finalBalanceResponse = await _client.SendAsync(finalBalanceRequest);
        var finalBalance = (await finalBalanceResponse.Content.ReadFromJsonAsync<BalanceDto>())!;
        finalBalance.TotalIncome.Should().Be(0m);
        finalBalance.TotalExpense.Should().Be(1500m);
        finalBalance.Balance.Should().Be(-1500m);
    }

    [Fact]
    public async Task DataIsolation_UserBCannotSeeUserATransactions()
    {
        var tokensA = await RegisterUserAsync();
        var tokensB = await RegisterUserAsync();

        // User A creates a transaction
        await CreateTransactionAsync(tokensA.AccessToken, "Salário", 5000m);

        // User B lists transactions — should be empty
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await response.Content.ReadFromJsonAsync<List<TransactionDto>>())!;
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task DataIsolation_UserBBalanceUnaffectedByUserATransactions()
    {
        var tokensA = await RegisterUserAsync();
        var tokensB = await RegisterUserAsync();

        // User A creates transactions
        await CreateTransactionAsync(tokensA.AccessToken, "Salário", 5000m, type: "Income");
        await CreateTransactionAsync(tokensA.AccessToken, "Aluguel", 1500m, type: "Expense");

        // User B balance should still be zero
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/balance")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", tokensB.AccessToken) }
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = (await response.Content.ReadFromJsonAsync<BalanceDto>())!;
        balance.TotalIncome.Should().Be(0m);
        balance.TotalExpense.Should().Be(0m);
        balance.Balance.Should().Be(0m);
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record AuthTokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    private sealed record TransactionDto(
        Guid Id,
        string Description,
        decimal Amount,
        string Date,
        string Type,
        DateTime CreatedAt);

    private sealed record BalanceDto(decimal TotalIncome, decimal TotalExpense, decimal Balance);

    private sealed record ValidationProblemDetailsDto(
        string? Type,
        string? Title,
        int Status,
        Dictionary<string, string[]> Errors);
}
