namespace FinanceSentry.Modules.Subscriptions.API.Controllers;

using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Subscriptions.API.Responses;
using FinanceSentry.Modules.Subscriptions.Application.Commands;
using FinanceSentry.Modules.Subscriptions.Application.Queries;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("subscriptions")]
public class SubscriptionsController(
    IQueryHandler<GetSubscriptionsQuery, SubscriptionsListResponse> getSubscriptions,
    IQueryHandler<GetSubscriptionSummaryQuery, SubscriptionSummaryResponse> getSummary,
    ICommandHandler<DismissSubscriptionCommand, bool> dismiss,
    ICommandHandler<RestoreSubscriptionCommand, bool> restore) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] bool includeDismissed = false,
        CancellationToken ct = default)
    {
        var result = await getSubscriptions.Handle(
            new GetSubscriptionsQuery(User.RequireUserId().ToString(), includeDismissed), ct);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct = default)
    {
        var result = await getSummary.Handle(
            new GetSubscriptionSummaryQuery(User.RequireUserId().ToString()), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct = default)
    {
        await dismiss.Handle(new DismissSubscriptionCommand(User.RequireUserId().ToString(), id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await restore.Handle(new RestoreSubscriptionCommand(User.RequireUserId().ToString(), id), ct);
        return NoContent();
    }
}
