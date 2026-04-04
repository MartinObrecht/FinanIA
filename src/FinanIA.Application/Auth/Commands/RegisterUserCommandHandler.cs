using BCrypt.Net;
using FinanIA.Application.Auth.DTOs;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using FluentValidation;

namespace FinanIA.Application.Auth.Commands;

public class RegisterUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterUserCommand> _validator;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterUserCommand> validator)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _validator = validator;
    }

    public async Task<AuthResponse> HandleAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        var validationResult = await _validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var email = command.Email.ToLowerInvariant();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

        var user = User.Create(email, passwordHash);
        await _userRepository.AddAsync(user, ct);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawRefreshToken);
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash);
        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        return new AuthResponse(accessToken, rawRefreshToken, expiresAt);
    }
}
