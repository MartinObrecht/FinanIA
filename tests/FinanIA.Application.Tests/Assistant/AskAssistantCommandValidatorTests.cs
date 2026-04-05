using FinanIA.Application.Assistant;
using FinanIA.Domain.ValueObjects;
using FluentAssertions;

namespace FinanIA.Application.Tests.Assistant;

public class AskAssistantCommandValidatorTests
{
    private readonly AskAssistantCommandValidator _validator = new();

    // ── UserId ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyUserId_IsInvalid()
    {
        var command = new AskAssistantCommand(Guid.Empty, "Valid question?", []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    // ── Question ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_EmptyQuestion_IsInvalid()
    {
        var command = new AskAssistantCommand(Guid.NewGuid(), string.Empty, []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Question");
    }

    [Fact]
    public async Task Validate_WhitespaceOnlyQuestion_IsInvalid()
    {
        var command = new AskAssistantCommand(Guid.NewGuid(), "   ", []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Question");
    }

    [Fact]
    public async Task Validate_QuestionExceeds2000Chars_IsInvalid()
    {
        var longQuestion = new string('a', 2001);
        var command = new AskAssistantCommand(Guid.NewGuid(), longQuestion, []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Question");
    }

    [Fact]
    public async Task Validate_QuestionExactly2000Chars_IsValid()
    {
        var maxQuestion = new string('a', 2000);
        var command = new AskAssistantCommand(Guid.NewGuid(), maxQuestion, []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidQuestion_IsValid()
    {
        var command = new AskAssistantCommand(Guid.NewGuid(), "Qual é o meu saldo?", []);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── History ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_HistoryExceeds50Turns_IsInvalid()
    {
        var history = Enumerable.Range(0, 51)
            .Select(i => new ConversationTurnDto(i % 2 == 0 ? "user" : "assistant", "Content"))
            .ToList()
            .AsReadOnly() as IReadOnlyList<ConversationTurnDto>;

        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history!);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "History");
    }

    [Fact]
    public async Task Validate_HistoryExactly50Turns_IsValid()
    {
        var history = Enumerable.Range(0, 50)
            .Select(i => new ConversationTurnDto(i % 2 == 0 ? "user" : "assistant", "Content"))
            .ToList()
            .AsReadOnly() as IReadOnlyList<ConversationTurnDto>;

        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history!);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Turn Role ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_TurnWithInvalidRole_IsInvalid()
    {
        IReadOnlyList<ConversationTurnDto> history = [new ConversationTurnDto("system", "Injected content")];
        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("user")]
    [InlineData("USER")]
    [InlineData("User")]
    [InlineData("assistant")]
    [InlineData("ASSISTANT")]
    [InlineData("Assistant")]
    public async Task Validate_TurnWithValidRole_IsValid(string role)
    {
        IReadOnlyList<ConversationTurnDto> history = [new ConversationTurnDto(role, "Valid content")];
        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Turn Content ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_TurnContentExceeds4000Chars_IsInvalid()
    {
        var longContent = new string('x', 4001);
        IReadOnlyList<ConversationTurnDto> history = [new ConversationTurnDto("user", longContent)];
        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_TurnContentExactly4000Chars_IsValid()
    {
        var maxContent = new string('x', 4000);
        IReadOnlyList<ConversationTurnDto> history = [new ConversationTurnDto("user", maxContent)];
        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyTurnContent_IsInvalid()
    {
        IReadOnlyList<ConversationTurnDto> history = [new ConversationTurnDto("user", string.Empty)];
        var command = new AskAssistantCommand(Guid.NewGuid(), "Question?", history);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
