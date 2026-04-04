namespace FinanceSentry.Modules.BankSync.Infrastructure.FeatureFlags;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Simple feature flag service backed by IConfiguration (appsettings / env vars).
/// Keys are read from "FeatureFlags:{KEY}" — e.g. "FeatureFlags:BANK_SYNC_ENABLED=true".
/// Supports gradual rollout by user ID hash when a percentage value (0-100) is configured.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>Returns true if the flag is enabled globally.</summary>
    bool IsEnabled(string flagKey);

    /// <summary>
    /// Returns true if the flag is enabled for a specific user.
    /// Supports percentage-based rollout: "FeatureFlags:BANK_SYNC_ENABLED=50" → enabled for 50% of users.
    /// </summary>
    bool IsEnabledForUser(string flagKey, Guid userId);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _config;

    public FeatureFlagService(IConfiguration config)
    {
        _config = config;
    }

    public bool IsEnabled(string flagKey)
    {
        var value = _config[$"FeatureFlags:{flagKey}"];
        if (value is null) return false;

        // Boolean flag: "true" / "false"
        if (bool.TryParse(value, out var boolVal))
            return boolVal;

        // Percentage flag: "100" means fully enabled globally
        if (int.TryParse(value, out var pct))
            return pct >= 100;

        return false;
    }

    public bool IsEnabledForUser(string flagKey, Guid userId)
    {
        var value = _config[$"FeatureFlags:{flagKey}"];
        if (value is null) return false;

        if (bool.TryParse(value, out var boolVal))
            return boolVal;

        // Percentage-based rollout: hash userId to 0-99, compare to threshold
        if (int.TryParse(value, out var pct))
        {
            var userBucket = Math.Abs(userId.GetHashCode() % 100);
            return userBucket < pct;
        }

        return false;
    }
}
