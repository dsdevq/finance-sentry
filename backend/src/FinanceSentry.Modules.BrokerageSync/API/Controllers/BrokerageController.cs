using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

[ApiController]
[Route("brokerage")]
public sealed class BrokerageController(
    ICommandHandler<ConnectIBKRCommand, ConnectIBKRResult> connectHandler,
    ICommandHandler<DisconnectIBKRCommand, Unit> disconnectHandler,
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdingsHandler) : ControllerBase
{
    [HttpPost("ibkr/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIBKRRequest request, CancellationToken ct)
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
        await disconnectHandler.Handle(new DisconnectIBKRCommand(User.RequireUserId()), ct);
        return NoContent();
    }
}

public sealed record ConnectIBKRRequest(string Username, string Password);
