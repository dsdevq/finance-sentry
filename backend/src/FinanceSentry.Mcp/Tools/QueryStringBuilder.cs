namespace FinanceSentry.Mcp.Tools;

using System.Text;

internal sealed class QueryStringBuilder
{
    private readonly List<KeyValuePair<string, string>> _parameters = [];

    public static QueryStringBuilder Create()
    {
        return new QueryStringBuilder();
    }

    public QueryStringBuilder Add(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _parameters.Add(new KeyValuePair<string, string>(name, value));
        }

        return this;
    }

    public override string ToString()
    {
        if (_parameters.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("?");

        for (var index = 0; index < _parameters.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('&');
            }

            var parameter = _parameters[index];
            builder
                .Append(Uri.EscapeDataString(parameter.Key))
                .Append('=')
                .Append(Uri.EscapeDataString(parameter.Value));
        }

        return builder.ToString();
    }
}
