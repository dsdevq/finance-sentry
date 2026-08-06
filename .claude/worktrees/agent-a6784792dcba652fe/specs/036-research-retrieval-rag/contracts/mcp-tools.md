# MCP Tool Contracts: Research Retrieval and RAG Context

**Feature**: `036-research-retrieval-rag`

All tools live in `backend/src/FinanceSentry.Mcp/Tools/` and are thin wrappers over Research module CQRS handlers. Retrieval tools do not mutate user financial state.

## `search_research_corpus` (new)

Search stored Finance Sentry research documents. This is non-authoritative research context; current balances, holdings, exposure, tax lots, and risk verdicts must come from dedicated structured MCP tools.

**Request**:

| Param | Type | Required | Notes |
|---|---|---|---|
| `query` | string | yes | Natural-language search text |
| `tickers` | string[] | no | Filter to linked ticker symbols |
| `thesisId` | guid | no | Filter to one linked thesis |
| `sourceTypes` | string[] | no | Optional source filter: `NewsArticle`, `InvestmentThesis`, `DecisionNote`, `ThesisEvent`, `Postmortem`, `FilingExcerpt` |
| `from` | ISO timestamp | no | Publication/capture lower bound |
| `to` | ISO timestamp | no | Publication/capture upper bound |
| `limit` | int | no | Default 10, max 50 |

No `userId` parameter. User visibility comes from the authenticated MCP identity.

**Response**:

```json
{
  "query": "memory cycle recovery evidence",
  "results": [
    {
      "documentId": "9ad8b699-7b81-4761-b613-1f88c5d2a796",
      "chunkId": "df23491a-7623-4d2f-ad57-7401e8c65ffc",
      "sourceType": "NewsArticle",
      "sourceName": "TrendForce Press Center",
      "title": "DRAM contract prices continue to recover",
      "canonicalUrl": "https://example.com/article",
      "publishedAt": "2026-07-25T09:00:00Z",
      "capturedAt": "2026-07-25T09:31:00Z",
      "tickers": ["MU"],
      "thesisIds": ["0a6f3f6d-9de0-4c05-96fc-8e12d791bb2d"],
      "snippet": "Contract pricing improved for another quarter...",
      "semanticScore": 0.82,
      "lexicalScore": 0.41,
      "combinedScore": 0.73
    }
  ],
  "retrievedAt": "2026-07-26T12:00:00Z"
}
```

## `get_research_context` (new)

Build a bounded, cited context packet for a thesis or ticker. Use this before Ledger synthesizes research-heavy answers such as "what changed?", "what supports this thesis?", or "what breaks this thesis?". This is not the source for current portfolio/account numbers.

**Request**:

| Param | Type | Required | Notes |
|---|---|---|---|
| `thesisId` | guid | conditional | Required when `ticker` is omitted |
| `ticker` | string | conditional | Required when `thesisId` is omitted |
| `question` | string | no | Optional focusing question |
| `from` | ISO timestamp | no | Freshness lower bound for supporting chunks |
| `maxChunks` | int | no | Default 12, max 30 |
| `includeSourceTypes` | string[] | no | Optional source type allow-list |

No `userId` parameter. User visibility comes from the authenticated MCP identity.

**Response**:

```json
{
  "subjectType": "Thesis",
  "subjectId": "0a6f3f6d-9de0-4c05-96fc-8e12d791bb2d",
  "ticker": "MU",
  "thesis": {
    "id": "0a6f3f6d-9de0-4c05-96fc-8e12d791bb2d",
    "title": "MU memory cycle recovery",
    "summary": "DRAM pricing recovery and operating leverage..."
  },
  "groups": [
    {
      "name": "recent_news",
      "items": [
        {
          "documentId": "9ad8b699-7b81-4761-b613-1f88c5d2a796",
          "chunkId": "df23491a-7623-4d2f-ad57-7401e8c65ffc",
          "title": "DRAM contract prices continue to recover",
          "canonicalUrl": "https://example.com/article",
          "publishedAt": "2026-07-25T09:00:00Z",
          "snippet": "Contract pricing improved for another quarter...",
          "combinedScore": 0.73
        }
      ]
    }
  ],
  "omittedCount": 8,
  "retrievedAt": "2026-07-26T12:00:00Z"
}
```

## Tool Surface Contract Update

Add:

- `search_research_corpus`
- `get_research_context`

Existing tools remain available. The tool-name contract test should increase from 55 to 57 unless another branch changes the surface first.
