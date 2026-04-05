using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FinanIA.Infrastructure.Persistence;
using FinanIA.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinanIA.Infrastructure.Tests.Repositories;

public class TransactionRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TransactionRepository _repository;

    public TransactionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new TransactionRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── AddAsync + GetByIdAsync ───────────────────────────────────────────
    [Fact]
    public async Task AddAsync_PersistsTransactionInDatabase()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Salário", 5000m,
            new DateOnly(2026, 3, 31), TransactionType.Income);

        await _repository.AddAsync(transaction);

        var found = await _repository.GetByIdAsync(transaction.Id, userId);
        found.Should().NotBeNull();
        found!.Description.Should().Be("Salário");
        found.Amount.Should().Be(5000m);
        found.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUserId_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var transaction = Transaction.Create(ownerId, "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        await _repository.AddAsync(transaction);

        var differentUserId = Guid.NewGuid();
        var result = await _repository.GetByIdAsync(transaction.Id, differentUserId);

        result.Should().BeNull();
    }

    // ── GetAllByUserIdAsync ───────────────────────────────────────────────
    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsOnlyUserTransactions()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await _repository.AddAsync(Transaction.Create(userA, "A1", 100m, new DateOnly(2026, 1, 1), TransactionType.Income));
        await _repository.AddAsync(Transaction.Create(userA, "A2", 200m, new DateOnly(2026, 2, 1), TransactionType.Expense));
        await _repository.AddAsync(Transaction.Create(userB, "B1", 300m, new DateOnly(2026, 1, 1), TransactionType.Income));

        var result = await _repository.GetAllByUserIdAsync(userA);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.UserId.Should().Be(userA));
    }

    [Fact]
    public async Task GetAllByUserIdAsync_EmptyUser_ReturnsEmptyList()
    {
        var result = await _repository.GetAllByUserIdAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsOrderedByDateDescThenCreatedAtDesc()
    {
        var userId = Guid.NewGuid();

        var older = Transaction.Create(userId, "Older", 100m, new DateOnly(2026, 1, 1), TransactionType.Income);
        await Task.Delay(5); // ensure different CreatedAt
        var recent = Transaction.Create(userId, "Recent", 200m, new DateOnly(2026, 4, 1), TransactionType.Income);

        await _repository.AddAsync(older);
        await _repository.AddAsync(recent);

        var result = await _repository.GetAllByUserIdAsync(userId);

        result[0].Description.Should().Be("Recent");
        result[1].Description.Should().Be("Older");
    }

    // ── GetBalanceSummaryAsync ────────────────────────────────────────────
    [Fact]
    public async Task GetBalanceSummaryAsync_NoTransactions_ReturnsZeros()
    {
        var summary = await _repository.GetBalanceSummaryAsync(Guid.NewGuid());

        summary.TotalIncome.Should().Be(0m);
        summary.TotalExpense.Should().Be(0m);
        summary.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceSummaryAsync_SumsIncomeCorrectly()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(Transaction.Create(userId, "S1", 3000m, new DateOnly(2026, 1, 1), TransactionType.Income));
        await _repository.AddAsync(Transaction.Create(userId, "S2", 2000m, new DateOnly(2026, 2, 1), TransactionType.Income));

        var summary = await _repository.GetBalanceSummaryAsync(userId);

        summary.TotalIncome.Should().Be(5000m);
        summary.TotalExpense.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceSummaryAsync_SumsExpensesCorrectly()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(Transaction.Create(userId, "E1", 1000m, new DateOnly(2026, 1, 1), TransactionType.Expense));
        await _repository.AddAsync(Transaction.Create(userId, "E2", 500m, new DateOnly(2026, 2, 1), TransactionType.Expense));

        var summary = await _repository.GetBalanceSummaryAsync(userId);

        summary.TotalExpense.Should().Be(1500m);
        summary.TotalIncome.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceSummaryAsync_CalculatesBalanceCorrectly()
    {
        var userId = Guid.NewGuid();
        await _repository.AddAsync(Transaction.Create(userId, "Income", 5000m, new DateOnly(2026, 1, 1), TransactionType.Income));
        await _repository.AddAsync(Transaction.Create(userId, "Expense", 1500m, new DateOnly(2026, 1, 1), TransactionType.Expense));

        var summary = await _repository.GetBalanceSummaryAsync(userId);

        summary.Balance.Should().Be(3500m);
    }

    [Fact]
    public async Task GetBalanceSummaryAsync_IsolatesByUserId()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await _repository.AddAsync(Transaction.Create(userA, "A income", 5000m, new DateOnly(2026, 1, 1), TransactionType.Income));
        await _repository.AddAsync(Transaction.Create(userB, "B income", 9999m, new DateOnly(2026, 1, 1), TransactionType.Income));

        var summaryA = await _repository.GetBalanceSummaryAsync(userA);

        summaryA.TotalIncome.Should().Be(5000m);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_PersistsChangesCorrectly()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Original", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        await _repository.AddAsync(transaction);

        transaction.Update("Updated", 999m, new DateOnly(2026, 6, 15), TransactionType.Expense);
        await _repository.UpdateAsync(transaction);

        var updated = await _repository.GetByIdAsync(transaction.Id, userId);
        updated!.Description.Should().Be("Updated");
        updated.Amount.Should().Be(999m);
        updated.Type.Should().Be(TransactionType.Expense);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task DeleteAsync_RemovesTransactionFromDatabase()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "To delete", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        await _repository.AddAsync(transaction);

        await _repository.DeleteAsync(transaction);

        var result = await _repository.GetByIdAsync(transaction.Id, userId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_BalanceRecalculatesAfterDeletion()
    {
        var userId = Guid.NewGuid();
        var income = Transaction.Create(userId, "Income", 5000m, new DateOnly(2026, 1, 1), TransactionType.Income);
        var expense = Transaction.Create(userId, "Expense", 1000m, new DateOnly(2026, 1, 1), TransactionType.Expense);
        await _repository.AddAsync(income);
        await _repository.AddAsync(expense);

        await _repository.DeleteAsync(expense);

        var summary = await _repository.GetBalanceSummaryAsync(userId);
        summary.TotalExpense.Should().Be(0m);
        summary.Balance.Should().Be(5000m);
    }
}
