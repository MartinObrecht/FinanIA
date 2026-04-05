using FinanIA.Application.Transactions.DTOs;
using FinanIA.Domain.Interfaces;

namespace FinanIA.Application.Transactions.Queries;

public record GetTransactionsQuery(Guid UserId);

public class GetTransactionsQueryHandler
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TransactionResponse>> HandleAsync(GetTransactionsQuery query, CancellationToken ct = default)
    {
        var transactions = await _repository.GetAllByUserIdAsync(query.UserId, ct);
        return transactions
            .Select(t => new TransactionResponse(t.Id, t.Description, t.Amount, t.Date, t.Type, t.CreatedAt))
            .ToList()
            .AsReadOnly();
    }
}
