using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public class IBeamReconcilerTests
{
    // Under the OAuth 1.0a model the reconciler is inert — there is no
    // interactive session to respawn. It must complete without touching any
    // dependency.
    [Fact]
    public async Task ReconcileAllAsync_IsNoOp()
    {
        var reconciler = new IBeamReconciler(NullLogger<IBeamReconciler>.Instance);

        await reconciler.ReconcileAllAsync(default);
    }
}
