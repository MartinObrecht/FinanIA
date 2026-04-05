using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FluentAssertions;

namespace FinanIA.Domain.Tests.Entities;

public class TransactionTests
{
    // ── Create ────────────────────────────────────────────────────────────
    [Fact]
    public void Create_WithIncome_SetsAllFieldsCorrectly()
    {
        var userId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 31);

        var transaction = Transaction.Create(userId, "Salário", 5000m, date, TransactionType.Income);

        transaction.UserId.Should().Be(userId);
        transaction.Description.Should().Be("Salário");
        transaction.Amount.Should().Be(5000m);
        transaction.Date.Should().Be(date);
        transaction.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public void Create_WithExpense_SetsTypeCorrectly()
    {
        var transaction = Transaction.Create(Guid.NewGuid(), "Aluguel", 1500m,
            new DateOnly(2026, 3, 1), TransactionType.Expense);

        transaction.Type.Should().Be(TransactionType.Expense);
        transaction.Amount.Should().Be(1500m);
    }

    [Fact]
    public void Create_GeneratesNonEmptyGuid()
    {
        var transaction = Transaction.Create(Guid.NewGuid(), "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        transaction.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_PreservesUserId()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        transaction.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_SetsCreatedAtInUtc()
    {
        var before = DateTime.UtcNow;
        var transaction = Transaction.Create(Guid.NewGuid(), "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        var after = DateTime.UtcNow;

        transaction.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        transaction.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_SetsCorrectTypeAndAmount()
    {
        var transaction = Transaction.Create(Guid.NewGuid(), "Freelance", 2500.50m,
            new DateOnly(2026, 4, 1), TransactionType.Income);

        transaction.Type.Should().Be(TransactionType.Income);
        transaction.Amount.Should().Be(2500.50m);
    }

    // ── Update ───────────────────────────────────────────────────────────
    [Fact]
    public void Update_SetsAllFieldsCorrectly()
    {
        var transaction = Transaction.Create(Guid.NewGuid(), "Original", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        var newDate = new DateOnly(2026, 6, 15);

        transaction.Update("Updated desc", 999m, newDate, TransactionType.Expense);

        transaction.Description.Should().Be("Updated desc");
        transaction.Amount.Should().Be(999m);
        transaction.Date.Should().Be(newDate);
        transaction.Type.Should().Be(TransactionType.Expense);
    }

    [Fact]
    public void Update_PreservesCreatedAt()
    {
        var transaction = Transaction.Create(Guid.NewGuid(), "Original", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        var originalCreatedAt = transaction.CreatedAt;

        transaction.Update("Changed", 200m, new DateOnly(2026, 2, 2), TransactionType.Expense);

        transaction.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Update_PreservesIdAndUserId()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Original", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        var originalId = transaction.Id;

        transaction.Update("Changed", 200m, new DateOnly(2026, 2, 2), TransactionType.Expense);

        transaction.Id.Should().Be(originalId);
        transaction.UserId.Should().Be(userId);
    }
}
