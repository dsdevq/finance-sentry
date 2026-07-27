# Research: Research Retrieval and RAG Context for Ledger

## Decision 1: DB-backed corpus before live web

**Decision**: Ledger's primary research context comes from stored Finance Sentry research documents, not direct live web access.

**Rationale**: Stored documents give provenance, timestamps, repeatability, user isolation, and testable behavior. They also fit the existing Research module model: external sources are ingested, normalized, monitored, and queried through CQRS/MCP.

**Alternatives considered**:

- Let Ledger browse the internet directly: rejected for core workflows because answers become hard to audit, hard to replay, and source quality varies per run.
- Store only summaries: rejected because summaries lose provenance and make later re-ranking or citation quality weaker.

## Decision 2: Retrieval belongs in Research, not MCP

**Decision**: Chunking, embeddings, indexing, ranking, and repositories live in `FinanceSentry.Modules.Research`. MCP exposes only thin tools.

**Rationale**: MCP already follows the pattern of thin adapters around module CQRS handlers. Keeping retrieval in Research preserves module ownership and makes the same retrieval service available to future REST/UI surfaces.

## Decision 3: Two MCP tools

**Decision**: Add `search_research_corpus` and `get_research_context`.

**Rationale**: `search_research_corpus` is a general retrieval primitive. `get_research_context` is a higher-level context packer for common Ledger workflows around a thesis or ticker. This avoids a separate MCP tool per document source.

## Decision 4: Hybrid retrieval

**Decision**: Retrieval uses structured filters plus semantic similarity. Lexical matching remains useful for tickers, source titles, exact terms, and short acronyms; vectors are useful for concept similarity.

**Rationale**: Finance research has many terms where exact matching matters (`MU`, `DRAM`, `gross margin`) and many questions where exact matching is insufficient ("memory cycle recovery" vs "pricing rebound"). Hybrid retrieval covers both.

## Decision 5: pgvector as preferred vector store

**Decision**: Use Postgres as the vector store, preferably via `pgvector`, inside the Research schema.

**Rationale**: Finance Sentry is already Postgres-centered. Keeping vectors beside source documents simplifies auth, transactions, migrations, backups, and operations. A separate vector database is unnecessary at personal-product scale.

**Risk**: Production Postgres must have the vector extension installed. The quickstart must include an explicit extension check.

## Decision 6: Provider interface for embeddings

**Decision**: Define `IEmbeddingService` in Application and implement provider-specific calls in Infrastructure.

**Rationale**: The domain should not know whether embeddings come from OpenAI or another compatible provider. Provider, model, dimensions, batch size, and timeout are configuration.

## Decision 7: Context, not generated answers

**Decision**: MCP retrieval tools return context and citations. They do not call an LLM to generate final prose.

**Rationale**: Ledger is already the consuming agent. The server should provide reliable data and context, while the client/agent performs synthesis using its model.

## Open Questions

- Which stored source types are MVP? Recommended MVP: `NewsArticle`, `InvestmentThesis`, `ThesisEvent` decision notes, and postmortem packet text if already persisted.
- Is the production Postgres image extension-ready for `pgvector`, or do we need a deployment image change?
- Should SEC filing full text be ingested in this feature or in a later filing-ingestion feature? Recommended: later unless excerpts already exist.
