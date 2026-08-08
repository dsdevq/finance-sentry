namespace FinanceSentry.Modules.Agent.Domain;

/// <summary>The three roles a persisted <see cref="Message"/> can carry.</summary>
public enum MessageRole
{
    User,
    Assistant,
    Tool,
}
