using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

public sealed record ConnectIBKRRequest(string Username, string Password);

[ApiController]
[Route("brokerage")]
public sealed class BrokerageController(
    IIBKRConnector connector,
    ICommandHandler<DisconnectIBKRCommand, Unit> disconnectHandler,
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdingsHandler) : ControllerBase
{
    /// <summary>
    /// Blocking connect: awaits credentials persist → per-user IBeam spawn →
    /// CPG auth (including 2FA push tap) → initial holdings sync. Typical
    /// end-to-end latency is 20–60s. Client disconnect is honoured — the
    /// request's CancellationToken tears the container down and rolls the
    /// credential row back so no half-applied state leaks.
    /// </summary>
    [HttpPost("ibkr/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIBKRRequest request, CancellationToken ct)
    {
        try
        {
            var result = await connector.ConnectAsync(
                User.RequireUserId(), request.Username, request.Password, ct);
            return Ok(result);
        }
        catch (IBKRConnectException ex)
        {
            return StatusCode(ex.StatusCode, new { errorCode = ex.ErrorCode, errorMessage = ex.Message });
        }
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
