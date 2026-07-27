# Feature Specification: Research Retrieval and RAG Context for Ledger

**Feature Branch**: `036-research-retrieval-rag`
**Created**: 2026-07-26
**Status**: Draft
**Input**: User description: "Add DB-backed research retrieval and RAG context tools for Ledger. Keep Finance Sentry DB-first; use retrieval for stored research context, not for authoritative balances/holdings."

## Overview

Ledger already has deterministic MCP tools for portfolio truth and structured analytics. The gap is the research side: news, filings, thesis notes, decision journal entries, postmortems, and source documents are stored or partially stored, but retrieval is keyword/tag based. This feature adds a retrieval layer over Finance Sentry's owned research corpus so Ledger can assemble cited context before reasoning.

The feature does not make Ledger freely browse the internet for core answers. External sources remain ingested, normalized, timestamped, and auditable before they become first-class context. Live web lookup remains advisory and out of scope for this feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search Stored Research Semantically (Priority: P1)

Ledger searches the stored research corpus using natural language and receives relevant chunks with source metadata, timestamps, tickers, thesis links, and scores.

**Why this priority**: This is the minimum useful retrieval slice. It upgrades existing research search from exact words and tags to semantic discovery without changing the authoritative finance tools.

**Independent Test**: Seed research documents and chunks for two tickers and two theses; query with terms that do not exactly match the stored wording; confirm semantically related chunks are returned, scoped correctly, and include citations.

**Acceptance Scenarios**:

1. **Given** stored articles about DRAM pricing tagged to a memory thesis, **When** Ledger searches "memory cycle recovery evidence", **Then** the DRAM chunks are returned even if the exact phrase is absent.
2. **Given** chunks for multiple tickers, **When** Ledger filters by `ticker=MU`, **Then** only MU-linked chunks are returned.
3. **Given** user-private thesis notes, **When** another authenticated user searches the corpus, **Then** those private chunks are not returned.

---

### User Story 2 - Build Thesis Context for RAG (Priority: P1)

Ledger asks for context around a specific thesis or ticker and receives a compact, cited context packet suitable for LLM reasoning.

**Why this priority**: Ledger needs a small, reliable context-building tool, not a raw dump of every matching article. This makes the agent better at questions like "what challenges my thesis?" while keeping generated conclusions outside the retrieval layer.

**Independent Test**: Seed a thesis, decision note, news articles, and a postmortem; call the context tool for that thesis; confirm the result groups evidence by source type and includes enough provenance to cite.

**Acceptance Scenarios**:

1. **Given** a thesis with linked notes and recent articles, **When** Ledger requests context for the thesis, **Then** the response includes thesis text, decision notes, relevant research chunks, citations, and freshness metadata.
2. **Given** a ticker with no thesis, **When** Ledger requests context for the ticker, **Then** the response returns public/global research chunks and states that no thesis context exists.
3. **Given** a large matching corpus, **When** Ledger requests context, **Then** the response stays within a configured chunk/token budget and reports omitted result counts.

---

### User Story 3 - Index Research Content Automatically (Priority: P2)

Research content inserted by existing ingestion flows is chunked and embedded asynchronously so retrieval stays current.

**Why this priority**: Retrieval quality depends on fresh indexes. The feature must not require manual embedding refreshes after every news ingestion or thesis edit.

**Independent Test**: Insert a new article and a new thesis note; run the indexing job; confirm chunks and embeddings are created once, deduped, and searchable.

**Acceptance Scenarios**:

1. **Given** a newly ingested news article, **When** the indexing job runs, **Then** the article is chunked, embedded, and marked indexed.
2. **Given** unchanged source content, **When** the indexing job runs again, **Then** duplicate chunks are not created.
3. **Given** the embedding provider fails, **When** indexing runs, **Then** the failure is recorded and other documents continue indexing.

---

### User Story 4 - Preserve Authoritative Tool Boundaries (Priority: P2)

Ledger uses retrieval only for research context and continues using dedicated tools for balances, holdings, budgets, taxes, risk, and analytics.

**Why this priority**: Finance answers need a clear trust model. RAG context must not become a substitute for structured financial truth.

**Independent Test**: Inspect MCP tool descriptions and retrieval contracts; confirm they explicitly state that retrieval is non-authoritative research context and that financial facts must come from structured tools.

**Acceptance Scenarios**:

1. **Given** Ledger asks "what is my current exposure?", **When** the tool catalog is inspected, **Then** retrieval tools instruct Ledger to use portfolio/risk tools for authoritative numbers.
2. **Given** retrieval returns a chunk mentioning a historical position, **When** Ledger answers a current-holdings question, **Then** it must call the holdings/portfolio tool instead of treating the chunk as current truth.

### Edge Cases

