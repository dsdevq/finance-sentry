using FinanceSentry.Modules.BrokerageSync.Domain;
using FluentAssertions;
using Xunit;

namespace FinanceSentry.Tests.Unit.BrokerageSync;

public sealed class IBKRCredentialSyncStateTests
{
    [Fact]
    public void RecordSyncError_DoesNotAdvanceLastSyncAt()
    {
        var credential = MakeCredential();

        credential.RecordSyncError("timeout");

        credential.LastSyncAt.Should().BeNull();
        credential.LastSyncError.Should().Be("timeout");
    }

    [Fact]
    public async Task RecordSyncSuccess_ClearsPriorErrorAndAdvancesLastSyncAt()
    {
        var credential = MakeCredential();
        credential.RecordSyncError("timeout");

        await Task.Delay(1);
        credential.RecordSyncSuccess();

        credential.LastSyncAt.Should().NotBeNull();
        credential.LastSyncError.Should().BeNull();
    }

    private static IBKRCredential MakeCredential()
    {
        var credential = new IBKRCredential(
            Guid.NewGuid(),
            "FINSENTRY",
            "access-token",
            "dh-pem",
            [1],
            [2],
            [3],
            [4],
            [5],
            [6],
            [7],
            [8],
            [9],
            keyVersion: 1);
        credential.UpdateAccountId("U1234567");
        return credential;
    }
}
