using FinanIA.Application.Transactions.Commands;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FinanIA.Application.Tests.Transactions;

public class DeleteTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly DeleteTransactionCommandHandler _handler;

    public DeleteTransactionCommandHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _handler = new DeleteTransactionCommandHandler(
            _repository,
            Substitute.For<ILogger<DeleteTransactionCommandHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ExistingTransaction_DeletesAndReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        _repository.GetByIdAsync(transaction.Id, userId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _handler.HandleAsync(new DeleteTransactionCommand(transaction.Id, userId));

        result.Should().BeTrue();
        await _repository.Received(1).DeleteAsync(transaction, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TransactionNotFound_ReturnsFalse()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _handler.HandleAsync(new DeleteTransactionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.Should().BeFalse();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TransactionBelongsToOtherUser_ReturnsFalse()
    {
        // Repository returns null because GetByIdAsync enforces userId ownership
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _handler.HandleAsync(
            new DeleteTransactionCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ExtractsUserIdFromCommand()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        await _handler.HandleAsync(new DeleteTransactionCommand(transactionId, userId));

        await _repository.Received(1).GetByIdAsync(transactionId, userId, Arg.Any<CancellationToken>());
    }
}
