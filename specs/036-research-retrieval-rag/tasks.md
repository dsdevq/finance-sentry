---
description: "Task list for Research Retrieval and RAG Context implementation"
---

# Tasks: Research Retrieval and RAG Context

**Input**: Design documents from `/specs/036-research-retrieval-rag/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-tools.md

**Tests**: Unit tests for chunking, idempotent indexing, scoring/filtering, and user isolation. PostgreSQL integration tests for vector/hybrid retrieval where EF InMemory cannot represent the behavior. MCP contract tests for tool names and response shape.

**Organization**: US1 semantic search (P1 MVP), US2 context packet (P1), US3 indexing automation (P2), US4 trust-boundary polish (P2).

## Phase 1: Setup

- [ ] T001 Confirm `pgvector` availability in local/prod Postgres images and document any Docker change needed
- [ ] T002 Add vector/embedding persistence dependency to `FinanceSentry.Modules.Research.csproj` if required
- [ ] T003 Add `ResearchRetrievalOptions` config object with chunk size, overlap, max chunks, embedding provider/model/dimensions, and indexing batch size

## Phase 2: Foundational

- [ ] T004 [P] Add `ResearchDocument`, `ResearchChunk`, `ResearchEmbedding`, and source/status enums in `backend/src/FinanceSentry.Modules.Research/Domain/`
- [ ] T005 [P] Add repository interfaces for research documents and retrieval in `Domain/Repositories/`
- [ ] T006 Add DbSet/configuration/indexes to `ResearchDbContext`
- [ ] T007 Generate migration `M009_ResearchRetrieval` with `.Designer.cs` and updated snapshot
- [ ] T008 Register repositories, options, chunker, indexer, retriever, and embedding service in `ResearchModule.cs`

**Checkpoint**: `dotnet build backend/` zero warnings; migration is discoverable.

## Phase 3: User Story 1 - Semantic Stored Search (P1)

- [ ] T009 [P] [US1] Unit tests for deterministic chunking and content hashing
- [ ] T010 [P] [US1] Unit tests for user/global visibility rules
- [ ] T011 [P] [US1] Integration test for semantic query returning seeded relevant chunks without exact keyword overlap
- [ ] T012 [US1] Implement `IResearchChunker`
- [ ] T013 [US1] Implement `IResearchIndexer` for source documents and chunks
- [ ] T014 [US1] Implement `IResearchRetriever` hybrid search with filters
- [ ] T015 [US1] Implement `SearchResearchCorpusQuery` + handler
- [ ] T016 [US1] Implement `search_research_corpus` MCP tool

**Checkpoint**: Ledger can retrieve cited stored research by semantic query.

## Phase 4: User Story 2 - Context Packet (P1)

- [ ] T017 [P] [US2] Unit test for thesis context grouping and bounded result size
- [ ] T018 [P] [US2] Unit test for ticker-without-thesis context behavior
- [ ] T019 [US2] Implement `GetResearchContextQuery` + handler
- [ ] T020 [US2] Implement context grouping for thesis, decision notes, recent news, filings, postmortems, and other research
- [ ] T021 [US2] Implement `get_research_context` MCP tool

**Checkpoint**: Ledger can request a bounded, cited context packet for a thesis or ticker.

## Phase 5: User Story 3 - Automatic Indexing (P2)

- [ ] T022 [P] [US3] Unit test for idempotent reindexing of unchanged content
- [ ] T023 [P] [US3] Unit test for per-document embedding failure isolation
- [ ] T024 [US3] Implement `IndexResearchDocumentsCommand` + handler
- [ ] T025 [US3] Implement `ResearchIndexingJob`
- [ ] T026 [US3] Mark changed source content as `Pending` when source content hash changes
- [ ] T027 [US3] Register recurring indexing job in `ResearchModule.cs`

**Checkpoint**: stored research content becomes searchable without manual refresh.

## Phase 6: MCP Contracts and Trust Boundary (P2)

- [ ] T028 Update `ToolNameContractTests` to include `search_research_corpus` and `get_research_context`
- [ ] T029 Add MCP contract tests for request/response shape and no `userId` parameter
- [ ] T030 Ensure retrieval tool descriptions state "research context, not authoritative financial truth"
- [ ] T031 Update `docs/mcp.md` current tool surface and retrieval guidance

## Phase 7: Verification

- [ ] T032 Run `dotnet build backend/` and resolve all warnings
- [ ] T033 Run `dotnet test FinanceSentry.sln --no-build -c Release --filter "Category!=Integration"`
- [ ] T034 Run retrieval integration tests against PostgreSQL/vector support
- [ ] T035 Run quickstart MCP smoke test against the deployed or local MCP server

## Dependencies

- Setup -> Foundational blocks all stories.
- US1 and US2 depend on Foundational; US2 also depends on US1 retriever.
- US3 depends on Foundational and can proceed after the document/chunk model exists.
- MCP contract updates should land with tool implementation.

## MVP

Ship Phases 1-4 plus T028-T030 first: stored documents can be searched semantically and Ledger can build cited context packets. Automatic recurring indexing can follow if manual command indexing is acceptable for the first validation pass.
