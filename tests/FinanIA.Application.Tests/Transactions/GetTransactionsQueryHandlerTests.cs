using FinanIA.Application.Transactions.Queries;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace FinanIA.Application.Tests.Transactions;

public class GetTransactionsQueryHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly GetTransactionsQueryHandler _handler;

    public GetTransactionsQueryHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _handler = new GetTransactionsQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllTransactionsForUser()
    {
        var userId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            Transaction.Create(userId, "Salário", 5000m, new DateOnly(2026, 3, 31), TransactionType.Income),
            Transaction.Create(userId, "Aluguel", 1500m, new DateOnly(2026, 3, 5), TransactionType.Expense)
        };
        _repository.GetAllByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Transaction>)transactions);

        var result = await _handler.HandleAsync(new GetTransactionsQuery(userId));

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Description == "Salário");
        result.Should().Contain(t => t.Description == "Aluguel");
    }

    [Fact]
    public async Task HandleAsync_NoTransactions_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _repository.GetAllByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Transaction>)new List<Transaction>());

        var result = await _handler.HandleAsync(new GetTransactionsQuery(userId));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PreservesOrderFromRepository()
    {
        var userId = Guid.NewGuid();
        // Repository returns in descending date order (as required)
        var t1 = Transaction.Create(userId, "Recent", 100m, new DateOnly(2026, 4, 1), TransactionType.Income);
        var t2 = Transaction.Create(userId, "Older", 200m, new DateOnly(2026, 3, 1), TransactionType.Expense);
        _repository.GetAllByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Transaction>)new List<Transaction> { t1, t2 });

        var result = await _handler.HandleAsync(new GetTransactionsQuery(userId));

        result[0].Description.Should().Be("Recent");
        result[1].Description.Should().Be("Older");
    }

    [Fact]
    public async Task HandleAsync_IsolatesUserData()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var transactionA = Transaction.Create(userA, "UserA tx", 100m, new DateOnly(2026, 1, 1), TransactionType.Income);
        _repository.GetAllByUserIdAsync(userA, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Transaction>)new List<Transaction> { transactionA });
        _repository.GetAllByUserIdAsync(userB, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Transaction>)new List<Transaction>());

        var resultA = await _handler.HandleAsync(new GetTransactionsQuery(userA));
        var resultB = await _handler.HandleAsync(new GetTransactionsQuery(userB));

        resultA.Should().HaveCount(1);
        resultB.Should().BeEmpty();
    }
}
