namespace FinanceSentry.Tests.Unit.BrokerageSync;

using System.Text.Json;
using FinanceSentry.Modules.BrokerageSync.Infrastructure.IBKR;
using FluentAssertions;
using Xunit;

public class IBKRPerformanceContractTests
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    private const string FixtureJson = """
        {
          "nav": {
            "data": [
              { "date": "20240101", "nav": 15234.56 },
              { "date": "20240102", "nav": 15412.78 },
              { "date": "20240201", "nav": 16000.00 }
            ]
          }
        }
        """;

    [Fact]
    public void IBKRPerformanceResponse_DeserializesCorrectly()
    {
        var response = JsonSerializer.Deserialize<IBKRPerformanceResponse>(FixtureJson, Opts);

        response.Should().NotBeNull();
        response!.Nav.Should().NotBeNull();
        response.Nav.Data.Should().HaveCount(3);
    }

    [Fact]
    public void IBKRNavEntry_FieldsDeserializeCorrectly()
    {
        var response = JsonSerializer.Deserialize<IBKRPerformanceResponse>(FixtureJson, Opts)!;
        var first = response.Nav.Data[0];

        first.Date.Should().Be("20240101");
        first.Nav.Should().Be(15234.56m);
    }

    [Fact]
    public void IBKRNavEntry_DateStringIsEightCharacters()
    {
        var response = JsonSerializer.Deserialize<IBKRPerformanceResponse>(FixtureJson, Opts)!;

        foreach (var entry in response.Nav.Data)
        {
            entry.Date.Should().HaveLength(8, "IBKR date format is YYYYMMDD");
            _ = DateOnly.ParseExact(entry.Date, "yyyyMMdd");
        }
    }
}
