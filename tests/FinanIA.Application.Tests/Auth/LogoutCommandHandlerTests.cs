using FinanIA.Application.Auth.Commands;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace FinanIA.Application.Tests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _handler = new LogoutCommandHandler(_refreshTokenRepository);
    }

    [Fact]
    public async Task HandleAsync_ValidUserId_RevokesAllTokens()
    {
        var userId = Guid.NewGuid();
        var command = new LogoutCommand(userId);

        await _handler.HandleAsync(command);

        await _refreshTokenRepository.Received(1).RevokeAllByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyUserId_ThrowsUnauthorizedAccessException()
    {
        var command = new LogoutCommand(Guid.Empty);

        var act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAsync_ValidUserId_CompletesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var command = new LogoutCommand(userId);

        var act = async () => await _handler.HandleAsync(command);

        await act.Should().NotThrowAsync();
    }
}
