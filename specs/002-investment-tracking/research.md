# Investment Tracking: Technical Research & Decisions

**Document Version**: 1.0  
**Created**: 2026-03-21  
**Last Updated**: 2026-03-21  
**Status**: Final  

---

## Executive Summary

This document captures technical research and architectural decisions for the Multi-Source Investment Tracking feature. Three key decisions are documented with rationale and alternatives considered: LLM provider for AI analysis, market data feed source for price feeds, and caching strategy for performance optimization.

---

## Decision 1: LLM Provider for AI-Powered Analysis [DEFERRED TO PHASE 2]

### Status

**DEFERRED**: This decision and research is deferred to Phase 2. The analysis will be revisited when AI integration features are planned.

### Decision

**Selected: OpenAI GPT-4** with Claude 3 Sonnet as fallback.

### Rationale

1. **Model Quality**: GPT-4 provides superior reasoning for financial analysis, risk assessment, and forecasting compared to GPT-3.5. It understands nuanced portfolio context and generates more accurate risk assessments.

2. **Cost-Performance Balance**: 
   - GPT-4 Turbo: $0.01/1K input tokens, $0.03/1K output tokens
   - Claude 3 Sonnet: $0.003/1K input tokens, $0.015/1K output tokens
   - For typical analysis (500-2000 tokens per report), cost per report: $0.02-0.06
   - Estimated annual cost for 1000 users with 2 analyses/week: ~$6,240

3. **API Stability**: OpenAI has proven production track record with finance sector clients. Consistent uptime and backward compatibility.

4. **Prompt Flexibility**: GPT-4 handles complex prompts with portfolio data, multiple assets, and multi-currency context better than smaller models.

### Alternatives Considered

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Claude 3 Opus | Excellent reasoning, fewer hallucinations | Higher cost ($0.015/$0.075), slower response | Not selected—GPT-4 better ROI |
| GPT-3.5 Turbo | Cheaper ($0.0005/$0.0015 per 1K tokens) | Lower reasoning quality for complex analysis | Not selected—analysis quality critical |
| Open-source (Llama2, Mistral) | No API costs, full control | Requires self-hosting, maintenance overhead, lower quality | Not selected—operational burden too high |
| Anthropic (Claude 3 Sonnet) | Balanced cost/quality | Slightly lower reasoning than GPT-4 | Selected as fallback for redundancy |

### Implementation Impact

- **Backend**: Use OpenAI SDK for .NET (`Azure.AI.OpenAI` or `OpenAI.NET`)
- **Fallback Logic**: If GPT-4 unavailable, retry with Claude 3 Sonnet via Anthropic API
- **Prompt Templates**: Store versioned prompts in database for A/B testing and quality tracking
- **Rate Limiting**: Implement queue to avoid API throttling; max 10 concurrent analysis requests
- **Caching**: Cache identical analyses for 24 hours to reduce API calls

> **Note**: LLM integration research is complete but deferred. Revisit this decision when planning AI features in Phase 2.

---

## Decision 2: Market Data Feed Source

### Decision

