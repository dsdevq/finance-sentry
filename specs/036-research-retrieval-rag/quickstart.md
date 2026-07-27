# Quickstart: Research Retrieval and RAG Context

## Prerequisites

```bash
cd backend
dotnet restore FinanceSentry.sln
dotnet build FinanceSentry.sln --no-restore -c Release
```

PostgreSQL must support the selected vector storage. If using `pgvector`, verify the extension is available in the Research database before applying `M009_ResearchRetrieval`.

## Local Verification

1. Apply the Research migration.

```bash
cd backend
dotnet ef database update --project src/FinanceSentry.Modules.Research --startup-project src/FinanceSentry.API
```

2. Seed or create at least:

- one `NewsArticle` about a ticker
- one `InvestmentThesis`
- one decision note or thesis event
- one private document for a different test user

3. Run the indexing job or command.

Expected result:

- `research_documents` contains source-backed rows
- `research_chunks` contains deterministic chunks
- `research_embeddings` contains one active embedding per chunk/model/version
- unchanged documents do not create duplicates on a second run

4. Run backend tests.

```bash
cd backend
dotnet test FinanceSentry.sln --no-build -c Release --filter "Category!=Integration"
```

5. Run the retrieval integration tests that require PostgreSQL/vector support.

```bash
cd backend
dotnet test tests/FinanceSentry.Modules.Research.Tests/FinanceSentry.Modules.Research.Tests.csproj -c Release --filter "Category=RetrievalIntegration"
```

## MCP Smoke Test

Invoke `search_research_corpus` with a semantic query that does not exactly match the seeded text.

Expected result:

- relevant seeded chunk is returned
- result includes document/chunk ids and source citation metadata
- private documents from another user are absent

Invoke `get_research_context` for a seeded thesis.

Expected result:

- response contains thesis summary plus grouped evidence
- all evidence has citations
- response is bounded by `maxChunks`
- tool description states this is research context, not authoritative portfolio truth

## Production Checks

- Confirm `M009_ResearchRetrieval` appears in `__ef_migrations_history_research`.
- Confirm embedding provider config is present and secrets are not logged.
- Confirm indexing failures are visible in logs and persisted per document.
- Confirm the MCP tool-name contract includes `search_research_corpus` and `get_research_context`.
