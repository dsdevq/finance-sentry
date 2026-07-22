namespace FinanceSentry.Modules.Companion.Domain;

/// <summary>The source class of a captured material event (feature 031).</summary>
public enum CompanionEventKind
{
    RiskViolation,
    SyncFailure,
    UnusualSpend,
    Opportunity,
    ThesisBreak,
    AnalystAction,
}
