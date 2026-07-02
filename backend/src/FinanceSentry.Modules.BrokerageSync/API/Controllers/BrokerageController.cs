using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

/// <summary>Async IBKR connect: kicks off a background session and returns
/// its id; frontend polls status until a terminal state is reached.</summary>
public sealed record ConnectIBKRRequest(string Username, string Password);

public sealed record ConnectIBKRSessionResponse(Guid SessionId);

[ApiController]
[Route("brokerage")]
public sealed class BrokerageController(
    IIBKRConnectOrchestrator connectOrchestrator,
    IIBKRConnectSessionStore sessionStore,
    ICommandHandler<DisconnectIBKRCommand, Unit> disconnectHandler,
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdingsHandler) : ControllerBase
{
    [HttpPost("ibkr/connect")]
    public IActionResult Connect([FromBody] ConnectIBKRRequest request)
    {
        var sessionId = connectOrchestrator.Start(User.RequireUserId(), request.Username, request.Password);
        return StatusCode(202, new ConnectIBKRSessionResponse(sessionId));
    }

    [HttpGet("ibkr/connect/{sessionId:guid}")]
    public IActionResult GetConnectStatus(Guid sessionId)
    {
        var snapshot = sessionStore.Get(sessionId, User.RequireUserId());
        if (snapshot is null)
            return NotFound(new { errorCode = "SESSION_NOT_FOUND" });

        return Ok(snapshot);
    }

    [HttpDelete("ibkr/connect/{sessionId:guid}")]
    public IActionResult CancelConnect(Guid sessionId)
    {
        var cancelled = sessionStore.Cancel(sessionId, User.RequireUserId());
        return cancelled ? NoContent() : NotFound(new { errorCode = "SESSION_NOT_FOUND" });
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
