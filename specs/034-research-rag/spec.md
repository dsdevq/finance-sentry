# Feature Specification: Research Corpus Semantic Search (RAG)

**Feature Branch**: `034-research-rag`
**Created**: 2026-07-22
**Status**: Draft (research done — see research.md)
**Input**: User description: "Semantic search over my financial text corpus — news, SEC filings, my own thesis/decision notes — so Ledger can reason over research with citations. Numbers never come from fuzzy retrieval; RAG only locates and cites text."

## Overview

Finance Sentry's tools answer *numeric/structured* questions deterministically. What they can't do is let the agent **reason over the text** it has accumulated — "what have analysts and filings been saying about DRAM pricing," "what did *I* write when I entered MU," "summarize the bear case across everything I've read." Today Ledger can only keyword-search news; it can't semantically search the whole corpus, and it can't touch filings or its own notes as a searchable body.

This feature adds **semantic (RAG) search over the unstructured text corpus** — market news, SEC filings, and the user's own thesis/decision notes — returning the most relevant **passages with citations** for the agent to reason over. The **hard boundary** (validated by the research pass): **numbers never come from retrieval.** RAG *locates and cites text*; authoritative figures still come from SQL/tools. This is the complement to the analytical query tool (033): prose vs. numbers.

> The full research report (chunking, embedding-model choice, pgvector, hybrid retrieval, evaluation, finance pitfalls) is in [research.md](./research.md). The plan should follow its "recommended minimal architecture."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reason over news + my own notes (Priority: P1)

The user's market news and personal thesis/decision notes are chunked, embedded, and indexed. Ledger can ask a natural-language question and get back the most relevant passages **with citations** (source, title/url, date), which it reasons over — never inventing figures, always attributing.

**Why this priority**: This is the core new capability and the smallest corpus that proves the whole pipeline (ingest → embed → hybrid retrieve → cite). News + notes are already in Finance Sentry and are the highest-value, lowest-complexity slice.

**Independent Test**: Ask "what have I written about MU, and what has the news said about memory pricing?" → receive relevant passages from *both* the user's notes and news, each with a citation; confirm no fabricated numbers and that an unanswerable query returns "nothing found," not a guess.

**Acceptance Scenarios**:

1. **Given** ingested news + notes, **When** Ledger runs a semantic search, **Then** it receives the top relevant passages, each with a source citation and date.
2. **Given** a query with exact terms (a ticker), **When** searched, **Then** hybrid retrieval (semantic + keyword) surfaces exact-term matches that pure semantic search would miss.
3. **Given** the retrieved passages, **When** Ledger answers, **Then** every claim is attributable to a returned passage and **no numeric figure is asserted from the passages** as authoritative.
4. **Given** a query with no relevant content, **When** searched, **Then** it returns an explicit empty result — never a fabricated passage or citation.

---

### User Story 2 - Bring in SEC filings (Priority: P2)

SEC filings (long, structured, table-heavy) are ingested with **structure-aware chunking and table-to-text extraction** so the agent can search across filings — the research's single highest-recall lever — with the same citation guarantees.

**Why this priority**: Filings are the richest research source but the hardest to chunk well (the research shows table-to-text alone lifted recall ~40%). Deferred from US1 because it needs the specialized ingestion, but it's the biggest content unlock.

**Independent Test**: Ingest a filing with tables; ask a question answerable from a filing table's surrounding text; confirm the relevant section is retrieved and cited, and the table's labels/shape are intact in the returned passage.

**Acceptance Scenarios**:

1. **Given** an ingested filing, **When** searched, **Then** the relevant section is retrieved with its citation (filing, section, date).
2. **Given** a filing with tables, **When** chunked, **Then** tables are kept intact (readable text) and not split mid-table.

---

### User Story 3 - Know it actually works (Priority: P3)

A lightweight evaluation harness measures retrieval quality against a small golden set, so changes to chunking/embedding/retrieval can be judged instead of guessed.

**Why this priority**: The research's clearest meta-finding is that *configuration, not model choice, drives accuracy* — which means you must be able to measure. Valuable, but the capability ships without it.

**Independent Test**: Run the eval harness over a ~100-pair golden set; get a retrieval recall@k score and a faithfulness read; confirm a deliberate degradation (worse chunking) lowers the score.

**Acceptance Scenarios**:

1. **Given** a golden set, **When** the eval runs, **Then** it reports retrieval recall@5 and an answer-faithfulness measure.
2. **Given** a config change, **When** re-evaluated, **Then** the score moves in the expected direction (regression is detectable).

---

### Edge Cases

