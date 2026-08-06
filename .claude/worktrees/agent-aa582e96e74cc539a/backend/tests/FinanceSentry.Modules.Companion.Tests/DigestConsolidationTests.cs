namespace FinanceSentry.Modules.Companion.Tests;

using FinanceSentry.Modules.Companion.Application.Commands;
using FinanceSentry.Modules.Companion.Application.Queries;
using FinanceSentry.Modules.Companion.Domain;
using FinanceSentry.Modules.Companion.Domain.Repositories;
using FinanceSentry.Modules.Companion.Infrastructure.Persistence;
using FinanceSentry.Modules.Companion.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Digest consolidation semantics (feature 031, US3, T036): held-for-digest events are collected once,
/// excluded from the normal pull, and don't repeat after the agent acks. Empty → nothing.
/// </summary>
public sealed class DigestConsolidationTests
{
    private static readonly Guid User = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Other = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private sealed class DigestModeSettings : INotificationSettingRepository
    {
        public Task<CompanionNotificationSetting> GetOrDefaultAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(new CompanionNotificationSetting { UserId = userId, Mode = NotificationMode.Digest });

        public Task UpsertAsync(CompanionNotificationSetting setting, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<CompanionNotificationSetting>> ListByModeAsync(
            NotificationMode mode, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CompanionNotificationSetting>>([]);
    }

    private static CompanionDbContext NewDb() => new(
        new DbContextOptionsBuilder<CompanionDbContext>()
            .UseInMemoryDatabase($"digest-{Guid.NewGuid():N}").Options);

    private static CompanionEvent Held(Guid user, string key) => new()
    {
        UserId = user, Kind = CompanionEventKind.Opportunity, Subject = "X", Severity = "info",
        Summary = "held", DedupKey = key, Disposition = EventDisposition.HeldForDigest,
        OccurredAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Held_events_are_pulled_only_with_digest_flag_and_not_across_users()
    {
        await using var db = NewDb();
        db.Events.AddRange(Held(User, "a"), Held(User, "b"), Held(Other, "c"));
        await db.SaveChangesAsync();
        var events = new CompanionEventRepository(db);
        var handler = new GetPendingCompanionEventsQueryHandler(events, new DigestModeSettings());

        var withoutFlag = await handler.Handle(new GetPendingCompanionEventsQuery(User, 25, false), default);
        withoutFlag.Events.Should().BeEmpty("held-for-digest is excluded from the normal pull");

        var withFlag = await handler.Handle(new GetPendingCompanionEventsQuery(User, 25, true), default);
        withFlag.Events.Should().HaveCount(2, "only this user's held events");
    }

    [Fact]
    public async Task Acked_digest_events_do_not_repeat()
    {
        await using var db = NewDb();
        db.Events.AddRange(Held(User, "a"), Held(User, "b"));
        await db.SaveChangesAsync();
        var events = new CompanionEventRepository(db);
        var handler = new GetPendingCompanionEventsQueryHandler(events, new DigestModeSettings());

        var first = await handler.Handle(new GetPendingCompanionEventsQuery(User, 25, true), default);
        var ids = first.Events.Select(e => e.Id).ToList();
        await new AcknowledgeCompanionEventsCommandHandler(events)
            .Handle(new AcknowledgeCompanionEventsCommand(User, ids), default);

        var second = await handler.Handle(new GetPendingCompanionEventsQuery(User, 25, true), default);
        second.Events.Should().BeEmpty("acked events are Delivered and don't resurface");
    }

    [Fact]
    public async Task No_held_events_yields_nothing()
    {
        await using var db = NewDb();
        var handler = new GetPendingCompanionEventsQueryHandler(
            new CompanionEventRepository(db), new DigestModeSettings());

        var result = await handler.Handle(new GetPendingCompanionEventsQuery(User, 25, true), default);

        result.Events.Should().BeEmpty();
    }
}
