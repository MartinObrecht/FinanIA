using FinanIA.Domain.Enums;

namespace FinanIA.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }  // EF Core

    public static Transaction Create(
        Guid userId,
        string description,
        decimal amount,
        DateOnly date,
        TransactionType type)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = description,
            Amount = amount,
            Date = date,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string description, decimal amount, DateOnly date, TransactionType type)
    {
        Description = description;
        Amount = amount;
        Date = date;
        Type = type;
    }
}
