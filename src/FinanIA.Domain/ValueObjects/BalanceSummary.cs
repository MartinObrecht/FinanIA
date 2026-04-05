namespace FinanIA.Domain.ValueObjects;

public sealed record BalanceSummary(
    decimal TotalIncome,
    decimal TotalExpense)
{
    public decimal Balance => TotalIncome - TotalExpense;
}
