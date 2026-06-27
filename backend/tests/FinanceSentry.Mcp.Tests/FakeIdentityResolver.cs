using FinanceSentry.Mcp.Abstractions;

namespace FinanceSentry.Mcp.Tests;

/// <summary>
/// Test double — tests always pass an explicit userId, so the resolver never fires.
/// Returns null by default; set <see cref="ResolvedUserId"/> to assert defaulting behaviour.
/// </summary>
public sealed class FakeIdentityResolver : IIdentityResolver
{
    public Guid? ResolvedUserId { get; init; }
    public string? ResolvedEmail { get; init; }

    public Guid? GetUserId() => ResolvedUserId;
    public string? GetEmail() => ResolvedEmail;
    public bool IsConfigured => ResolvedUserId.HasValue;
}
