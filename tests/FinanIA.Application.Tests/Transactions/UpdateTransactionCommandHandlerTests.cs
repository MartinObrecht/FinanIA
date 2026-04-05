using FinanIA.Application.Transactions.Commands;
using FinanIA.Application.Transactions.DTOs;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Enums;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FinanIA.Application.Tests.Transactions;

public class UpdateTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly IValidator<UpdateTransactionRequest> _validator;
    private readonly UpdateTransactionCommandHandler _handler;

    public UpdateTransactionCommandHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _validator = Substitute.For<IValidator<UpdateTransactionRequest>>();

        _handler = new UpdateTransactionCommandHandler(
            _repository,
            _validator,
            Substitute.For<ILogger<UpdateTransactionCommandHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesAndReturnsResponse()
    {
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(userId, "Original", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);
        _repository.GetByIdAsync(transaction.Id, userId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var command = new UpdateTransactionCommand(transaction.Id, userId, "Updated", 999m,
            new DateOnly(2026, 6, 1), TransactionType.Expense);

        var result = await _handler.HandleAsync(command);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Updated");
        result.Amount.Should().Be(999m);
        result.Type.Should().Be(TransactionType.Expense);
        await _repository.Received(1).UpdateAsync(transaction, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Amount", "O valor deve ser maior que zero.") };
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", 0m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NegativeAmount_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Amount", "O valor deve ser maior que zero.") };
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", -50m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_EmptyDescription_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Description", "A descrição é obrigatória.") };
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_TransactionNotFound_ReturnsNull()
    {
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var result = await _handler.HandleAsync(command);

        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_TransactionBelongsToOtherUser_ReturnsNull()
    {
        _validator.ValidateAsync(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Repository returns null because userId doesn't match (opaque 404)
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var command = new UpdateTransactionCommand(Guid.NewGuid(), Guid.NewGuid(), "Test", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var result = await _handler.HandleAsync(command);

        result.Should().BeNull();
    }
}
