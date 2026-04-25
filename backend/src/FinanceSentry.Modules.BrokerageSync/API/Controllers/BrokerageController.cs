using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using FinanceSentry.Modules.BrokerageSync.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

[ApiController]
[Route("api/v1/brokerage")]
public sealed class BrokerageController(
    ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult> connectHandler,
    ICommandHandler<DisconnectIBKRCommand, Unit> disconnectHandler,
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdingsHandler) : ControllerBase
{
    [HttpPost("ibkr/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIBKRRequest request, CancellationToken ct)
    {
        try
        {
            var result = await connectHandler.Handle(
                new ConnectIBKRCommand(User.RequireUserId(), request.Username, request.Password),
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
        var result = await holdingsHandler.Handle(new GetBrokerageHoldingsQuery(User.RequireUserId()), ct);

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
        try
        {
            await disconnectHandler.Handle(new DisconnectIBKRCommand(User.RequireUserId()), ct);
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
