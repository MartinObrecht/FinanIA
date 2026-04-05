namespace FinanIA.Infrastructure.Assistant;

internal sealed record FinancialContext(
    decimal Balance,
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<TransactionSummary> RecentTransactions);

internal sealed record TransactionSummary(
    string Description,
    decimal Amount,
    string Type,
    DateOnly Date);
