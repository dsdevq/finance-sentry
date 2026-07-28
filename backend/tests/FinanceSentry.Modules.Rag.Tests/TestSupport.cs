namespace FinanceSentry.Modules.Rag.Tests;

using FinanceSentry.Modules.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal static class TestSupport
{
    public static RagDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseInMemoryDatabase($"rag-tests-{Guid.NewGuid():N}")
            .Options;
        return new RagDbContext(options);
    }
}
