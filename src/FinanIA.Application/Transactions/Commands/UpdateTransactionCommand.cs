using FinanIA.Application.Transactions.DTOs;
using FinanIA.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Transactions.Commands;

public record UpdateTransactionCommand(
    Guid TransactionId,
    Guid UserId,
    string Description,
    decimal Amount,
    DateOnly Date,
    Domain.Enums.TransactionType Type);

public class UpdateTransactionCommandHandler
{
    private readonly ITransactionRepository _repository;
    private readonly IValidator<UpdateTransactionRequest> _validator;
    private readonly ILogger<UpdateTransactionCommandHandler> _logger;

    public UpdateTransactionCommandHandler(
        ITransactionRepository repository,
        IValidator<UpdateTransactionRequest> validator,
        ILogger<UpdateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TransactionResponse?> HandleAsync(UpdateTransactionCommand command, CancellationToken ct = default)
    {
        var request = new UpdateTransactionRequest(command.Description, command.Amount, command.Date, command.Type);
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var transaction = await _repository.GetByIdAsync(command.TransactionId, command.UserId, ct);
        if (transaction is null)
            return null;

        transaction.Update(command.Description, command.Amount, command.Date, command.Type);
        await _repository.UpdateAsync(transaction, ct);

        _logger.LogInformation("Transaction updated. TransactionId: {TransactionId}, UserId: {UserId}", transaction.Id, command.UserId);

        return new TransactionResponse(transaction.Id, transaction.Description, transaction.Amount,
            transaction.Date, transaction.Type, transaction.CreatedAt);
    }
}
