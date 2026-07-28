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
    ICommandHandler<RestoreSubscriptionCommand, bool> restore,
    ICommandHandler<SetInstallmentTermCommand, bool> setTerm,
    ICommandHandler<CompleteInstallmentCommand, bool> completeInstallment,
    ICommandHandler<DeleteInstallmentCommand, bool> deleteInstallment,
    ICommandHandler<AddManualInstallmentCommand, Guid> addInstallment) : ControllerBase
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

    [HttpPatch("installments/{id:guid}/term")]
    public async Task<IActionResult> SetTerm(Guid id, [FromBody] SetTermRequest body, CancellationToken ct = default)
    {
        await setTerm.Handle(new SetInstallmentTermCommand(User.RequireUserId().ToString(), id, body.TermCount), ct);
        return NoContent();
    }

    [HttpPatch("installments/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct = default)
    {
        await completeInstallment.Handle(new CompleteInstallmentCommand(User.RequireUserId().ToString(), id), ct);
        return NoContent();
    }

    [HttpDelete("installments/{id:guid}")]
    public async Task<IActionResult> DeleteInstallment(Guid id, CancellationToken ct = default)
    {
        await deleteInstallment.Handle(new DeleteInstallmentCommand(User.RequireUserId().ToString(), id), ct);
        return NoContent();
    }

    [HttpPost("installments")]
    public async Task<IActionResult> AddInstallment([FromBody] AddInstallmentRequest body, CancellationToken ct = default)
    {
        var id = await addInstallment.Handle(new AddManualInstallmentCommand(
            User.RequireUserId().ToString(),
            body.Merchant,
            body.MonthlyAmount,
            body.Currency,
            body.StartDate,
            body.TermCount), ct);
        return Ok(new { id });
    }
}

public record SetTermRequest(int? TermCount);

public record AddInstallmentRequest(
    string Merchant,
    decimal MonthlyAmount,
    string Currency,
    DateOnly StartDate,
    int? TermCount);
