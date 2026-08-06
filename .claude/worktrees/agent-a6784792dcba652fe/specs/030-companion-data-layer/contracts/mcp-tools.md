# MCP Tool Contracts: Companion-Mode Data Layer

**Feature**: 030-companion-data-layer. All tools live in `FinanceSentry.Mcp/Tools/`, `[McpServerToolType]` + `[McpServerTool(Name = ...)]`, thin over CQRS handlers. No REST endpoints in this feature.

## `get_analyst_actions` (new)

Query analyst/street actions. Market-wide — ticker is optional.

**Request**:
| Param | Type | Required | Notes |
|---|---|---|---|
| `ticker` | string | no | filter to one ticker |
| `since` | ISO date | no | default: 30 days back |
| `actionType` | string | no | `Upgrade`\|`Downgrade`\|`Initiate`\|`TargetChange`\|`Reiterate`\|`TopIdea` |
| `limit` | int | no | default 50, max 200 |

**Response** (list):
```json
{
  "ticker": "MU",
  "firm": "Morgan Stanley",
  "actionType": "Upgrade",
  "priorRating": "Equal-Weight",
  "newRating": "Overweight",
  "priorTarget": 98.0,
  "newTarget": 135.0,
  "actionDate": "2026-07-20",
  "source": "marketbeat",
  "sourceUrl": "https://www.marketbeat.com/ratings/",
  "ingestedAt": "2026-07-21T01:04:12Z"
}
```
Plus envelope fields: `coverage` — `"inUniverse"` | `"notInUniverse"` | `"marketWide"` (distinguishes "no coverage in universe" from "no recent actions" per spec edge case), `retrievedAt`.

## `get_valuation_snapshot` (new)

**Request**:
| Param | Type | Required | Notes |
|---|---|---|---|
| `ticker` | string | yes | equities only; crypto → explicit `notApplicable` |
| `peers` | string[] | no | override default sector/industry peer set |

**Response**:
```json
{
  "ticker": "MCD",
  "price": 265.1,
  "isStale": false,
  "metrics": {
    "trailingPe": { "value": 24.1, "fiveYearAvg": 26.0, "historyWindowYears": 5 },
    "forwardPe": { "value": 21.6, "fiveYearAvg": null, "historyUnavailable": true },
    "evToEbitda": { "value": 16.6, "fiveYearAvg": null, "historyUnavailable": true },
    "dividendYield": { "value": 0.0295, "fiveYearAvg": null, "historyUnavailable": true }
  },
  "consensusTarget": 336.0,
  "impliedUpsidePct": 26.7,
  "peerSet": { "name": "sector:Consumer Cyclical (default)", "peers": [
    { "ticker": "YUM", "forwardPe": 24.2, "evToEbitda": 19.1 }
  ]},
  "sources": ["yahoo:quoteSummary", "sec-edgar:xbrl"],
  "retrievedAt": "2026-07-21T18:40:00Z"
}
```
Rules: missing metric → `value: null` + reason flag; NEVER zero-filled (FR-006). Every response persists a `valuation_snapshots` row.

## `register_thesis_source` (new)

**Request**: `thesisId` (guid, required), `name` (string), `url` (string), `kind` (`Rss`|`Page`), `keywords` (string[], optional). Registers a source; `thesisId` omitted/null registers a market-wide source (Denys-only decision — tool description will say Ledger must have his confirmation, mirroring `acknowledge_risk_violation` phrasing).
**Response**: `{ "sourceId": "...", "enabled": true }`

## `list_news_sources` (new)

**Request**: none. **Response**: all sources with `id, name, kind, url, keywords, thesisId, enabled, consecutiveFailures, lastSuccessAt, lastFailureReason` — Ledger can see source health directly.

## `search_market_news` (extended)

New optional param `thesisId` (guid) — filters to articles tagged with that thesis (`ThesisIds` column). Existing params unchanged (backward-compatible).

## `add_candidate` / candidate tools (extended)

`CandidateSource` gains `Ledger`. Whichever existing tool creates candidates accepts `source: "Ledger"`; `list_candidates` output already includes source — no shape change.

## External source contracts (contract tests required per constitution)

| Source | Contract asserted |
|---|---|
| Yahoo `quoteSummary?modules=upgradeDowngradeHistory` | JSON path `quoteSummary.result[0].upgradeDowngradeHistory.history[]` with `firm`, `toGrade`, `fromGrade`, `action`, `epochGradeDate` |
| Yahoo `quoteSummary?modules=summaryDetail,defaultKeyStatistics,financialData` | fields: `trailingPE`, `forwardPE`, `dividendYield`, `enterpriseValue`, `ebitda`, `targetMeanPrice` (each optional-tolerant) |
| MarketBeat `/ratings/` | HTML table present with columns {company/ticker, action, brokerage, rating change, price target}; fixture-based + explicit live smoke |
| TrendForce press center | article list selector yields (title, url, date); fixture-based |
