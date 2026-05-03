namespace FinanceSentry.Tests.Unit.CryptoSync;

using System.Text.Json;
using FinanceSentry.Modules.CryptoSync.Infrastructure.Binance;
using FluentAssertions;
using Xunit;

public class BinanceAccountSnapshotContractTests
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    private const string FixtureJson = """
        {
          "code": 200,
          "msg": "",
          "snapshotVos": [
            {
              "type": "spot",
              "updateTime": 1640966400000,
              "data": {
                "totalAssetOfBtc": "0.12345",
                "balances": [
                  { "asset": "BTC", "free": "0.1", "locked": "0.02345" },
                  { "asset": "ETH", "free": "2.5", "locked": "0.0" }
                ]
              }
            },
            {
              "type": "spot",
              "updateTime": 1641052800000,
              "data": {
                "totalAssetOfBtc": "0.13000",
                "balances": [
                  { "asset": "BTC", "free": "0.13", "locked": "0.0" }
                ]
              }
            }
          ]
        }
        """;

    [Fact]
    public void BinanceSnapshotResponse_DeserializesCorrectly()
    {
        var response = JsonSerializer.Deserialize<BinanceSnapshotResponse>(FixtureJson, Opts);

        response.Should().NotBeNull();
        response!.Code.Should().Be(200);
        response.SnapshotVos.Should().HaveCount(2);
    }

    [Fact]
    public void BinanceSnapshotVo_FieldsDeserializeCorrectly()
    {
        var response = JsonSerializer.Deserialize<BinanceSnapshotResponse>(FixtureJson, Opts)!;
        var first = response.SnapshotVos[0];

        first.Type.Should().Be("spot");
        first.UpdateTime.Should().Be(1640966400000L);
        first.Data.TotalAssetOfBtc.Should().Be("0.12345");
    }

    [Fact]
    public void BinanceSnapshotData_BalancesDeserializeCorrectly()
    {
        var response = JsonSerializer.Deserialize<BinanceSnapshotResponse>(FixtureJson, Opts)!;
        var balances = response.SnapshotVos[0].Data.Balances;

        balances.Should().HaveCount(2);
        balances[0].Asset.Should().Be("BTC");
        balances[0].Free.Should().Be("0.1");
        balances[0].Locked.Should().Be("0.02345");
        balances[1].Asset.Should().Be("ETH");
    }
}