- A document has no usable text: mark it `Skipped` with a reason; do not create empty chunks.
- A source URL changes but content hash is unchanged: retain one document identity and update source metadata.
- An article is global market data with no `UserId`: make it searchable to authenticated users only when it is from global research sources, never as another user's private note.
- A private thesis note references a public article: return the public article to all authenticated users, but return the private note only to its owner.
- A query asks for current balances or holdings through retrieval: return context only and rely on tool descriptions to redirect Ledger to authoritative tools.
- Embedding model or dimension changes: store embedding model metadata and reindex via versioned batches.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST store retrievable research documents in the database with source type, source identifier, canonical URL when available, title, text/summary, publication/capture timestamps, content hash, tickers, thesis ids, optional user id, and indexing status.
- **FR-002**: The system MUST split supported research documents into deterministic chunks with stable ordinals, content hashes, source offsets where available, and provenance back to the parent document.
- **FR-003**: The system MUST store embeddings for chunks through a provider interface; provider implementation and model name MUST be configurable.
- **FR-004**: The system MUST support hybrid retrieval over stored chunks using semantic similarity plus lexical/title/ticker/thesis filters.
- **FR-005**: Retrieval MUST enforce user isolation: private user documents are visible only to the owning authenticated user; global market documents may be visible to authenticated users.
- **FR-006**: MCP retrieval responses MUST include citations/provenance sufficient for Ledger to explain where context came from: document id, chunk id, source type, source name/url, title, published/captured timestamp, and relevance score.
- **FR-007**: The MCP surface MUST expose `search_research_corpus` for general retrieval and `get_research_context` for thesis/ticker context packets.
- **FR-008**: Retrieval tools MUST state in their descriptions that they provide non-authoritative research context; authoritative financial facts remain in dedicated structured MCP tools.
- **FR-009**: Indexing MUST be idempotent: unchanged documents do not create duplicate chunks or embeddings.
- **FR-010**: Indexing failures MUST be recorded per document without blocking unrelated documents.
- **FR-011**: The feature MUST include unit tests for chunking, scoring/filtering, user isolation, and idempotent indexing.
- **FR-012**: The feature MUST include MCP contract tests for the two new retrieval tools and update the tool-name contract test.
- **FR-013**: The feature MUST NOT add a general live-internet browsing tool for Ledger.

### Key Entities

- **Research Document**: A stored research object such as a news article, filing excerpt, thesis note, decision journal entry, or postmortem. It is the parent record for retrieval chunks.
- **Research Chunk**: A deterministic text segment derived from a research document. It is the retrievable unit and carries provenance to the parent.
- **Research Embedding**: A vector representation of a chunk for semantic similarity search, tied to an embedding provider, model, dimension, and version.
- **Retrieval Query**: A user-scoped search request with natural language text, optional filters, ranking configuration, and audit metadata.
- **Research Context Packet**: A compact, cited response that groups relevant chunks by thesis, ticker, source type, freshness, and relevance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A semantic query with no exact keyword overlap returns the expected seeded relevant chunk in unit or integration tests.
- **SC-002**: User isolation tests prove that one user's private thesis notes cannot be retrieved by another user.
- **SC-003**: Re-running indexing over unchanged documents creates zero duplicate chunks and zero duplicate active embeddings.
- **SC-004**: MCP tool-name contract includes exactly the two new retrieval tools and all existing tools remain available.
- **SC-005**: `get_research_context` returns a bounded context packet with citations for at least thesis, news, and decision-note source types in tests.
- **SC-006**: `dotnet build backend/` completes with zero warnings after implementation.

## Assumptions

- PostgreSQL remains the primary storage engine. The preferred semantic index is `pgvector` in the Research module database.
- The first implementation uses a configurable embedding provider behind `IEmbeddingService`; the specific provider/model is deploy-time configuration, not hard-coded in domain logic.
- Retrieval is backend-only for this feature. No frontend UI is required.
- Existing Research module ingestion continues to own external source fetching. This feature indexes what the system stores; it does not introduce broad live browsing.
- Existing MCP identity resolution is reused, but retrieval tools must not accept arbitrary cross-user `userId` overrides.

## Notes

- **[DECISION] DB-first trust boundary**: Authoritative finance answers stay on structured MCP tools backed by normalized database state. Retrieval is for research context only.
- **[DECISION] Two-tool MCP surface**: Add `search_research_corpus` and `get_research_context`; avoid one tool per source type.
- **[DECISION] Hybrid retrieval**: Use semantic search plus lexical/ticker/thesis filters. Pure vector search is not enough for tickers, dates, and thesis-scoped questions.
- **[DECISION] Provider boundary**: Embedding generation sits behind an application interface so OpenAI or compatible providers can be swapped without touching domain or MCP tool code.
- **[OUT OF SCOPE] Agent answer generation**: MCP returns context and citations. The consuming agent decides how to synthesize an answer.
- **[OUT OF SCOPE] Live web browsing**: Direct internet lookup for Ledger is a separate advisory-enrichment feature and must be labeled differently from stored Finance Sentry data.
