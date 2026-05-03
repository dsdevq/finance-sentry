namespace FinanceSentry.Modules.BankSync.Application.Services.CategoryMapping;

public interface IProviderCategoryMapper
{
    string Map(string? rawCategory);
}
