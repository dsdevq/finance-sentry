namespace FinanceSentry.Modules.Alerts.API.Controllers;

using FinanceSentry.Core.Auth;
using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.Alerts.API.Responses;
using FinanceSentry.Modules.Alerts.Application.Commands;
using FinanceSentry.Modules.Alerts.Application.Queries;
using FinanceSentry.Modules.Alerts.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("alerts")]
public class AlertsController(
    IQueryHandler<GetAlertsQuery, AlertsPageResponse> getAlerts,
    IQueryHandler<GetUnreadCountQuery, UnreadCountResponse> getUnreadCount,
    ICommandHandler<MarkAlertReadCommand, bool> markRead,
    ICommandHandler<MarkAllAlertsReadCommand, Unit> markAllRead,
    ICommandHandler<DismissAlertCommand, bool> dismiss) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string filter = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await getAlerts.Handle(
            new GetAlertsQuery(User.RequireUserId(), filter, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await getUnreadCount.Handle(new GetUnreadCountQuery(User.RequireUserId()), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var ok = await markRead.Handle(new MarkAlertReadCommand(User.RequireUserId(), id), ct);
        if (!ok) throw new AlertNotFoundException();
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await markAllRead.Handle(new MarkAllAlertsReadCommand(User.RequireUserId()), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
    {
        var ok = await dismiss.Handle(new DismissAlertCommand(User.RequireUserId(), id), ct);
        if (!ok) throw new AlertNotFoundException();
        return NoContent();
    }
}
