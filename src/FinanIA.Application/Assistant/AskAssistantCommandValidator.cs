using FluentValidation;

namespace FinanIA.Application.Assistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    private static readonly HashSet<string> ValidRoles =
        new(StringComparer.OrdinalIgnoreCase) { "user", "assistant" };

    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.History)
            .NotNull()
            .Must(h => h.Count <= 50)
                .WithMessage("History must not exceed 50 turns.");

        RuleForEach(x => x.History)
            .ChildRules(turn =>
            {
                turn.RuleFor(t => t.Role)
                    .Must(r => ValidRoles.Contains(r))
                        .WithMessage("Each turn Role must be 'user' or 'assistant'.");

                turn.RuleFor(t => t.Content)
                    .NotEmpty()
                    .MaximumLength(4000);
            });
    }
}
