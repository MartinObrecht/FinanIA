using FinanIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Transactions.Commands;

public record DeleteTransactionCommand(Guid TransactionId, Guid UserId);

public class DeleteTransactionCommandHandler
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<DeleteTransactionCommandHandler> _logger;

    public DeleteTransactionCommandHandler(
        ITransactionRepository repository,
        ILogger<DeleteTransactionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteTransactionCommand command, CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(command.TransactionId, command.UserId, ct);
        if (transaction is null)
            return false;

        await _repository.DeleteAsync(transaction, ct);

        _logger.LogInformation("Transaction deleted. TransactionId: {TransactionId}, UserId: {UserId}", command.TransactionId, command.UserId);

        return true;
    }
}
