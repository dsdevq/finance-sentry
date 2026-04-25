using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.CryptoSync.Application.Commands;
using FinanceSentry.Modules.CryptoSync.Application.Queries;
using FinanceSentry.Modules.CryptoSync.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.CryptoSync.API.Controllers;

[ApiController]
[Route("api/v1/crypto")]
public sealed class CryptoController(
    ICommandHandler<ConnectBinanceCommand, ConnectBinanceResult> connectHandler,
    ICommandHandler<DisconnectBinanceCommand, Unit> disconnectHandler,
    IQueryHandler<GetCryptoHoldingsQuery, CryptoHoldingsResponse> holdingsHandler) : ControllerBase
{
    [HttpPost("binance/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectBinanceRequest request, CancellationToken ct)
    {
        try
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
        catch (BinanceException ex) when (ex.BinanceErrorCode == -1001)
        {
            return Conflict(new
            {
                error = "A Binance account is already connected for this user.",
                errorCode = "ALREADY_CONNECTED",
            });
        }
        catch (BinanceException)
        {
            return UnprocessableEntity(new
            {
                error = "Binance rejected the provided credentials. Verify your API key and secret.",
                errorCode = "INVALID_CREDENTIALS",
            });
        }
    }

    [HttpDelete("binance/disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        try
        {
            await disconnectHandler.Handle(new DisconnectBinanceCommand(User.RequireUserId()), ct);
            return NoContent();
        }
        catch (BinanceException ex) when (ex.BinanceErrorCode == -1002)
        {
            return NotFound(new
            {
                error = "No Binance account is connected for this user.",
                errorCode = "NOT_FOUND",
            });
        }
    }

    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings(CancellationToken ct)
    {
        var result = await holdingsHandler.Handle(new GetCryptoHoldingsQuery(User.RequireUserId()), ct);
        return Ok(result);
    }
}

public sealed record ConnectBinanceRequest(string ApiKey, string ApiSecret);
