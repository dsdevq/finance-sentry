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

- [ ] Confirm production Postgres image/extension plan for `pgvector`.
- [ ] Confirm MVP source types: recommended `NewsArticle`, `InvestmentThesis`, `DecisionNote`/`ThesisEvent`, and `Postmortem`.
- [ ] Confirm embedding provider/model config values during implementation, without hard-coding them into domain logic.
