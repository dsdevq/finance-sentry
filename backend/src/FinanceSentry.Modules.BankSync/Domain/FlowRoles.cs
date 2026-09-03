namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Semantic roles a counterparty can play in the money flow. The role decides where the
/// netted movement lands: family support is real spending, investment routing is not
/// (the money is still the user's, it just changed sleeve).
/// </summary>
public static class FlowRoles
{
    /// <summary>Net outbound is an expense and counts against the savings rate.</summary>
    public const string FamilySupport = "family_support";

    /// <summary>
    /// Net outbound left the bank for an investment venue. It is not spending, so it stays
    /// out of outflow; it is carved out of what would otherwise read as idle cash.
    /// </summary>
    public const string Investment = "investment";
}
