namespace FinanceSentry.Modules.Research.Application.Services;

/// <summary>
/// Pure return-math contract (SC-001) — no I/O, deterministic: identical inputs always produce
/// identical outputs.
/// </summary>
public interface IThesisPerformanceCalculator
{
    ThesisPerformanceResult Calculate(ThesisPerformanceInput input);
}
