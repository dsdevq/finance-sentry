using System.Text.Json.Serialization;
using FinanceSentry.Modules.Research.API.Responses;

namespace FinanceSentry.Mcp.Responses;

/// <summary>
/// Union-shaped result for the merged <c>watchlist</c> tool (feature 035). Only the members relevant to
/// the requested action are populated; null members are omitted from the JSON so each action returns a
/// clean shape. Reuses the Research module's <see cref="WatchlistItemDto"/> — no new item shape.
/// </summary>
public sealed record WatchlistToolResult
{
    /// <summary>Echoes the requested action: <c>list</c> | <c>add</c> | <c>remove</c>.</summary>
    public required string Action { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<WatchlistItemDto>? Items { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WatchlistItemDto? Item { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Removed { get; init; }

    /// <summary>Populated when the call was malformed (e.g. <c>add</c> without a ticker).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    public static WatchlistToolResult ForList(IReadOnlyList<WatchlistItemDto> items)
        => new() { Action = "list", Items = items };

    public static WatchlistToolResult ForAdd(WatchlistItemDto item)
        => new() { Action = "add", Item = item };

    public static WatchlistToolResult ForRemove(bool removed)
        => new() { Action = "remove", Removed = removed };

    public static WatchlistToolResult Invalid(string action, string reason)
        => new() { Action = action, Error = reason };
}
