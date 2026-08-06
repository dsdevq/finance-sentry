# External API Contract: Finnhub `/stock/recommendation`

**Provider**: Finnhub (finnhub.io) · **Tier**: free · **Docs**: <https://finnhub.io/docs/api/recommendation-trends>
**Contract-test obligation** (constitution, Testing Discipline #1): a recorded-fixture test validates this shape on every CI run; a live smoke test (skipped unless `FINNHUB_API_KEY` is set) validates the real API still conforms.

## Request

```
GET {BaseUrl}/stock/recommendation?symbol={TICKER}
X-Finnhub-Token: {ApiKey}
```

- Auth: **header only** in our client (`X-Finnhub-Token`). The `token` query-param alternative is forbidden here — keys must never appear in URLs/logs (Principle V).
- Rate limits (free): 60 calls/min, 30 calls/sec global hard cap. Client paces to `RequestsPerMinute` (default 50).

## Response — 200

JSON array, newest month first. Zero-coverage tickers return `[]` (expected — Debug log, not Warning; FR-003).

```json
[
  {
    "symbol": "MU",
    "period": "2026-08-01",
    "strongBuy": 14,
    "buy": 20,
    "hold": 6,
    "sell": 1,
    "strongSell": 0
  },
  {
    "symbol": "MU",
    "period": "2026-07-01",
    "strongBuy": 13,
    "buy": 21,
    "hold": 6,
    "sell": 1,
    "strongSell": 0
  }
]
```

| Field | Type | Mapping → `RecommendationTrend` |
|---|---|---|
| `symbol` | string | echoed; record keeps the caller's canonical ticker (do not trust echo casing) |
| `period` | string `YYYY-MM-01` | `Period` (`DateOnly`) |
| `strongBuy`/`buy`/`hold`/`sell`/`strongSell` | int | same-named columns |

Parsing tolerances (contract-test assertions):
- Unknown extra fields → ignored.
- Missing count field → treated as 0.
- Malformed `period` → row skipped (Debug), never throws for a single bad row.
- Non-array root / HTML error body → `AnalystSourceParseException` (visible failure → health strike).

## Error semantics

| Status | Client behavior |
|---|---|
| 401 / 403 | Throw `AnalystSourceParseException` ("invalid key / endpoint moved behind paywall") → health strike → 2-strike Telegram alert. **Never** silent. |
| 429 | One bounded backoff-retry (respect ~60s window); then skip ticker with Debug log. |
| 5xx / timeout | Skip ticker (Debug); whole-run failure only if *every* ticker fails. |

## Fixture

`backend/tests/FinanceSentry.Modules.Research.Tests/Fixtures/finnhub-recommendation.json` — recorded from a real free-tier response at implementation time (strip nothing; the fixture is the contract snapshot).
