using FinanIA.Application.Transactions.Commands;
using FinanIA.Application.Transactions.DTOs;
using FinanIA.Application.Transactions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FinanIA.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionCommandHandler _createHandler;
    private readonly UpdateTransactionCommandHandler _updateHandler;
    private readonly DeleteTransactionCommandHandler _deleteHandler;
    private readonly GetTransactionsQueryHandler _getTransactionsHandler;

    public TransactionsController(
        CreateTransactionCommandHandler createHandler,
        UpdateTransactionCommandHandler updateHandler,
        DeleteTransactionCommandHandler deleteHandler,
        GetTransactionsQueryHandler getTransactionsHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getTransactionsHandler = getTransactionsHandler;
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var command = new CreateTransactionCommand(userId, request.Description, request.Amount, request.Date, request.Type);
        var response = await _createHandler.HandleAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var transactions = await _getTransactionsHandler.HandleAsync(new GetTransactionsQuery(userId), ct);
        return Ok(transactions);
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    // Returns 404 opaque for transactions belonging to other users (does not leak existence).
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var command = new UpdateTransactionCommand(id, userId, request.Description, request.Amount, request.Date, request.Type);
        var response = await _updateHandler.HandleAsync(command, ct);
        if (response is null)
            return NotFound();

        return Ok(response);
    }

    // UserId is extracted exclusively from the validated JWT sub claim via [Authorize].
    // Returns 404 opaque for transactions belonging to other users (does not leak existence).
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var deleted = await _deleteHandler.HandleAsync(new DeleteTransactionCommand(id, userId), ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
