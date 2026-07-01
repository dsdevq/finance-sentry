using FinanceSentry.Core.Cqrs;
using FinanceSentry.Modules.BrokerageSync.Domain;
using FinanceSentry.Modules.BrokerageSync.Domain.Interfaces;
using FinanceSentry.Modules.BrokerageSync.Domain.Repositories;

namespace FinanceSentry.Modules.BrokerageSync.Application.Commands;

public sealed record SyncIBKRHoldingsCommand(Guid UserId) : ICommand<SyncIBKRHoldingsResult>;

public sealed record SyncIBKRHoldingsResult(int HoldingsCount, DateTime SyncedAt);

public sealed class SyncIBKRHoldingsCommandHandler : ICommandHandler<SyncIBKRHoldingsCommand, SyncIBKRHoldingsResult>
{
    private readonly IIBKRCredentialRepository _credentialRepository;
    private readonly IBrokerageHoldingRepository _holdingRepository;
    private readonly IBrokerAdapter _adapter;

    public SyncIBKRHoldingsCommandHandler(
        IIBKRCredentialRepository credentialRepository,
        IBrokerageHoldingRepository holdingRepository,
        IBrokerAdapter adapter)
    {
        _credentialRepository = credentialRepository;
        _holdingRepository = holdingRepository;
        _adapter = adapter;
    }

    public async Task<SyncIBKRHoldingsResult> Handle(SyncIBKRHoldingsCommand request, CancellationToken ct)
    {
        var credential = await _credentialRepository.GetByUserIdAsync(request.UserId, ct)
            ?? throw new InvalidOperationException("No active IBKR credential found for this user.");

        try
        {
            await _adapter.EnsureSessionAsync(credential.Id, ct);

            var accountId = credential.AccountId
                ?? await _adapter.GetAccountIdAsync(credential.Id, ct);

            if (credential.AccountId is null)
            {
                credential.UpdateAccountId(accountId);
                _credentialRepository.Update(credential);
            }

            var positions = await _adapter.GetPositionsAsync(credential.Id, accountId, ct);

            var syncedAt = DateTime.UtcNow;

            var holdings = positions
                .Select(p => new BrokerageHolding(
                    request.UserId,
                    p.Symbol,
                    p.InstrumentType,
                    p.Quantity,
                    p.UsdValue,
                    "ibkr",
                    averageCostUsd: p.AverageCostUsd,
                    acquiredAt: p.AverageCostUsd.HasValue ? syncedAt : null))
                .ToList();

            await _holdingRepository.UpsertRangeAsync(holdings, ct);
            await _holdingRepository.SaveChangesAsync(ct);

            var positionByKey = positions.ToDictionary(p => p.Symbol, StringComparer.Ordinal);
            var persisted = await _holdingRepository.GetByUserIdAsync(request.UserId, ct);
            foreach (var h in persisted)
            {
                if (positionByKey.TryGetValue(h.Symbol, out var pos))
                {
                    h.SetCostBasis(
                        pos.AverageCostUsd,
                        pos.AverageCostUsd.HasValue ? (h.AcquiredAt ?? syncedAt) : h.AcquiredAt);
                }
            }
            await _holdingRepository.SaveChangesAsync(ct);

            credential.RecordSyncSuccess();
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);

            return new SyncIBKRHoldingsResult(holdings.Count, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            credential.RecordSyncError(ex.Message);
            _credentialRepository.Update(credential);
            await _credentialRepository.SaveChangesAsync(ct);
            throw;
        }
    }
}
