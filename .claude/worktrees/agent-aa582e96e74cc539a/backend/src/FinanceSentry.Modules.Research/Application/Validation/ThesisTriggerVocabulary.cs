namespace FinanceSentry.Modules.Research.Application.Validation;

using FinanceSentry.Modules.Research.Domain;
using FinanceSentry.Modules.Research.Domain.Exceptions;
using FinanceSentry.Modules.Research.Domain.ThesisMonitor;

/// <summary>
/// Guards that every invalidation trigger's <c>Metric</c> belongs to the closed
/// <see cref="ThesisMetric"/> vocabulary (FR-012). Invoked at save time.
/// </summary>
public static class ThesisTriggerVocabulary
{
    public static void Validate(IReadOnlyList<ThesisInvalidationTrigger> triggers)
    {
        foreach (var trigger in triggers)
        {
            if (!ThesisMetric.Contains(trigger.Metric))
            {
                throw new InvalidThesisTriggerException(trigger.Metric);
            }
        }
    }
}
