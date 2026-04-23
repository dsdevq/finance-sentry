using System.Security.Claims;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

[ApiController]
[Route("api/v1/brokerage")]
public sealed class BrokerageController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrokerageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    [HttpPost("ibkr/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIBKRRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and Password are required.", errorCode = "VALIDATION_ERROR" });

        try
        {
            var result = await _mediator.Send(
                new ConnectIBKRCommand(userId.Value, request.Username, request.Password),
                ct);

            return StatusCode(201, new
            {
                message = "IBKR account connected successfully.",
                holdingsCount = result.HoldingsCount,
                connectedAt = result.ConnectedAt,
            });
        }
        catch (BrokerAlreadyConnectedException)
        {
            return Conflict(new
            {
                error = "An IBKR account is already connected for this user.",
                errorCode = "ALREADY_CONNECTED",
            });
        }
        catch (BrokerAuthException)
        {
            return UnprocessableEntity(new
            {
                error = "IB Gateway rejected the provided credentials. Verify your username and password.",
                errorCode = "INVALID_CREDENTIALS",
            });
        }
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        var result = await _mediator.Send(new GetBrokerageHoldingsQuery(userId.Value), ct);

        return Ok(new
        {
            provider = result.Provider,
            syncedAt = result.SyncedAt,
            isStale = result.IsStale,
            positions = result.Positions.Select(p => new
            {
                symbol = p.Symbol,
                instrumentType = p.InstrumentType,
                quantity = p.Quantity,
                usdValue = p.UsdValue,
            }),
            totalUsdValue = result.TotalUsdValue,
        });
    }
    [HttpDelete("ibkr/disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        try
        {
            await _mediator.Send(new DisconnectIBKRCommand(userId.Value), ct);
            return NoContent();
        }
        catch (BrokerAccountNotFoundException)
        {
            return NotFound(new
            {
                error = "No active IBKR account connected for this user.",
                errorCode = "NOT_CONNECTED",
            });
        }
    }
}

public sealed record ConnectIBKRRequest(string Username, string Password);
