namespace FinanceSentry.Modules.Risk.Domain.Exceptions;

public sealed class RiskRuleSetNotFoundException(Guid userId)
    : Exception($"No risk rule set on file for user {userId}.");
