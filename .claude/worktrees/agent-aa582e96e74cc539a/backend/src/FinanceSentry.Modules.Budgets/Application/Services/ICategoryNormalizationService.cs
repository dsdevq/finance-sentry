namespace FinanceSentry.Modules.Budgets.Application.Services;

public interface ICategoryNormalizationService
{
    string Normalize(string? rawCategory);
    string GetLabel(string categoryKey);
}
