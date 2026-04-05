using FinanIA.Application.Transactions.Queries;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FinanIA.Domain.Interfaces;
using FinanIA.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace FinanIA.Application.Tests.Transactions;

public class GetBalanceQueryHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly GetBalanceQueryHandler _handler;

    public GetBalanceQueryHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _handler = new GetBalanceQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectBalance()
    {
        var userId = Guid.NewGuid();
        _repository.GetBalanceSummaryAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new BalanceSummary(3000m, 1000m));

        var result = await _handler.HandleAsync(new GetBalanceQuery(userId));

        result.TotalIncome.Should().Be(3000m);
        result.TotalExpense.Should().Be(1000m);
        result.Balance.Should().Be(2000m);
    }

    [Fact]
    public async Task HandleAsync_NoTransactions_ReturnsZeroBalance()
    {
        var userId = Guid.NewGuid();
        _repository.GetBalanceSummaryAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new BalanceSummary(0m, 0m));

        var result = await _handler.HandleAsync(new GetBalanceQuery(userId));

        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task HandleAsync_ExpensesExceedIncome_ReturnsNegativeBalance()
    {
        var userId = Guid.NewGuid();
        _repository.GetBalanceSummaryAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new BalanceSummary(500m, 1500m));

        var result = await _handler.HandleAsync(new GetBalanceQuery(userId));

        result.Balance.Should().Be(-1000m);
    }

    [Fact]
    public async Task HandleAsync_IsolatesUserData()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        _repository.GetBalanceSummaryAsync(userA, Arg.Any<CancellationToken>())
            .Returns(new BalanceSummary(5000m, 0m));
        _repository.GetBalanceSummaryAsync(userB, Arg.Any<CancellationToken>())
            .Returns(new BalanceSummary(0m, 0m));

        var resultA = await _handler.HandleAsync(new GetBalanceQuery(userA));
        var resultB = await _handler.HandleAsync(new GetBalanceQuery(userB));

        resultA.Balance.Should().Be(5000m);
        resultB.Balance.Should().Be(0m);
    }
}
