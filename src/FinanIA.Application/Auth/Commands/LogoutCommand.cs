using FinanIA.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinanIA.Application.Auth.Commands;

public record LogoutCommand(Guid UserId);

public class LogoutCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        // UserId is extracted exclusively from the validated JWT sub claim in the controller.
        // It is never accepted from the request body to prevent privilege escalation.
        if (command.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuário não identificado.");

        await _refreshTokenRepository.RevokeAllByUserIdAsync(command.UserId, ct);

        _logger.LogInformation("User logged out. All refresh tokens revoked. UserId: {UserId}", command.UserId);
    }
}
