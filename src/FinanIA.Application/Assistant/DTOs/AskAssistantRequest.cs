using FinanIA.Domain.ValueObjects;

namespace FinanIA.Application.Assistant.DTOs;

public sealed record AskAssistantRequest(
    string Message,
    IReadOnlyList<ConversationTurnDto> PreviousMessages);
