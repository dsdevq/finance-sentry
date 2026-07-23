namespace FinanceSentry.Tests.Unit.BankSync.Infrastructure;

using FinanceSentry.Modules.BankSync.Domain;
using FinanceSentry.Modules.BankSync.Infrastructure.Categorization;
using FluentAssertions;
using Xunit;

public class TransferDescriptionClassifierTests
{
    [Theory]
    [InlineData("To Mario Scalas", CategoryKeys.TransferOut)]
    [InlineData("To Denys Sychov", CategoryKeys.TransferOut)]
    [InlineData("To Interactive Brokers LLC", CategoryKeys.TransferOut)]
    [InlineData("To Instant Access Savings", CategoryKeys.TransferOut)]
    [InlineData("From Instant Access Savings", CategoryKeys.TransferIn)]
    [InlineData("From investment account", CategoryKeys.TransferIn)]
    [InlineData("Payment from Andrea Di Florio", CategoryKeys.TransferIn)]
    public void Resolve_DirectionalPrefix_ClassifiesTransfer(string description, string expected)
    {
        TransferDescriptionClassifier.Resolve(description).Should().Be(expected);
    }

    [Theory]
    [InlineData("Tobacco Shop")]      // "To" without the trailing space must not match
    [InlineData("Tommy Hilfiger")]
    [InlineData("Lidl Ireland Ltd")]
    [InlineData("Fromage Cheese Co")] // "From" without the trailing space must not match
    [InlineData("")]
    [InlineData(null)]
    public void Resolve_NonTransfer_ReturnsNull(string? description)
    {
        TransferDescriptionClassifier.Resolve(description).Should().BeNull();
    }

    [Fact]
    public void Resolve_LeadingWhitespace_IsTrimmedBeforeMatching()
    {
        TransferDescriptionClassifier.Resolve("   To Someone").Should().Be(CategoryKeys.TransferOut);
    }
}
