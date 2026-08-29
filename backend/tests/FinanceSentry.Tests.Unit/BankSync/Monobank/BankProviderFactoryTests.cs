namespace FinanceSentry.Tests.Unit.BankSync.Monobank;

using FinanceSentry.Modules.BankSync.Application.Services;
using FinanceSentry.Modules.BankSync.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for BankProviderFactory (T035): resolves the registered provider
/// by name and throws for unknown providers.
/// </summary>
public class BankProviderFactoryTests
{
    private static IBankProvider StubProvider(string name)
    {
        var mock = new Mock<IBankProvider>(MockBehavior.Strict);
        mock.SetupGet(p => p.ProviderName).Returns(name);
        return mock.Object;
    }

    private static BankProviderFactory CreateSut(out IBankProvider truelayer, out IBankProvider monobank)
    {
        truelayer = StubProvider("truelayer");
        monobank = StubProvider("monobank");
        return new BankProviderFactory([truelayer, monobank]);
    }

    [Fact]
    public void Resolve_TrueLayer_ReturnsTrueLayerProvider()
    {
        var sut = CreateSut(out var truelayer, out _);

        sut.Resolve("truelayer").Should().BeSameAs(truelayer);
    }

    [Fact]
    public void Resolve_Monobank_ReturnsMonobankProvider()
    {
        var sut = CreateSut(out _, out var monobank);

        sut.Resolve("monobank").Should().BeSameAs(monobank);
    }

    [Fact]
    public void Resolve_UnknownProvider_Throws()
    {
        var sut = CreateSut(out _, out _);

        var act = () => sut.Resolve("revolut");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revolut*");
    }
}
