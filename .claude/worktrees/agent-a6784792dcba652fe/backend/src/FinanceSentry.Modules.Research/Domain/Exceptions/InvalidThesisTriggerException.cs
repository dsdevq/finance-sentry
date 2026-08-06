namespace FinanceSentry.Modules.Research.Domain.Exceptions;

using FinanceSentry.Core.Exceptions;

public class InvalidThesisTriggerException(string metric)
    : ApiException(422, "INVALID_THESIS_TRIGGER_METRIC", $"Unsupported invalidation trigger metric: '{metric}'.");
