using BCrypt.Net;
using FinanIA.Application.Auth;
using FinanIA.Application.Auth.Commands;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace FinanIA.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();

        _handler = new LoginCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _jwtTokenService);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var user = User.Create("user@test.com", passwordHash);

        _userRepository.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenService.GenerateAccessToken(user).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        var result = await _handler.HandleAsync(new LoginCommand("user@test.com", "correct-password"));

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        var user = User.Create("user@test.com", passwordHash);

        _userRepository.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await _handler.HandleAsync(new LoginCommand("user@test.com", "wrong-password"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = async () => await _handler.HandleAsync(new LoginCommand("ghost@test.com", "password"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleAsync_EmailCaseInsensitive_NormalizesToLowercase()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = User.Create("user@test.com", passwordHash);

        _userRepository.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenService.GenerateAccessToken(user).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        await _handler.HandleAsync(new LoginCommand("USER@TEST.COM", "password123"));

        await _userRepository.Received(1).GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>());
    }
}
