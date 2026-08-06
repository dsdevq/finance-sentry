namespace FinanceSentry.Modules.BankSync.Infrastructure.Categorization;

using FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;
using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <inheritdoc />
public sealed class CategoryReadService(BankSyncDbContext db) : ICategoryReadService
{
    private readonly BankSyncDbContext _db = db;

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
}
