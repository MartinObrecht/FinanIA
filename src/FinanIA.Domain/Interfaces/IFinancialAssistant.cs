using FinanIA.Domain.ValueObjects;

namespace FinanIA.Domain.Interfaces;

public interface IFinancialAssistant
{
    Task<string> AskAsync(
        Guid userId,
        string question,
        IReadOnlyList<ConversationTurnDto> history,
        CancellationToken cancellationToken = default);
}
