using FinanceSentry.Core.Cqrs;
using FinanceSentry.Mcp.Abstractions;
using FinanceSentry.Modules.Auth.Application.Commands;

namespace FinanceSentry.Mcp.Tools.Identity;

public sealed class GetCurrentUserTool(IQueryHandler<GetProfileQuery, UserProfileDto> handler) : IMcpTool
{
    public string Name => "get_current_user";

    public string Description => "Returns the authenticated user's id, email, and display name from the Auth module.";

    public bool IsReadOnly => true;

    public bool IsStub => false;

    public async Task<McpToolResult<object>> InvokeAsync(
        IReadOnlyDictionary<string, object?> args,
        McpToolContext context,
        CancellationToken ct)
    {
        try
        {
            var profile = await handler.Handle(new GetProfileQuery(context.UserId), ct);
            var displayName = $"{profile.FirstName} {profile.LastName}".Trim();
            return McpToolResult.Success(new UserPayload(context.UserId, profile.Email, displayName));
        }
        catch (Exception ex)
        {
            return McpToolResult.Error(ex.Message);
        }
    }

    private record UserPayload(Guid UserId, string Email, string DisplayName);
}
