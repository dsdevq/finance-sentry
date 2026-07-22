namespace FinanceSentry.Modules.Research.Tests.Companion;

using FinanceSentry.Modules.Research.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Builds a throwaway in-memory <see cref="ResearchDbContext"/> for companion-layer tests.</summary>
internal static class CompanionTestContext
{
    public static ResearchDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ResearchDbContext>()
            .UseInMemoryDatabase($"companion-{Guid.NewGuid():N}")
            .Options;
        return new ResearchDbContext(options);
    }
}
