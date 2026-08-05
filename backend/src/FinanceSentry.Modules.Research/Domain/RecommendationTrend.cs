namespace FinanceSentry.Modules.Research.Domain;

/// <summary>
/// One month of aggregate analyst consensus for one ticker (feature 037), from a structured
/// provider (Finnhub <c>/stock/recommendation</c>). Global market data — no <c>UserId</c>
/// (precedent: <see cref="AnalystAction"/>). Upserted by <c>(Ticker, Period)</c>: providers
/// restate recent months, so counts update in place while past periods accumulate the trend.
/// </summary>
public sealed class RecommendationTrend
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized upper-case canonical ticker (caller's symbol, not the provider echo).</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>First day of the consensus month.</summary>
    public DateOnly Period { get; set; }

    public int StrongBuy { get; set; }

    public int Buy { get; set; }

    public int Hold { get; set; }

    public int Sell { get; set; }

    public int StrongSell { get; set; }

    /// <summary>Originating provider — <c>finnhub</c>.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Last capture/update time.</summary>
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}
