namespace FinanceSentry.Modules.Analytics.Application.Services;

using FinanceSentry.Modules.Analytics.API.Responses;

/// <summary>
/// Static description of the v1 curated views. MUST stay in lockstep with the view DDL in migration
/// M001 — the <c>CuratedSchemaTests</c> pin the shape and <c>describe_query_schema</c> serves it so the
/// agent writes correct SQL instead of guessing.
/// </summary>
public sealed class CuratedSchema : ICuratedSchema
{
    private static readonly QuerySchemaDto Schema = new(
    [
        new QueryViewDto("analytics.v_transactions", "Bank/card transactions (your data only)",
        [
            new QueryColumnDto("date", "date"),
            new QueryColumnDto("amount", "numeric"),
            new QueryColumnDto("currency", "text"),
            new QueryColumnDto("merchant", "text"),
            new QueryColumnDto("category", "text"),
            new QueryColumnDto("account_name", "text"),
            new QueryColumnDto("direction", "text"),
        ]),
        new QueryViewDto("analytics.v_holdings", "Current holdings across brokerage + crypto (your data only)",
        [
            new QueryColumnDto("symbol", "text"),
            new QueryColumnDto("asset_class", "text"),
            new QueryColumnDto("quantity", "numeric"),
            new QueryColumnDto("market_value_usd", "numeric"),
            new QueryColumnDto("cost_basis_usd", "numeric"),
            new QueryColumnDto("account", "text"),
        ]),
        new QueryViewDto("analytics.v_analyst_actions", "Street analyst actions (market-wide, not user-scoped)",
        [
            new QueryColumnDto("ticker", "text"),
            new QueryColumnDto("firm", "text"),
            new QueryColumnDto("action_type", "text"),
            new QueryColumnDto("prior_rating", "text"),
            new QueryColumnDto("new_rating", "text"),
            new QueryColumnDto("prior_target", "numeric"),
            new QueryColumnDto("new_target", "numeric"),
            new QueryColumnDto("action_date", "date"),
        ]),
        new QueryViewDto("analytics.v_net_worth_daily", "Net-worth history, one row per day (your data only)",
        [
            new QueryColumnDto("as_of_date", "date"),
            new QueryColumnDto("total_usd", "numeric"),
            new QueryColumnDto("banking_usd", "numeric"),
            new QueryColumnDto("brokerage_usd", "numeric"),
            new QueryColumnDto("crypto_usd", "numeric"),
        ]),
        new QueryViewDto("analytics.v_budgets", "Monthly budgets with current-month spend (your data only)",
        [
            new QueryColumnDto("category", "text"),
            new QueryColumnDto("period", "text"),
            new QueryColumnDto("limit_amount", "numeric"),
            new QueryColumnDto("spent_amount", "numeric"),
            new QueryColumnDto("remaining", "numeric"),
        ]),
    ]);

    public QuerySchemaDto Get() => Schema;
}
