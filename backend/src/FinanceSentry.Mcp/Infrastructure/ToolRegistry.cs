namespace FinanceSentry.Mcp.Infrastructure;

using FinanceSentry.Mcp.Domain;

public sealed class ToolRegistry
{
    private readonly IReadOnlyList<IMcpTool> _tools;
    private readonly Dictionary<string, IMcpTool> _byName;

    public ToolRegistry(IEnumerable<IMcpTool> tools)
    {
        _tools = tools.ToList().AsReadOnly();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IMcpTool> GetAll() => _tools;

    public IMcpTool? GetTool(string name) =>
        _byName.TryGetValue(name, out var tool) ? tool : null;
}
