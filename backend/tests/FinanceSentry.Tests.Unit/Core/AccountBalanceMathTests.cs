namespace FinanceSentry.Tests.Unit.Core;

using FinanceSentry.Core.Utils;
using FluentAssertions;
using Xunit;

public class AccountBalanceMathTests
{
    [Theory]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("Credit")]
    public void IsLiability_CreditAnyCase_True(string accountType)
        => AccountBalanceMath.IsLiability(accountType).Should().BeTrue();

    [Theory]
    [InlineData("checking")]
    [InlineData("savings")]
    [InlineData("")]
    [InlineData(null)]
    public void IsLiability_NonCredit_False(string? accountType)
        => AccountBalanceMath.IsLiability(accountType).Should().BeFalse();

    [Fact]
    public void SignedForNetTotal_CreditAccount_Negated()
        => AccountBalanceMath.SignedForNetTotal("credit", 250m).Should().Be(-250m);

    [Fact]
    public void SignedForNetTotal_NonCreditAccount_Unchanged()
        => AccountBalanceMath.SignedForNetTotal("checking", 250m).Should().Be(250m);
}
