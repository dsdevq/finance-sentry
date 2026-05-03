namespace FinanceSentry.Core.Interfaces;

public interface IUserBaseCurrencyReader
{
    Task<string> GetAsync(Guid userId, CancellationToken ct = default);
}
