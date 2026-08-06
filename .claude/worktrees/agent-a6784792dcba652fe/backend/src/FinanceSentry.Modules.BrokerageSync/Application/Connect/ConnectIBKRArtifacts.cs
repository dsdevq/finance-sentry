namespace FinanceSentry.Modules.BrokerageSync.Application.Connect;

/// <summary>
/// The six OAuth 1.0a self-service artifacts a user brings from IBKR's portal.
/// <see cref="ConsumerKey"/>, <see cref="AccessToken"/> and <see cref="DhParam"/>
/// are stored in the clear; the remaining three are encrypted at rest.
/// </summary>
public sealed record ConnectIBKRArtifacts(
    string ConsumerKey,
    string AccessToken,
    string AccessTokenSecret,
    string SignatureKey,
    string EncryptionKey,
    string DhParam);
