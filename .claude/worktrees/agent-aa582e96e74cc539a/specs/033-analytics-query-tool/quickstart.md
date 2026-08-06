# Quickstart / Verification: Analytics Query Tool

Backend + MCP only. Against the Docker stack.

## Prereqs
- Stack up; health green.
- Migration `M001` in `__ef_migrations_history_analytics`; `analytics` schema has the curated views + `query_audit`; role `fs_readonly` exists with SELECT on the views only.

## US1 — guarded query
1. `describe_query_schema` → lists the curated views + columns, nothing else.
2. `run_analytics_query {"sql":"SELECT category, SUM(amount) total FROM analytics.v_transactions GROUP BY category ORDER BY total DESC"}` → rows + echoed SQL (SC-001).
3. `run_analytics_query` with `UPDATE`/`DELETE`/`DROP`/two statements → rejected, nothing mutated (SC-002).
4. With a second user's data present, run a query and confirm **only the caller's rows** come back — try to reference another user's id explicitly and confirm RLS still returns only the caller's (SC-003).
5. A deliberate runaway (`SELECT ... CROSS JOIN` or huge scan) → stopped by timeout/row cap with a narrow-it message; `truncated` true when clipped (SC-004).
6. Confirm the response echoes the exact SQL.

## US2 — schema discovery
- `describe_query_schema` matches the actual curated views; no raw internal tables exposed (SC-005).

## US3 — audit
- After the above, `analytics.query_audit` has one row per query (executed + rejected), with caller, SQL, outcome, row count, duration (SC-006).

## Boundary check
- Confirm the read-only connection uses `fs_readonly` and has **no** write grant (attempt a write directly as that role → permission denied). Confirm the app's normal connection is unaffected.
