namespace FinanceSentry.Modules.Analytics.Application.Services;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Single-<c>SELECT</c> validator (FR-005). Strategy: strip comments and string/identifier literals so
/// keywords inside them can't trigger false positives or hide an injection, then require the statement
/// to (a) be exactly one statement, (b) start with <c>SELECT</c> or <c>WITH</c>, and (c) contain no
/// write/DDL/transaction-control keyword anywhere (which also blocks data-modifying CTEs and
/// <c>SELECT … FOR UPDATE</c>). This is the second layer; the read-only role is the first.
/// </summary>
public sealed partial class SqlGuard : ISqlGuard
{
    private const string RejectReason =
        "only a single read-only SELECT over the curated analytics views is allowed";

    // Anything that writes, changes schema, controls transactions, or otherwise isn't a pure read.
    private static readonly IReadOnlySet<string> ForbiddenKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "MERGE", "UPSERT",
        "DROP", "ALTER", "CREATE", "TRUNCATE", "RENAME", "COMMENT",
        "GRANT", "REVOKE", "COPY", "IMPORT",
        "CALL", "DO", "EXECUTE", "PREPARE", "DEALLOCATE",
        "VACUUM", "ANALYZE", "REINDEX", "REFRESH", "CLUSTER", "LOCK",
        "SET", "RESET", "SHOW",
        "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "START",
        "LISTEN", "NOTIFY", "UNLISTEN", "DECLARE", "FETCH", "MOVE",
    };

    public SqlGuardResult Validate(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlGuardResult.Invalid(RejectReason);
        }

        var sanitized = StripLiteralsAndComments(sql).Trim();

        // Allow a single trailing semicolon; reject any interior one (statement chaining).
        sanitized = sanitized.TrimEnd(';', ' ', '\t', '\r', '\n');
        if (sanitized.Contains(';'))
        {
            return SqlGuardResult.Invalid(RejectReason);
        }

        if (sanitized.Length == 0)
        {
            return SqlGuardResult.Invalid(RejectReason);
        }

        // First token must be SELECT or WITH.
        var firstToken = FirstWordRegex().Match(sanitized).Value;
        if (!firstToken.Equals("SELECT", StringComparison.OrdinalIgnoreCase)
            && !firstToken.Equals("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return SqlGuardResult.Invalid(RejectReason);
        }

        foreach (Match word in WordRegex().Matches(sanitized))
        {
            if (ForbiddenKeywords.Contains(word.Value))
            {
                return SqlGuardResult.Invalid(RejectReason);
            }
        }

        return SqlGuardResult.Valid;
    }

    /// <summary>
    /// Replaces <c>--</c> and <c>/* */</c> comments and the contents of single-quoted strings and
    /// double-quoted identifiers with spaces, so downstream keyword scanning sees only bare SQL.
    /// </summary>
    private static string StripLiteralsAndComments(string sql)
    {
        var sb = new StringBuilder(sql.Length);
        var i = 0;
        while (i < sql.Length)
        {
            var c = sql[i];

            // Line comment.
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                {
                    i++;
                }
                continue;
            }

            // Block comment.
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                {
                    i++;
                }
                i += 2;
                sb.Append(' ');
                continue;
            }

            // Single-quoted string literal (handles '' escape).
            if (c == '\'')
            {
                i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i += 2;
                        continue;
                    }
                    if (sql[i] == '\'')
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                sb.Append(" '' ");
                continue;
            }

            // Double-quoted identifier (handles "" escape).
            if (c == '"')
            {
                i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '"' && i + 1 < sql.Length && sql[i + 1] == '"')
                    {
                        i += 2;
                        continue;
                    }
                    if (sql[i] == '"')
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                sb.Append(" id ");
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex FirstWordRegex();

    [GeneratedRegex(@"\b[A-Za-z_][A-Za-z0-9_]*\b")]
    private static partial Regex WordRegex();
}
