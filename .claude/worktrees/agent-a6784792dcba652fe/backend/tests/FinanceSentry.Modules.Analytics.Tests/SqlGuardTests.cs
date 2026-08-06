namespace FinanceSentry.Modules.Analytics.Tests;

using FinanceSentry.Modules.Analytics.Application.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for the single-SELECT guard (feature 033, FR-005, SC-002). The guard is defense in depth;
/// each branch here is a statement the read-only role would also block — but rejecting before execution
/// is the contract.
/// </summary>
public sealed class SqlGuardTests
{
    private readonly SqlGuard _guard = new();

    [Theory]
    [InlineData("SELECT category, SUM(amount) FROM analytics.v_transactions GROUP BY category")]
    [InlineData("select * from analytics.v_holdings")]
    [InlineData("SELECT * FROM analytics.v_transactions WHERE amount > 100")]
    [InlineData("SELECT * FROM analytics.v_budgets;")] // single trailing semicolon allowed
    [InlineData("WITH recent AS (SELECT * FROM analytics.v_transactions) SELECT * FROM recent")]
    [InlineData("SELECT date, amount FROM analytics.v_transactions WHERE merchant = 'DELETE ME'")] // keyword only inside a string literal
    [InlineData("SELECT * FROM analytics.v_transactions -- drop everything\nWHERE amount > 0")] // forbidden word only in a comment
    public void Validate_AllowsReadOnlySelect(string sql)
    {
        _guard.Validate(sql).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("INSERT INTO analytics.v_transactions VALUES (1)")]
    [InlineData("UPDATE analytics.v_transactions SET amount = 0")]
    [InlineData("DELETE FROM analytics.v_transactions")]
    [InlineData("DROP VIEW analytics.v_transactions")]
    [InlineData("ALTER TABLE bank_sync.\"Transactions\" DROP COLUMN \"Amount\"")]
    [InlineData("CREATE TABLE x (id int)")]
    [InlineData("TRUNCATE analytics.query_audit")]
    [InlineData("GRANT SELECT ON analytics.v_holdings TO fs_readonly")]
    [InlineData("SELECT * FROM analytics.v_holdings FOR UPDATE")] // locking clause is not a pure read
    [InlineData("SET ROLE postgres")]
    public void Validate_RejectsWritesAndDdl(string sql)
    {
        var result = _guard.Validate(sql);
        result.IsValid.Should().BeFalse();
        result.Reason.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("SELECT 1; DROP TABLE analytics.query_audit")] // statement chaining
    [InlineData("SELECT 1; SELECT 2")]
    [InlineData("WITH moved AS (DELETE FROM analytics.query_audit RETURNING *) SELECT * FROM moved")] // data-modifying CTE
    public void Validate_RejectsMultiStatementAndDataModifyingCte(string sql)
    {
        _guard.Validate(sql).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("EXPLAIN SELECT 1")] // does not start with SELECT/WITH
    [InlineData("VALUES (1), (2)")]
    public void Validate_RejectsEmptyOrNonSelectLead(string? sql)
    {
        _guard.Validate(sql).IsValid.Should().BeFalse();
    }
}
