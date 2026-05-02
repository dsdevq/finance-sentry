using Microsoft.AspNetCore.Identity;

namespace FinanceSentry.Modules.Auth.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? GoogleId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string Theme { get; set; } = "system";
    public bool EmailAlerts { get; set; } = true;
    public bool LowBalanceAlerts { get; set; } = true;
    public decimal LowBalanceThreshold { get; set; } = 500m;
    public bool SyncFailureAlerts { get; set; } = true;
}
