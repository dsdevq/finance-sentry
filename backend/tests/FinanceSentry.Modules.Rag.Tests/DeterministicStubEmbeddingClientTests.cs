namespace FinanceSentry.Modules.Rag.Tests;

using FinanceSentry.Modules.Rag.Infrastructure.Embeddings;
using FluentAssertions;
using Xunit;

public sealed class DeterministicStubEmbeddingClientTests
{
    private readonly DeterministicStubEmbeddingClient _client = new();

    [Fact]
    public async Task EmbedAsync_SameText_ReturnsSameVector()
    {
        var v1 = await _client.EmbedAsync("DRAM pricing cycle");
        var v2 = await _client.EmbedAsync("DRAM pricing cycle");

        v1.Should().BeEquivalentTo(v2);
    }

    [Fact]
    public async Task EmbedAsync_DifferentTexts_ReturnDifferentVectors()
    {
        var v1 = await _client.EmbedAsync("DRAM pricing cycle");
        var v2 = await _client.EmbedAsync("Fed interest rate decision");

        v1.Should().NotBeEquivalentTo(v2);
    }

    [Fact]
    public async Task EmbedAsync_Returns1024Dimensions()
    {
        var v = await _client.EmbedAsync("any text");

        v.Should().HaveCount(1024);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsUnitVector()
    {
        var v = await _client.EmbedAsync("any text");

        var norm = MathF.Sqrt(v.Sum(x => x * x));
        norm.Should().BeApproximately(1f, precision: 1e-5f);
    }

    [Fact]
    public async Task EmbedAsync_EmptyString_ReturnsUnitVector()
    {
        var v = await _client.EmbedAsync(string.Empty);

        v.Should().HaveCount(1024);
        var norm = MathF.Sqrt(v.Sum(x => x * x));
        norm.Should().BeApproximately(1f, precision: 1e-5f);
    }

    [Fact]
    public void Generate_IsStateless_SameResultAcrossInstances()
    {
        var v1 = DeterministicStubEmbeddingClient.Generate("MU earnings beat");
        var v2 = DeterministicStubEmbeddingClient.Generate("MU earnings beat");

        v1.Should().BeEquivalentTo(v2);
    }

    [Theory]
    [InlineData("ticker: MU")]
    [InlineData("ticker: NVDA")]
    [InlineData("memory supply tightening in Q3")]
    public async Task EmbedAsync_IsRoundTrippableDeterministic(string text)
    {
        var first = await _client.EmbedAsync(text);
        var second = await _client.EmbedAsync(text);

        first.Should().BeEquivalentTo(second, because: "same text must always produce same embedding");
    }
}
