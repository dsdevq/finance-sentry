using FinanceSentry.Modules.Risk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceSentry.Modules.Risk.Tests;

internal static class TestSupport
{
    public static RiskDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<RiskDbContext>()
            .UseInMemoryDatabase($"risk-tests-{Guid.NewGuid():N}")
            .Options;
        return new RiskDbContext(options);
    }
}
