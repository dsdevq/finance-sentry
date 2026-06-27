namespace FinanceSentry.Mcp.Abstractions;

public interface IReadOnlyMcpTool
{
    string ToolName { get; }
    bool IsReadOnly => true;
}
