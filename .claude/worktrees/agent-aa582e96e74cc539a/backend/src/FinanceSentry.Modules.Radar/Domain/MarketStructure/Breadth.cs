namespace FinanceSentry.Modules.Radar.Domain.MarketStructure;

/// <summary>Pure breadth math: % of the universe trading above its 20/50/200-day MAs.</summary>
public static class Breadth
{
    /// <summary>One ticker's close and its three MAs (null when insufficient history).</summary>
    public readonly record struct TickerMaState(decimal Close, decimal? Ma20, decimal? Ma50, decimal? Ma200);

    /// <summary>
    /// Percentage of tickers whose close is above each MA, evaluated only over tickers where that MA
    /// exists. <c>Evaluated</c> is the count of tickers with at least one evaluable MA.
    /// </summary>
    public static BreadthResult Compute(IReadOnlyCollection<TickerMaState> states)
    {
        if (states.Count == 0)
        {
            return new BreadthResult(null, null, null, 0);
        }

        var evaluated = states.Count(s => s.Ma20 is not null || s.Ma50 is not null || s.Ma200 is not null);

        return new BreadthResult(
            Pct(states, s => s.Ma20),
            Pct(states, s => s.Ma50),
            Pct(states, s => s.Ma200),
            evaluated);
    }

    private static decimal? Pct(IReadOnlyCollection<Breadth.TickerMaState> states, Func<TickerMaState, decimal?> ma)
    {
        var evaluable = states.Where(s => ma(s) is not null).ToArray();
        if (evaluable.Length == 0)
        {
            return null;
        }

        var above = evaluable.Count(s => s.Close > ma(s)!.Value);
        return (decimal)above / evaluable.Length;
    }
}
