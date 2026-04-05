using FinanIA.Application.Transactions.DTOs;
using FinanIA.Application.Transactions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FinanIA.Api.Controllers;

[ApiController]
[Route("api/balance")]
[Authorize]
public class BalanceController : ControllerBase
{
    private readonly GetBalanceQueryHandler _getBalanceHandler;

    public BalanceController(GetBalanceQueryHandler getBalanceHandler)
    {
        _getBalanceHandler = getBalanceHandler;
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    [HttpGet]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var response = await _getBalanceHandler.HandleAsync(new GetBalanceQuery(userId), ct);
        return Ok(response);
    }
}
