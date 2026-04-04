using BCrypt.Net;
using FinanIA.Application.Auth.DTOs;
using FinanIA.Domain.Entities;
using FinanIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Auth.Commands;

public record RefreshTokenCommand(Guid UserId, string RawRefreshToken);

public class RefreshTokenCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var stored = await _refreshTokenRepository.GetActiveByUserIdAsync(command.UserId, ct);

        if (stored is null || !BCrypt.Net.BCrypt.Verify(command.RawRefreshToken, stored.TokenHash))
        {
            _logger.LogWarning("Invalid or expired refresh token for UserId: {UserId}", command.UserId);
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");
        }

        var newRawToken = _jwtTokenService.GenerateRefreshToken();
        var newHash = BCrypt.Net.BCrypt.HashPassword(newRawToken);

        stored.Revoke(newHash);
        await _refreshTokenRepository.UpdateAsync(stored, ct);

        var newRefreshToken = RefreshToken.Create(command.UserId, newHash);
        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);

        var user = await _userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _logger.LogInformation("Token rotated successfully. UserId: {UserId}", command.UserId);

        return new AuthResponse(accessToken, newRawToken, expiresAt);
    }
}
