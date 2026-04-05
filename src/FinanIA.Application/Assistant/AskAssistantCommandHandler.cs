using FinanIA.Application.Assistant.DTOs;
using FinanIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Assistant;

public sealed class AskAssistantCommandHandler
{
    private readonly IFinancialAssistant _financialAssistant;
    private readonly ILogger<AskAssistantCommandHandler> _logger;

    public AskAssistantCommandHandler(
        IFinancialAssistant financialAssistant,
        ILogger<AskAssistantCommandHandler> logger)
    {
        _financialAssistant = financialAssistant;
        _logger = logger;
    }

    public async Task<AssistantResponse> HandleAsync(
        AskAssistantCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing assistant request. UserId: {UserId}", command.UserId);

        var reply = await _financialAssistant.AskAsync(
            command.UserId,
            command.Question,
            command.History,
            cancellationToken);

        return new AssistantResponse(reply);
    }
}
