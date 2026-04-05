using BCrypt.Net;
using FinanIA.Application.Auth;
using FinanIA.Application.Auth.Commands;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FinanIA.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();

        _handler = new RefreshTokenCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _jwtTokenService,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsNewAuthResponseAndRevokesOld()
    {
        var userId = Guid.NewGuid();
        var rawToken = "valid-raw-token";
        var tokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var storedToken = RefreshToken.Create(userId, tokenHash);
        var user = User.Create("user@test.com", "hash");

        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenService.GenerateAccessToken(user).Returns("new-access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("new-raw-refresh-token");

        var result = await _handler.HandleAsync(new RefreshTokenCommand(userId, rawToken));

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-refresh-token");
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));

        storedToken.RevokedAt.Should().NotBeNull("old token must be revoked");
        await _refreshTokenRepository.Received(1).UpdateAsync(storedToken, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoActiveToken_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var act = async () => await _handler.HandleAsync(new RefreshTokenCommand(userId, "any-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAsync_WrongTokenValue_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var correctHash = BCrypt.Net.BCrypt.HashPassword("correct-token");
        var storedToken = RefreshToken.Create(userId, correctHash);

        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(storedToken);

        var act = async () => await _handler.HandleAsync(new RefreshTokenCommand(userId, "wrong-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAsync_RevokedToken_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        // Revoked token: GetActiveByUserIdAsync returns null (repo filters RevokedAt == null)
        _refreshTokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var act = async () => await _handler.HandleAsync(new RefreshTokenCommand(userId, "revoked-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
