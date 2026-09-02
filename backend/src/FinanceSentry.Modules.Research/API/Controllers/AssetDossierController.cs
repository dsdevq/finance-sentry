namespace FinanceSentry.Modules.Research.API.Controllers;

using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Research.API.Responses;
using FinanceSentry.Modules.Research.Application.Queries;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("research/assets")]
public class AssetDossierController(
    IQueryHandler<GetAssetDossierQuery, AssetDossierResult> handler) : ControllerBase
{
    [HttpGet("{symbol}/dossier")]
    public async Task<IActionResult> Get(string symbol, CancellationToken ct)
    {
        var result = await handler.Handle(new GetAssetDossierQuery(User.RequireUserId(), symbol), ct);
        return Ok(result);
    }
}
