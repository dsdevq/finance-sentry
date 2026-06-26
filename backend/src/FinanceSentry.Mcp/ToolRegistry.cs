namespace FinanceSentry.Mcp;

public sealed class ToolRegistry
{
    private readonly List<string> _toolNames = [];

    public IReadOnlyList<string> ToolNames => _toolNames.AsReadOnly();

    public void Register(string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        _toolNames.Add(toolName);
    }
}
