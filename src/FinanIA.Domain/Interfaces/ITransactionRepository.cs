using FinanIA.Domain.Entities;
using FinanIA.Domain.ValueObjects;

namespace FinanIA.Domain.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<BalanceSummary> GetBalanceSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
