namespace FinanceSentry.Core.Api;

using System.Text.Json.Serialization;

public record ApiErrorBody(string Error, string ErrorCode)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Details { get; init; }

    public static ApiErrorBody From(string error, string errorCode, IEnumerable<string>? details = null)
    {
        var list = details?.ToList();
        return new ApiErrorBody(error, errorCode) { Details = list is { Count: > 0 } ? list : null };
    }
}
