using FinanIA.Application.Transactions.DTOs;
using FinanIA.Domain.Interfaces;

namespace FinanIA.Application.Transactions.Queries;

public record GetBalanceQuery(Guid UserId);

public class GetBalanceQueryHandler
{
    private readonly ITransactionRepository _repository;

    public GetBalanceQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<BalanceResponse> HandleAsync(GetBalanceQuery query, CancellationToken ct = default)
    {
        var summary = await _repository.GetBalanceSummaryAsync(query.UserId, ct);
        return new BalanceResponse(summary.TotalIncome, summary.TotalExpense, summary.Balance);
    }
}