- **Stale vs current** (an old filing contradicts a new one): retrieval must not present stale text as current — use as-of-date / recency handling so the agent knows the passage's date.
- **Ticker ambiguity** (same name, different entity): retrieval scoped/filtered by ticker so a passage about the wrong "Acme" isn't returned as relevant.
- **Hallucinated citation**: a returned citation must point to a real ingested passage; the agent must not fabricate sources.
- **Numbers in text**: a figure inside a retrieved passage is *context to cite*, not an authoritative value — the boundary holds.
- **Empty / cold start**: before ingestion, search returns explicit emptiness.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST ingest the text corpus (news, thesis/decision notes; filings in US2) — chunk, embed, and index each document — with provenance (source, title/url, date, ticker where known) captured per chunk.
- **FR-002**: System MUST provide a semantic search that, given a natural-language query, returns the most relevant passages **with citations** (source, date, and a link/reference back to the original).
- **FR-003**: Retrieval MUST be **hybrid** — combining semantic similarity with keyword matching — so exact terms (tickers, named entities) are not missed.
- **FR-004**: Results MUST be filterable by metadata (ticker, source, date range) and MUST account for **recency/as-of-date** so stale text is not presented as current.
- **FR-005**: The system MUST **never present numeric figures from retrieved passages as authoritative** — retrieval locates and cites text; authoritative numbers come from the deterministic tools. The search tool's description MUST state this.
- **FR-006**: An unanswerable query MUST return an explicit empty result; the system MUST NOT fabricate a passage or a citation.
- **FR-007**: Filing ingestion (US2) MUST use structure-aware chunking and MUST keep tables intact (table-to-text), not split mid-table.
- **FR-008**: The system MUST provide a lightweight way to evaluate retrieval quality (recall@k) and answer faithfulness against a golden set (US3).
- **FR-009**: Ingestion MUST be incremental (new documents embedded as they arrive) and MUST reuse existing infrastructure (Postgres) rather than adding a separate datastore, per the research recommendation.

### Key Entities *(include if feature involves data)*

- **Corpus Document**: an ingested text item — type (news / filing / thesis-note), source, url, ticker(s), published/as-of date, raw text, provenance.
- **Corpus Chunk**: a retrievable unit of a document — chunk text, embedding, keyword-index vector, and inherited metadata (ticker, date, source, section) for filtering + citation.
- **Golden Eval Pair**: a question + expected-relevant source(s) used to measure retrieval quality.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ledger can answer a research question by retrieving relevant passages from **both** news and the user's own notes in a single search, each with a citation.
- **SC-002**: Hybrid retrieval surfaces exact-term (ticker) matches that pure semantic search misses — demonstrable on a query where the term is rare.
- **SC-003**: **Zero** fabricated citations and **zero** authoritative numbers sourced from retrieved text across a review sample (the boundary holds).
- **SC-004**: Filing tables are retrievable with their labels/shape intact (US2) — verified on a table-bearing filing.
- **SC-005**: Retrieval recall@5 on the golden set is **≥ 80%** (the research's target), and a config regression is detectable by the eval (US3).
- **SC-006**: No new datastore is introduced — the corpus + embeddings live in the existing Postgres.

## Assumptions

- **The research report governs the how.** Embedding model, chunking, pgvector index, hybrid fusion, and eval choices follow [research.md]'s recommended minimal architecture (BGE-M3 via the existing local Ollama with a voyage-finance-2 fallback; pgvector HNSW on the existing Postgres; RRF hybrid with native full-text; ~100-pair golden set + RAGAS). These are plan-level and may be revalidated against the eval set.
- **Corpus already partly exists in FS**: news (030), filings (EDGAR), thesis/decision notes (020). This feature adds embedding + retrieval over them.
- **Local, minimal infra**: reuse Postgres (pgvector) and the existing local Ollama; no dedicated vector DB, no paid SaaS in v1.
- **Single primary user**; per-user scoping where the corpus is personal (notes), market-wide where it isn't (news, filings).

## Notes

- **[DECISION] RAG is for prose, not numbers**: the defining boundary. Retrieval locates + cites text; authoritative figures come from SQL/tools (033 + existing tools). Corroborated by the research (numeric-retrieval failure modes).
- **[DECISION] Reuse Postgres/pgvector + local Ollama**: no new datastore or SaaS — the research shows pgvector is over-provisioned at this scale.
- **[DECISION] Start with news + notes (US1), filings second (US2)**: smallest corpus that proves the pipeline first; filings need the specialized table-aware ingestion.
- **[OUT OF SCOPE] Analytical/numeric querying**: that's feature 033 (structured data). 033 and 034 are complements (numbers vs. prose), not alternatives.
- **[DEFERRED per research]**: cross-encoder reranker, Anthropic contextual-retrieval prefixes, ParadeDB `pg_search`, HyDE/query-expansion, a dedicated vector DB — all deferred until the measured baseline warrants them.
