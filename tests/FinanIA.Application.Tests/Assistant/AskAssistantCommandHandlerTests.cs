using FinanIA.Application.Assistant;
using FinanIA.Application.Assistant.DTOs;
using FinanIA.Domain.Interfaces;
using FinanIA.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FinanIA.Application.Tests.Assistant;

public class AskAssistantCommandHandlerTests
{
    private readonly IFinancialAssistant _assistant;
    private readonly ILogger<AskAssistantCommandHandler> _logger;
    private readonly AskAssistantCommandHandler _handler;

    public AskAssistantCommandHandlerTests()
    {
        _assistant = Substitute.For<IFinancialAssistant>();
        _logger = Substitute.For<ILogger<AskAssistantCommandHandler>>();
        _handler = new AskAssistantCommandHandler(_assistant, _logger);
    }

    [Fact]
    public async Task HandleAsync_DelegatesToFinancialAssistant()
    {
        var userId = Guid.NewGuid();
        const string question = "Qual é o meu saldo?";
        IReadOnlyList<ConversationTurnDto> history = [];
        const string expectedReply = "Saldo: R$ 1.000,00.\n\nEsta resposta não substitui aconselhamento financeiro profissional.";

        _assistant
            .AskAsync(userId, question, history, Arg.Any<CancellationToken>())
            .Returns(expectedReply);

        var result = await _handler.HandleAsync(new AskAssistantCommand(userId, question, history));

        result.Reply.Should().Be(expectedReply);
        await _assistant.Received(1).AskAsync(userId, question, history, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsAssistantResponse()
    {
        const string reply = "Ok.\n\nEsta resposta não substitui aconselhamento financeiro profissional.";

        _assistant
            .AskAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ConversationTurnDto>>(), Arg.Any<CancellationToken>())
            .Returns(reply);

        var result = await _handler.HandleAsync(
            new AskAssistantCommand(Guid.NewGuid(), "Pergunta?", []));

        result.Should().BeOfType<AssistantResponse>();
        result.Reply.Should().Be(reply);
    }

    [Fact]
    public async Task HandleAsync_PassesCommandHistoryToAssistant()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<ConversationTurnDto> history =
        [
            new ConversationTurnDto("user", "Olá"),
            new ConversationTurnDto("assistant", "Olá!\n\nEsta resposta não substitui aconselhamento financeiro profissional.")
        ];

        _assistant
            .AskAsync(Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ConversationTurnDto>>(), Arg.Any<CancellationToken>())
            .Returns("Resposta.\n\nEsta resposta não substitui aconselhamento financeiro profissional.");

        await _handler.HandleAsync(new AskAssistantCommand(userId, "Mais uma pergunta?", history));

        await _assistant.Received(1).AskAsync(
            userId,
            "Mais uma pergunta?",
            history,
            Arg.Any<CancellationToken>());
    }
}
