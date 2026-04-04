using FinanIA.Domain.Interfaces;

namespace FinanIA.Application.Auth.Commands;

public record LogoutCommand(Guid UserId);

public class LogoutCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        // UserId is extracted exclusively from the validated JWT sub claim in the controller.
        // It is never accepted from the request body to prevent privilege escalation.
        if (command.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuário não identificado.");

        await _refreshTokenRepository.RevokeAllByUserIdAsync(command.UserId, ct);
    }
}
