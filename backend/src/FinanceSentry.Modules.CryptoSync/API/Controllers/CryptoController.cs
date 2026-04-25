using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.CryptoSync.API.Controllers;

[ApiController]
[Route("crypto")]
public sealed class CryptoController(
    ICommandHandler<ConnectBinanceCommand, ConnectBinanceResult> connectHandler,
    ICommandHandler<DisconnectBinanceCommand, Unit> disconnectHandler,
    IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> holdingsHandler) : ControllerBase
{
    [HttpPost("binance/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectBinanceRequest request, CancellationToken ct)
    {
        var result = await connectHandler.Handle(
            new ConnectBinanceCommand(User.RequireUserId(), request.ApiKey, request.ApiSecret),
            ct);

        return StatusCode(201, new
        {
            message = "Binance account connected successfully.",
            holdingsCount = result.HoldingsCount,
            syncedAt = result.SyncedAt,
        });
    }

    [HttpDelete("binance/disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await disconnectHandler.Handle(new DisconnectBinanceCommand(User.RequireUserId()), ct);
        return NoContent();
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var result = await holdingsHandler.Handle(new GetCryptoHoldingsQuery(User.RequireUserId()), ct);
        return Ok(result);
    }
}
