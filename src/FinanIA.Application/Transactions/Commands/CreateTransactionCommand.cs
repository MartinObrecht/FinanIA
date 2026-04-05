using FinanIA.Application.Transactions.DTOs;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Transactions.Commands;

public record CreateTransactionCommand(
    Guid UserId,
    string Description,
    decimal Amount,
    DateOnly Date,
    Domain.Enums.TransactionType Type);

public class CreateTransactionCommandHandler
{
    private readonly ITransactionRepository _repository;
    private readonly IValidator<DTOs.CreateTransactionRequest> _validator;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository repository,
        IValidator<DTOs.CreateTransactionRequest> validator,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TransactionResponse> HandleAsync(CreateTransactionCommand command, CancellationToken ct = default)
    {
        var request = new DTOs.CreateTransactionRequest(command.Description, command.Amount, command.Date, command.Type);
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var transaction = Transaction.Create(command.UserId, command.Description, command.Amount, command.Date, command.Type);
        await _repository.AddAsync(transaction, ct);

        _logger.LogInformation("Transaction created. TransactionId: {TransactionId}, UserId: {UserId}", transaction.Id, command.UserId);

        return new TransactionResponse(transaction.Id, transaction.Description, transaction.Amount,
            transaction.Date, transaction.Type, transaction.CreatedAt);
    }
}
