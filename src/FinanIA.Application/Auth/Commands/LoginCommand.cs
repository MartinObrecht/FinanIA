using BCrypt.Net;
using FinanIA.Application.Auth.DTOs;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Auth.Commands;

public record LoginCommand(string Email, string Password);

public class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var email = command.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, ct);

        // Generic error — do not reveal whether email exists
        if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", email);
            throw new UnauthorizedAccessException("E-mail ou senha incorretos.");
        }

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = BCrypt.Net.BCrypt.HashPassword(rawRefreshToken);

        // Revoke all existing tokens before issuing a new one (single active token per user)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, ct);

        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash);
        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _logger.LogInformation("User logged in successfully. UserId: {UserId}", user.Id);

        return new AuthResponse(accessToken, rawRefreshToken, expiresAt);
    }
}
