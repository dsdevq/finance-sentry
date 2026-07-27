# Requirements Checklist: Research Retrieval and RAG Context

**Feature**: `036-research-retrieval-rag`
**Date**: 2026-07-26

## Spec Quality

- [X] User stories are independently testable.
- [X] Requirements distinguish authoritative financial truth from research context.
- [X] User isolation is explicit.
- [X] MCP tool surface is bounded to two new tools.
- [X] Live internet browsing is explicitly out of scope.
- [X] Success criteria are measurable.

## Remaining Clarifications

- [X] Confirm production Postgres image/extension plan for `pgvector`. — Resolved 2026-07-27: no image change; embeddings stored as `real[]`, ranking in-app; pgvector is the documented upgrade path (research.md Decision 5).
- [X] Confirm MVP source types: recommended `NewsArticle`, `InvestmentThesis`, `DecisionNote`/`ThesisEvent`, and `Postmortem`. — Resolved: MVP indexes `NewsArticle`, `InvestmentThesis`, and `DecisionNote` (thesis events carrying decision notes). `Postmortem`/`FilingExcerpt`/`ThesisEvent` exist in the enum for future indexing; postmortem packets are query-time composites today, not persisted text.
- [X] Confirm embedding provider/model config values during implementation, without hard-coding them into domain logic. — Resolved: `ResearchRetrieval:Embedding` config section (OpenAI-compatible; defaults `openai` / `text-embedding-3-small` / 1536 dims), disabled by default; lexical-only ranking until enabled.
