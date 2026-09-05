namespace FinanceSentry.Modules.BankSync.Domain;

/// <summary>
/// Semantic roles a counterparty can play in the money flow. The role decides where each
/// direction of the movement lands: family support is real spending, investment routing is not
/// (the money is still the user's, it just changed sleeve).
/// </summary>
public static class FlowRoles
{
    /// <summary>
    /// Outbound is an expense and counts against the savings rate; inbound is income. Both
    /// sides count gross — support sent is not cancelled by rent received.
    /// </summary>
    public const string FamilySupport = "family_support";

    /// <summary>
    /// Outbound left the bank for an investment venue. It is not spending, so it stays out of
    /// outflow; it is carved out of what would otherwise read as idle cash. Inbound is capital
    /// coming back, not income, so it stays out of inflow too.
    /// </summary>
    public const string Investment = "investment";
}
