using FinanIA.Domain.Enums;
using FluentValidation;

namespace FinanIA.Application.Transactions.DTOs;

public record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    DateOnly Date,
    TransactionType Type);

public class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("A descrição não pode conter somente espaços.")
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("A data é obrigatória.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo deve ser 'Income' ou 'Expense'.");
    }
}
