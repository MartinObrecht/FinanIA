using FinanIA.Domain.Enums;

namespace FinanIA.Application.Transactions.DTOs;

public record TransactionResponse(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    TransactionType Type,
    DateTime CreatedAt);
