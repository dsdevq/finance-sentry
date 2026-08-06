namespace FinanceSentry.Infrastructure.Observability;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Adds a bounded <c>module</c> property derived from <c>SourceContext</c> so logs can be filtered by
/// coarse module in Loki/Grafana (US2) without promoting the full, high-cardinality type name as a label.
/// e.g. <c>FinanceSentry.Modules.BankSync.Foo</c> → <c>BankSync</c>; <c>FinanceSentry.API.*</c> → <c>API</c>.
/// </summary>
public sealed class ModuleEnricher : ILogEventEnricher
{
    private const string ModulesMarker = "FinanceSentry.Modules.";
    private const string RootMarker = "FinanceSentry.";
    private const string Fallback = "app";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey("module"))
            return;

        var module = Fallback;
        if (logEvent.Properties.TryGetValue("SourceContext", out var value)
            && value is ScalarValue { Value: string sourceContext })
        {
            module = DeriveModule(sourceContext);
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("module", module));
    }

    private static string DeriveModule(string sourceContext)
    {
        if (sourceContext.StartsWith(ModulesMarker, StringComparison.Ordinal))
            return FirstSegment(sourceContext[ModulesMarker.Length..]);
        if (sourceContext.StartsWith(RootMarker, StringComparison.Ordinal))
            return FirstSegment(sourceContext[RootMarker.Length..]);
        return Fallback;
    }

    private static string FirstSegment(string value)
    {
        var dot = value.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? value : value[..dot];
    }
}
