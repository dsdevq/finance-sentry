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
    public async Task<IActionResult> Connect(CancellationToken ct)
    {
        var result = await connectHandler.Handle(
            new ConnectIBKRCommand(User.RequireUserId()),
            ct);

        return StatusCode(201, result);
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var result = await holdingsHandler.Handle(new GetBrokerageHoldingsQuery(User.RequireUserId()), ct);
        return Ok(result);
    }

    [HttpDelete("ibkr/disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await disconnectHandler.Handle(new DisconnectIBKRCommand(User.RequireUserId()), ct);
        return NoContent();
    }
}
