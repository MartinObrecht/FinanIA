using System.IdentityModel.Tokens.Jwt;
using FinanIA.Application.Auth.Commands;
using FinanIA.Application.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FinanIA.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly RefreshTokenCommandHandler _refreshTokenHandler;

    public AuthController(
        RegisterUserCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        RefreshTokenCommandHandler refreshTokenHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshTokenHandler = refreshTokenHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Email, request.Password);
        var response = await _registerHandler.HandleAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _loginHandler.HandleAsync(command, ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var userId = ExtractUserIdFromBearer();
        if (userId == Guid.Empty)
            return Unauthorized();

        var command = new RefreshTokenCommand(userId, request.RefreshToken);
        var response = await _refreshTokenHandler.HandleAsync(command, ct);
        return Ok(response);
    }

    private Guid ExtractUserIdFromBearer()
    {
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Guid.Empty;

        var token = authHeader["Bearer ".Length..].Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return Guid.Empty;

            var jwtToken = handler.ReadJwtToken(token);
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}

