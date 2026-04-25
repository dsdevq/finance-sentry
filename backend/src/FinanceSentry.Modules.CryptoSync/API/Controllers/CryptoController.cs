using System.Security.Claims;
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

    private Guid? GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    [HttpPost("binance/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectBinanceRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        if (string.IsNullOrWhiteSpace(request.ApiKey) || string.IsNullOrWhiteSpace(request.ApiSecret))
            return BadRequest(new { error = "apiKey and apiSecret are required.", errorCode = "VALIDATION_ERROR" });

        try
        {
            var result = await connectHandler.Handle(
                new ConnectBinanceCommand(userId.Value, request.ApiKey, request.ApiSecret),
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
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        try
        {
            await disconnectHandler.Handle(new DisconnectBinanceCommand(userId.Value), ct);
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
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Authentication required.", errorCode = "UNAUTHORIZED" });

        var result = await holdingsHandler.Handle(new GetCryptoHoldingsQuery(userId.Value), ct);
        return Ok(result);
    }
}

public sealed record ConnectBinanceRequest(string ApiKey, string ApiSecret);
