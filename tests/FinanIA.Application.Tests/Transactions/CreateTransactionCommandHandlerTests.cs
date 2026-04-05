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

public class CreateTransactionCommandHandlerTests
{
    private readonly ITransactionRepository _repository;
    private readonly IValidator<CreateTransactionRequest> _validator;
    private readonly CreateTransactionCommandHandler _handler;

    public CreateTransactionCommandHandlerTests()
    {
        _repository = Substitute.For<ITransactionRepository>();
        _validator = Substitute.For<IValidator<CreateTransactionRequest>>();

        _handler = new CreateTransactionCommandHandler(
            _repository,
            _validator,
            Substitute.For<ILogger<CreateTransactionCommandHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SavesAndReturnsResponse()
    {
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var userId = Guid.NewGuid();
        var command = new CreateTransactionCommand(userId, "Salário", 5000m,
            new DateOnly(2026, 3, 31), TransactionType.Income);

        var result = await _handler.HandleAsync(command);

        result.Should().NotBeNull();
        result.Description.Should().Be("Salário");
        result.Amount.Should().Be(5000m);
        result.Type.Should().Be(TransactionType.Income);
        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Amount", "O valor deve ser maior que zero.") };
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new CreateTransactionCommand(Guid.NewGuid(), "Test", 0m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_NegativeAmount_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Amount", "O valor deve ser maior que zero.") };
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new CreateTransactionCommand(Guid.NewGuid(), "Test", -100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_EmptyDescription_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Description", "A descrição é obrigatória.") };
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new CreateTransactionCommand(Guid.NewGuid(), string.Empty, 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WhitespaceDescription_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Description", "A descrição não pode conter somente espaços.") };
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var command = new CreateTransactionCommand(Guid.NewGuid(), "   ", 100m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_ExtractsUserIdFromCommand()
    {
        _validator.ValidateAsync(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var expectedUserId = Guid.NewGuid();
        var command = new CreateTransactionCommand(expectedUserId, "Salário", 1000m,
            new DateOnly(2026, 1, 1), TransactionType.Income);

        await _handler.HandleAsync(command);

        await _repository.Received(1).AddAsync(
            Arg.Is<Transaction>(t => t.UserId == expectedUserId),
            Arg.Any<CancellationToken>());
    }
}
