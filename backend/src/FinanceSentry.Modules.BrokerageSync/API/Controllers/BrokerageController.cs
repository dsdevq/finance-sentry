using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Application.Commands;
using FinanceSentry.Modules.BrokerageSync.Application.Connect;
using FinanceSentry.Modules.BrokerageSync.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSentry.Modules.BrokerageSync.API.Controllers;

public sealed record ConnectIBKRRequest(
    string ConsumerKey,
    string AccessToken,
    string AccessTokenSecret,
    string SignatureKey,
    string EncryptionKey,
    string DhParam);

[ApiController]
[Route("brokerage")]
public sealed class BrokerageController(
    IIBKRConnector connector,
    ICommandHandler<DisconnectIBKRCommand, Unit> disconnectHandler,
    IQueryHandler<GetBrokerageHoldingsQuery, BrokerageHoldingsResponse> holdingsHandler) : ControllerBase
{
    /// <summary>
    /// Persists the user's IBKR OAuth 1.0a artifacts (encrypting the secret
    /// material at rest). Live-session-token derivation and the initial holdings
    /// sync run out of band once the consumer key activates on IBKR's side.
    /// </summary>
    [HttpPost("ibkr/connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectIBKRRequest request, CancellationToken ct)
    {
        try
        {
            var artifacts = new ConnectIBKRArtifacts(
                request.ConsumerKey,
                request.AccessToken,
                request.AccessTokenSecret,
                request.SignatureKey,
                request.EncryptionKey,
                request.DhParam);
            var result = await connector.ConnectAsync(User.RequireUserId(), artifacts, ct);
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
