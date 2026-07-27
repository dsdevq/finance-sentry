# Implementation Plan: Research Retrieval and RAG Context for Ledger

**Branch**: `036-research-retrieval-rag` | **Date**: 2026-07-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/036-research-retrieval-rag/spec.md`

## Summary

Add a DB-backed retrieval layer inside `FinanceSentry.Modules.Research` and expose it through two MCP tools. The retrieval layer indexes stored research documents into chunks, stores embeddings, runs hybrid semantic/lexical search, enforces user isolation, and returns cited context packets for Ledger. It deliberately does not replace authoritative finance tools or add live internet browsing.

## Technical Context

**Language/Version**: C# 13 / .NET 9
**Primary Dependencies**: Existing CQRS module pattern, EF Core, PostgreSQL; new dependency likely `pgvector` support for PostgreSQL/Npgsql plus a configurable embedding provider implementation
**Storage**: PostgreSQL Research schema; new migration `M009_ResearchRetrieval`
**Testing**: xUnit + FluentAssertions; EF InMemory for pure behavior; PostgreSQL integration tests for vector/hybrid retrieval and user isolation where needed; MCP contract tests
**Target Platform**: Linux server and Docker; MCP over stdio + streamable HTTP
**Project Type**: Backend Research module + MCP host
**Performance Goals**: Context packet generation should stay bounded by configured `MaxChunks`/`MaxCharacters`; indexing is asynchronous and idempotent
**Constraints**: zero-warning backend build; strict per-user isolation; no prompt-enforced data boundaries; no frontend changes
**Scale/Scope**: Initial corpus is personal scale: news, filings/excerpts, thesis notes, decision journals, postmortems, and registered source content

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Modular monolith / integration isolation**: PASS - retrieval lives in Research module; MCP tools remain thin adapters over CQRS handlers.
- **II. Code quality**: PASS - C# changes must keep `dotnet build backend/` at zero warnings.
- **III. Multi-source financial integration**: PASS - source fetching remains in existing ingestion services; retrieval indexes normalized stored data.
- **IV. AI-driven analytics**: PASS - adds AI-ready context infrastructure while keeping LLM/provider code behind interfaces.
- **V. Security-first financial data handling**: PASS WITH CARE - user isolation is a core requirement; retrieval tools must not accept arbitrary `userId` overrides.
- **VI. Frontend discipline**: N/A - backend-only feature.

No intentional constitution violations.

## Project Structure

### Documentation (this feature)

```text
specs/036-research-retrieval-rag/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── mcp-tools.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
backend/src/FinanceSentry.Modules.Research/
├── Domain/
│   ├── ResearchDocument.cs
│   ├── ResearchChunk.cs
│   ├── ResearchEmbedding.cs
│   ├── ResearchDocumentSourceType.cs
│   └── Repositories/
│       ├── IResearchDocumentRepository.cs
│       └── IResearchRetrievalRepository.cs
├── Application/
│   ├── Commands/
│   │   └── IndexResearchDocumentsCommand.cs
│   ├── Queries/
│   │   ├── SearchResearchCorpusQuery.cs
│   │   └── GetResearchContextQuery.cs
│   └── Services/
│       ├── IEmbeddingService.cs
│       ├── IResearchChunker.cs
│       ├── IResearchIndexer.cs
│       ├── IResearchRetriever.cs
│       └── ResearchRetrievalOptions.cs
├── Infrastructure/
│   ├── Jobs/
│   │   └── ResearchIndexingJob.cs
│   ├── Persistence/
│   │   ├── ResearchDbContext.cs
│   │   └── Repositories/
│   │       ├── ResearchDocumentRepository.cs
│   │       └── ResearchRetrievalRepository.cs
│   └── Services/
│       └── ConfiguredEmbeddingService.cs
└── Migrations/
    └── M009_ResearchRetrieval.cs

backend/src/FinanceSentry.Mcp/
└── Tools/
    ├── SearchResearchCorpusTool.cs
    └── GetResearchContextTool.cs

backend/tests/
├── FinanceSentry.Modules.Research.Tests/
│   ├── Unit/
│   └── Integration/
└── FinanceSentry.Mcp.Tests/
    └── ContractTests/
```

**Structure Decision**: The retrieval domain belongs in `FinanceSentry.Modules.Research` because the indexed corpus is research content. MCP remains a transport adapter and should contain no chunking, embedding, ranking, or persistence logic.

## Key Design Decisions

1. **Retrieval indexes stored data only.** Existing ingestion sources continue to fetch news and market research. This feature indexes data after it is in the database.
2. **Hybrid search is required.** Ticker/date/thesis filters are precise structured constraints; embeddings are for semantic matching. Ranking should combine both rather than rely on embeddings alone.
3. **Context packets are bounded.** `get_research_context` returns grouped, cited evidence within configured size limits so Ledger can reason without pulling an unbounded corpus.
4. **No `userId` parameter on retrieval MCP tools.** The authenticated MCP identity determines private visibility. Global documents are available to authenticated users; private notes are owner-only.
5. **Embedding metadata is versioned.** Store provider, model, dimension, and embedding version so reindexing is explicit when the embedding model changes.
6. **Indexing is idempotent and failure-isolated.** A failed document records a status/reason; other documents continue.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| New vector-capable persistence path in Research | Semantic retrieval needs approximate similarity over stored chunks | Existing `ILIKE` search misses semantically relevant evidence and cannot support RAG-quality context selection |
| New async indexing workflow | Embedding calls are external, slower, and failure-prone | Synchronous embedding during news/thesis writes would make core write paths brittle |
