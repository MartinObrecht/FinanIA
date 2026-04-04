using FinanIA.Application.Auth.Commands;
using FinanIA.Application.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace FinanIA.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;

    public AuthController(RegisterUserCommandHandler registerHandler)
    {
        _registerHandler = registerHandler;
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
}
