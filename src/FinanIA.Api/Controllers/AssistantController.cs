using System.IdentityModel.Tokens.Jwt;
using FinanIA.Application.Assistant;
using FinanIA.Application.Assistant.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanIA.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]
public class AssistantController : ControllerBase
{
    private readonly AskAssistantCommandHandler _handler;
    private readonly IValidator<AskAssistantCommand> _validator;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(
        AskAssistantCommandHandler handler,
        IValidator<AskAssistantCommand> validator,
        ILogger<AssistantController> logger)
    {
        _handler = handler;
        _validator = validator;
        _logger = logger;
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    [HttpPost("ask")]
    [ProducesResponseType<AssistantResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ask([FromBody] AskAssistantRequest request, CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var command = new AskAssistantCommand(userId, request.Message, request.PreviousMessages);
        var validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem();
        }

        try
        {
            var response = await _handler.HandleAsync(command, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assistant error. UserId: {UserId}", userId);
            return Problem(
                type: "https://tools.ietf.org/html/rfc7807",
                title: "Assistente temporariamente indisponível",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "Não foi possível obter resposta do assistente. Tente novamente em alguns instantes.");
        }
    }
}
