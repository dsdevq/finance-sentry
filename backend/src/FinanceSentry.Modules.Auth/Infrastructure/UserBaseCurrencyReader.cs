namespace FinanceSentry.Modules.Auth.Infrastructure;

using FinanceSentry.Core.Interfaces;
using FinanceSentry.Modules.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public class UserBaseCurrencyReader(UserManager<ApplicationUser> users) : IUserBaseCurrencyReader
{
    private readonly UserManager<ApplicationUser> _users = users;

    public async Task<string> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        return user?.BaseCurrency ?? "USD";
    }
}
