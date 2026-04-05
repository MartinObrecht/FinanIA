using FinanIA.Domain.ValueObjects;

namespace FinanIA.Application.Assistant;

public sealed record AskAssistantCommand(
    Guid UserId,
    string Question,
    IReadOnlyList<ConversationTurnDto> History);
