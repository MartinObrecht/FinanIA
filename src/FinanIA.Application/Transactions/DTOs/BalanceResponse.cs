namespace FinanIA.Application.Transactions.DTOs;

public record BalanceResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);