**Selected: Multi-Source Hybrid Approach**:
- **Crypto prices**: Binance API (primary for users' holdings) + CoinGecko (fallback)
- **Stock/ETF prices**: Interactive Brokers API (primary for users' holdings) + Alpha Vantage (fallback for historical data)
- **Exchange rates**: ECB API (primary, free, reliable) + Fixer.io (fallback)

### Rationale

1. **Primary Source Efficiency**: Use exchange APIs directly for users' connected accounts—no 3rd-party fees, real-time, and authoritative.

2. **Fallback Reliability**: If primary data unavailable (API down, rate limited), fall back to CoinGecko/Alpha Vantage without service disruption.

3. **Cost Optimization**: 
   - Binance & IB APIs: Free (user credentials)
   - CoinGecko: Free tier (1000 calls/min, sufficient for 100+ users)
   - Alpha Vantage: Free tier (5 calls/min) covers daily stock updates
   - ECB: Free (no rate limit)
   - Total: ~$0/month if using free tiers

4. **Data Quality**: Exchange APIs provide authoritative prices; aggregators used only for edge cases.

### Alternatives Considered

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Single aggregator (CoinGecko, Alpha Vantage) | Simple, unified API | Limited historical data, rate limits, third-party dependency | Not selected—less reliable |
| Real-time WebSocket feeds (Binance, IB) | Sub-second updates | High infrastructure complexity, continuous connections, costly | Not selected—polling sufficient for V1 |
| Premium data provider (Bloomberg, Reuters) | Highest quality | $1000+/month, overkill for MVP | Not selected—cost prohibitive |
| Internal price cache (no external feed) | Full control | Stale data, poor UX, market sync delays | Not selected—users need real-time |

### Implementation Impact

- **Backend**: Create abstraction layer `IPriceProvider` with implementations for each source
- **Polling**: Crypto every 5 mins, stocks every 15 mins, exchange rates hourly
- **Fallback Chain**: Try primary → fallback1 → fallback2 → cache
- **Error Handling**: If all sources fail, show last known price with "Stale (X hours)" label
- **Rate Limiting**: Queue requests, respect API limits, batch calls where possible

---

## Decision 3: Caching Strategy for Market Data & Holdings

### Decision

**Redis Cache with Layered TTLs**:
- Market prices: 5-10 min TTL (crypto volatile, stocks less so)
- Portfolio calculations: 2-5 min TTL (dependent on price changes)
- Holdings data (from exchange): 30 min TTL (rarely changes intra-day)
- Exchange rates: 1 hour TTL (infrequently updated)
- AI analysis: 24 hour TTL (computationally expensive)

### Rationale

1. **Performance**: Redis lookups (~1ms) vs. API calls (~500-2000ms) reduces portfolio load time from 5-10s to <1s.

2. **Cost Savings**: Cache hits reduce API calls by ~80% during typical trading hours. Estimated savings: ~$500/month for 1000 users.

3. **Scalability**: Can support 1000+ concurrent users without external API throttling.

4. **Freshness Trade-off**: 
   - Crypto: 5-10 min acceptable (users refresh manually if needed)
   - Stocks: 15 min acceptable (market data delayed 15+ min anyway)
   - Analysis: 24 hour acceptable (market conditions change slowly enough)

5. **User-Scoped Caching**: Cache keyed as `user:{user_id}:portfolio:holdings` to ensure data isolation.

### Alternatives Considered

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| In-Memory Cache (Distributed Dictionary) | No external dependency | Limited size, no cross-instance sharing, loses on restart | Not selected—doesn't scale |
| Database Materialized Views | Persistent, no TTL churn | Slower updates, complex refresh logic | Not selected—less responsive |
| No Caching (Direct API) | Always current | API throttling, slow UX, high operational cost | Not selected—unacceptable performance |
| CDN Cache (CloudFlare, etc.) | Global distribution | Non-user-scoped, public data exposure risk | Not selected—financial data is private |

### Implementation Impact

- **Backend**: Use `StackExchange.Redis` client for .NET
- **Cache Key Design**: `user:{userId}:portfolio:holdings` (user-scoped), `crypto:BTC` (shared public data with safety check)
- **Invalidation Strategy**: TTL-based (passive) + event-based (active on sync completion)
- **Monitoring**: Track cache hit rate (target: >75% during normal hours), eviction rate
- **Persistence**: Configure RDB snapshots daily for recovery
- **Docker**: Redis runs in docker-compose alongside backend

---

## Technical Debt & Future Decisions

### Revisit in v2 (if scaling beyond 10K users):

1. **Real-time WebSocket feeds** if latency < 1 min becomes critical
2. **Database-level caching** (PostgreSQL materialized views) for aggregations
3. **Dedicated market data service** if external provider overhead grows
4. **LLM cost optimization** via model routing (use GPT-3.5 for simple summaries, GPT-4 for complex analysis)

### Research Needed Before Implementation:

- [ ] Verify OpenAI API has production SLA and fallback support
- [ ] Test CoinGecko rate limits with 100+ concurrent users
- [ ] Performance test portfolio calculation with 500+ assets
- [ ] Verify Redis persistence model meets compliance requirements

---

## Appendix: API Endpoint Assumptions

All decisions assume:
- Binance API v3 endpoint: https://api.binance.com/api/v3
- Interactive Brokers API: REST gateway or FIX protocol (TBD in contracts/)
- OpenAI API: https://api.openai.com/v1
- CoinGecko API: https://api.coingecko.com/api/v3
- Alpha Vantage API: https://www.alphavantage.co/query
- ECB API: https://data.ecb.europa.eu/

