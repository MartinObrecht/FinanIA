using BCrypt.Net;
using FinanIA.Application.Auth;
using FinanIA.Application.Auth.Commands;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace FinanIA.Application.Tests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _validator = Substitute.For<IValidator<RegisterUserCommand>>();

        _handler = new RegisterUserCommandHandler(
            _userRepository,
            _refreshTokenRepository,
            _jwtTokenService,
            _validator);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsAuthResponse()
    {
        _validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _jwtTokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        var result = await _handler.HandleAsync(new RegisterUserCommand("user@test.com", "password123"));

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh-token");
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsValidationException()
    {
        var errors = new[] { new ValidationFailure("Email", "E-mail já está em uso.") };
        _validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(errors));

        var act = async () =>
            await _handler.HandleAsync(new RegisterUserCommand("existing@test.com", "password123"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task HandleAsync_PasswordIsBCryptHashedBeforeSave()
    {
        _validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _jwtTokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        await _handler.HandleAsync(new RegisterUserCommand("user@test.com", "password123"));

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => BCrypt.Net.BCrypt.Verify("password123", u.PasswordHash)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CreatesAndSavesRefreshToken()
    {
        _validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _jwtTokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        await _handler.HandleAsync(new RegisterUserCommand("user@test.com", "password123"));

        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Any<RefreshToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailIsNormalized_BeforeSave()
    {
        _validator.ValidateAsync(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _jwtTokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");

        await _handler.HandleAsync(new RegisterUserCommand("User@Test.COM", "password123"));

        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "user@test.com"),
            Arg.Any<CancellationToken>());
    }
}
