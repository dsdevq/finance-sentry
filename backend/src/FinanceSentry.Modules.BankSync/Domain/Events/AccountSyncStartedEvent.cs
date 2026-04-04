namespace FinanceSentry.Modules.BankSync.Domain.Events;

using MediatR;

/// <summary>
/// Published when a sync job begins for a bank account.
/// Consumers may use this to track sync initiation in monitoring dashboards.
/// </summary>
public record AccountSyncStartedEvent(
    Guid AccountId,
    string? CorrelationId,
    DateTime StartedAt) : INotification;
