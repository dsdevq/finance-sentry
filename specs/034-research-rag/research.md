# Research: Production RAG over a Personal Financial Text Corpus

**Method**: deep-research pass (2026-07-22) — multi-source, cross-checked, cited. Conflicts flagged rather than smoothed. Scoped to *this* system (ASP.NET Core 9 / C# 13 monolith on PostgreSQL 14, single user, hundreds–low-thousands of docs; corpus = short news, long SEC filings, medium thesis notes; hard rule = numbers from SQL/tools, never fuzzy retrieval; preference = minimal infra, reuse Postgres, lean local/cheap).

**Meta-finding (the through-line):** *configuration, not model choice, drives most of the accuracy.* On FinanceBench, naive shared-vector RAG scored ~19% vs an optimized RAG ~76% on the same questions, near the ~79% long-context ceiling. Biggest wins here are **table-aware ingestion, metadata + temporal filtering, and a golden-set eval loop** — not exotic models.

---

## 1. Chunking

- **Baseline**: recursive/structure-aware split at **~400–512 tokens, 10–20% overlap** (~50–100 tokens). Optimal size is query-dependent (factoid 256–512; analytical 1024+), so no single size is best for both news and filings.
- **Overlap — conflicting evidence**: a 2026 analysis found overlap gave no measurable benefit; most guides still say 10–20%. → start 512/~50 and treat overlap as tunable-toward-zero.
- **Skip semantic chunking**: multiple studies (Vectara; arXiv cost study) found fixed-size *matches or beats* semantic on retrieval F1 at a fraction of the cost; on 10 SEC filings, structure-aware hit 0.877 context recall vs semantic 0.759 with ~10× more chunks.
- **Table-to-text = the single highest-leverage fix**: converting HTML filing tables to pipe-delimited text lifted recall **52.6% → 73.8% (~40% relative)** before any other tuning. Keep tables **atomic — never split mid-table.**
- **Two high-ROI techniques (defer to v2)**: parent-document / small-to-big retrieval (+15–30% on context-needing queries); Anthropic **Contextual Retrieval** (LLM-generated 50–100 token prefix per chunk) — cut top-20 failures 35% alone, 49% with contextual BM25, 67% adding a reranker (strongest single technique; costs an LLM call per chunk at ingest — affordable at this volume).

**Recommendation**: route by doc type. News/notes → near-atomic (1 chunk if <512 tokens, else 512/50). Filings → section/structure-aware split with **dedicated table-to-text**, tables atomic. Add contextual-chunk prefixes in v2. Skip semantic chunking.

## 2. Embedding models

- Top of MTEB is compressed (~5 pts) → context length, finance-fit, cost, dims matter more than top-line score.
- **Local**: **BGE-M3** — 1024-dim, **8192-token** context, emits dense+sparse+multi-vector from one model (drives hybrid by itself), ONNX-exportable → **best self-hosted default.** nomic-embed-text-v1.5 (768-dim, 8192, Matryoshka) is a strong alt. bge-large / E5 cap at 512 tokens — too short for filings. Qwen3-Embedding-7B tops open boards but is overkill to serve for one user.
- **API**: OpenAI text-embedding-3-large (3072-dim Matryoshka, ~$0.13/1M) cheap strong fallback; **voyage-finance-2** — finance-specialized, 1024-dim, **32K context**, ~+7% over OpenAI-3-large and +12% over Cohere-v3 on 11 financial datasets, **free 50M-token tier**.
- **Conflict flagged**: "Do we need domain-specific embeddings?" — gains are task-dependent; treat Voyage's +7–12% as vendor benchmarks, validate on your eval set. **FinBERT is sentiment, not a retrieval embedder — do not use for retrieval.** Matryoshka (truncatable dims) is now standard.
- **Running a local model from .NET**: (1) ONNX Runtime in-process (lowest latency; must port the HF tokenizer to C#); (2) **Ollama embeddings API** (`POST /api/embed`, serves bge-m3/nomic — easiest wiring, plain HTTP, no tokenizer plumbing, extra process); (3) Python sidecar (widest support, extra service).

**Recommendation**: **BGE-M3 via the existing local Ollama** (reuses infra you already run; gives sparse vectors for hybrid for free). Swap to **voyage-finance-2** (free tier, drop-in) if finance retrieval underperforms on the eval set. Keep 1024 dims (storage is a non-issue).

## 3. pgvector on Postgres 14

- **Over-provisioned at this scale.** pgvector HNSW matches/beats dedicated DBs at 1M vectors (Supabase: outperforms Qdrant on equal compute, accuracy@10=0.99); sub-10ms p99 to ~5M. You're 2–3 orders of magnitude below where it strains.
- **PG14 supported**: current pgvector 0.8.x supports Postgres 13+ (HNSW since 0.5, quantization 0.7, iterative scans 0.8).
- **Use HNSW, not IVFFlat** (3× faster, better accuracy; IVFFlat only for very large static sets). Defaults `m=16, ef_construction=64`; raise `ef_search`→~80–100 for recall.
- **Metadata filtering**: pre-0.8 post-filtering "overfiltered"; enable pgvector 0.8 `hnsw.iterative_scan=relaxed_order` (or partial indexes for selective filters).
- **Outgrow only** near low-millions of vectors; even then **pgvectorscale** (disk-backed StreamingDiskANN) keeps you in Postgres to tens of millions. A dedicated vector DB is a "1000× growth" concern, maybe never.

**Recommendation**: pgvector on existing PG14, **HNSW defaults**, `ef_search≈80`, **0.8 iterative_scan=relaxed_order** for filtered queries. No vector DB. pgvectorscale as the future lever.

## 4. Hybrid retrieval & reranking

- **Hybrid (dense + BM25) for finance — mixed evidence, flag as unresolved.** Pro: Anthropic saw contextual embeddings + contextual BM25 cut failures 49%; hybrid's edge concentrates on exact-match (tickers, codes). Con: on LOFin, plain dense *beat* naive BM25+dense hybrid; another source has BM25 beating dense. → **Build hybrid but tune weights empirically; don't trust naive RRF blindly.**
- **BM25 in Postgres**: native `ts_rank` **lacks IDF**; ParadeDB `pg_search` / Tiger `pg_textsearch` add real BM25 + fuzzy (~20× faster ranked top-K at 1M rows). But native FTS is "fine" at thousands–hundreds-of-thousands of docs, and staying in Postgres means the keyword index updates **transactionally** (no separate engine, no stale-index ETL gap).
- **Fusion**: **RRF, k=60** (robust near-universal default; works on ranks, no score normalization). Reach for weighted fusion only with calibrated comparable scores.
- **Reranking (high ROI, validate)**: retrieve 20–50 → rerank to 3–5. Reported +15–30% precision for ~100–300ms; on finance text+tables, hybrid + Cohere Rerank gave +17.4% Recall@5. **But** off-the-shelf cross-encoders can *hurt* specialized domains (−12% to −34% on patents) — validate. Models: **bge-reranker-v2-m3** (278M, Apache-2.0, ~matches Cohere, zero API cost) self-host default; **ms-marco MiniLM** lightest CPU. **License trap**: Jina Reranker v2 is CC-BY-NC (non-commercial); bge/mxbai are Apache-2.0.

**Recommendation**: hybrid = **pgvector HNSW (dense) + Postgres full-text (BM25-ish)**, fused **RRF k=60**. Start **native tsvector** (adequate; zero new infra); upgrade to ParadeDB `pg_search` only if exact-term ranking disappoints. Reranker is a **fast follow, not v0** (bge-reranker-v2-m3 on GPU, MiniLM-ONNX on CPU; retrieve 30 → rerank 5) — validate it helps on the golden set first.

## 5. Retrieval-quality tactics

- **HyDE / query expansion — contested; risky for numbers.** Some evals show gains with reranking, others show HyDE *underperforming* vanilla dense; HyDE's fabricated hypothetical doc can inject false details and helps little on precise numeric queries. **Defer; prefer a reranker first.**
- **Metadata filtering = highest-value tactic.** Store `ticker, published/filing_date, source, doc_type, thesis_id, as_of_date` and filter with the vector search. Also the ticker-disambiguation defense.
- **Recency**: use **soft time-decay** (similarity × age-decay), not hard `WHERE date<X` (too aggressive). RAG is "blind to time" — retrieves stale + current at equal similarity; **as-of-date filtering** is the best mitigation.
- **Citations**: capture provenance (doc, section, url, offset) *before* chunking so every chunk is traceable; citations can be hallucinated even when they look grounded → **atomic-claim verification** (decompose answer, check each claim vs its cited chunk) as a v2 guardrail.

**Recommendation**: ship metadata filtering + **as-of-date + soft recency decay** + provenance-at-ingest from day one (cheap, hit finance failure modes). Atomic-claim citation verification as v2. **Skip HyDE** unless the eval proves it.

## 6. Evaluation (solo-dev, lightweight)

- **RAGAS** is the right fit — RAG-specific, LLM-judge, synthetic test-gen. Metrics: faithfulness, answer relevancy, context precision, context recall. **Only context recall needs labels**; the other three are reference-free → start with almost no labeling.
- **Golden set ~100 Q/A pairs** (LLM-generate then human-review) is the practical target.
- **LLM-as-judge biases** (position >10% swing, verbosity, self-preference) → randomize/swap order + average; **judge with a different model family than the answer generator** (if answers come from Qwen/Claude, judge with the other).
- **Primary metric: recall@k** (how much answerable evidence reaches the generator); target **recall@5 ≥ 80%**. Rank metrics (MRR/nDCG) matter less once you rerank. Optional: Arize Phoenix for local traces + embedding-cluster inspection.

**Recommendation**: ~100-pair golden set (LLM-gen, hand-reviewed, incl. hard numeric ones), run **RAGAS** (faithfulness + context precision reference-free; recall on labeled subset), track **recall@5 ≥ 80%**, judge with a different model family. A few hours of setup.

## 7. Finance-specific pitfalls & mitigations

| Pitfall | Mitigation |
|---|---|
| Stale/temporal data (old filing contradicts new) | as-of-date filtering + soft recency decay; optional supersession ledger |
| Numbers in text/tables | table-aware chunking; **resolve actual figures via SQL/tools, RAG only locates + cites** |
| Entity/ticker disambiguation | ticker metadata filter + name/co-reference resolution before retrieval |
| Hallucinated citations | atomic-claim verification against the specific cited chunk |
| Over-chunking loses context | structure-aware + parent-document retrieval; don't over-split |

The "numbers never from fuzzy retrieval" rule is exactly right and corroborated by the numeric-retrieval failures above — keep the boundary hard.

---

## (a) Recommended minimal architecture (v0)

**Storage (reuse Postgres, one new `rag` schema):**
- `rag.documents` (id, doc_type {news|filing|thesis_note}, ticker, source, published_date, as_of_date, url, raw_text, provenance jsonb)
- `rag.chunks` (id, document_id fk, chunk_text, section, ticker, published_date, embedding `vector(1024)`, `content_tsv` tsvector)
- **HNSW** on `embedding` (cosine, defaults; pgvector 0.8 `iterative_scan=relaxed_order`; session `ef_search≈80`). **GIN** on `content_tsv`.

**Ingest (C#):** route by doc_type (news/notes near-atomic; filings section-aware + **table-to-text**, tables atomic); capture provenance per chunk *before* embedding; embed with **BGE-M3 (1024-dim, 8192-token) via Ollama** (or ONNX in-process); populate `content_tsv`.

**Query:** extract ticker/date filters → `WHERE`; dense (HNSW top-30) + keyword (`ts_rank` top-30) in parallel; **RRF k=60** → top-10; soft recency decay + as-of-date; feed top-5 to the LLM with a strict "cite the chunk id or say you don't know; numbers come from SQL/tools, not these passages" instruction; return answer with chunk-level citations.

**Eval:** ~100-pair golden set + RAGAS (recall@5 target ≥80%), judged by a different model family.

**Models locked**: embedder **BGE-M3** (fallback **voyage-finance-2**); index **pgvector HNSW**; fusion **RRF k=60**; keyword **native tsvector**. Zero new services beyond Postgres + local Ollama.

## (b) Defer until later
1. Cross-encoder reranker (bge-reranker-v2-m3 / MiniLM-ONNX) — validate on golden set first.
2. Anthropic Contextual Retrieval (chunk prefixes) — strongest single technique, adds ingest cost.
3. ParadeDB `pg_search` (real BM25+IDF+fuzzy) — only if native tsvector disappoints.
4. voyage-finance-2 swap — only if BGE-M3 underperforms on the eval set.
5. HyDE / query expansion — contested, risky for numeric queries.
6. Atomic-claim citation verification loop — v2 guardrail.
7. Semantic chunking — evidence says skip entirely.
8. pgvectorscale / dedicated vector DB — irrelevant until ~millions of vectors.
9. Parent-document retrieval — nice filings upgrade; can wait if section-aware chunking already returns whole sections.

## Key conflicts / low-confidence flags
- Chunk overlap helps vs no-benefit — tune toward zero.
- Semantic chunking loses to fixed/structure-aware — skip.
- Hybrid-on-finance: dense-vs-BM25 ordering is dataset-dependent — tune empirically.
- HyDE — coin-flip in the literature, risky for numbers.
- Rerankers can hurt specialized domains — validate.
- Vendor benchmarks (Voyage/Cohere/Supabase/reranker deltas) are self-published — verify on your own corpus.
