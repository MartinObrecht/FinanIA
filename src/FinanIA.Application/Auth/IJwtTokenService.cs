using FinanIA.Domain.Entities;

namespace FinanIA.Application.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
