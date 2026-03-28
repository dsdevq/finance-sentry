# Feature Specification: Multi-Source Investment Tracking

**Feature Branch**: `002-investment-tracking`  
**Created**: 2026-03-21  
**Status**: Draft  
**Input**: User description: "Multi-source investment tracking for Binance and Interactive Brokers with AI-powered analytics"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Connect Crypto Wallet (Binance) & View Holdings (Priority: P1)

A user connects their Binance account using API keys. The system fetches current holdings (coins, quantities, values), displays them in a portfolio view, and shows real-time market prices.

**Why this priority**: Binance is one of the primary investment sources. Users must be able to connect and view their crypto holdings as the foundation for multi-source tracking.

**Independent Test**: Can be fully tested by connecting a real or sandbox Binance account, verifying holdings are fetched and displayed with current prices. Delivers immediate value: user sees consolidated crypto portfolio.

**Acceptance Scenarios**:

1. **Given** a user is logged in, **When** they click "Connect Investment Account" and select Binance, **Then** they see a prompt to enter API keys
2. **Given** the user enters valid API keys, **When** the system validates them, **Then** it fetches and displays all holdings (coins, quantities, current values in user's preferred currency)
3. **Given** holdings are fetched, **When** the user opens the portfolio view, **Then** they see each asset with symbol, quantity, current price, and total value
4. **Given** market prices update, **When** the price feed refreshes, **Then** asset values update in real-time (max 1-minute latency)
5. **Given** a user has holdings in multiple currencies, **When** they view total portfolio value, **Then** all assets are converted to a single base currency (e.g., USD)

---

### User Story 2 - Connect Brokerage (Interactive Brokers) & Aggregate (Priority: P2)

A user connects their Interactive Brokers account. The system fetches stock/ETF/bond holdings, integrates them with existing Binance holdings, and displays consolidated portfolio view with allocation percentages.

**Why this priority**: Interactive Brokers represents traditional stock/ETF holdings. Multi-source consolidation is the key differentiator—users see all investments in one place. Depends on Story 1 working.

**Independent Test**: Can be tested by connecting Interactive Brokers account, verifying holdings are fetched, and checking that portfolio view correctly aggregates with Binance holdings.

**Acceptance Scenarios**:

1. **Given** the user has connected Binance and now adds Interactive Brokers, **When** they complete the connection, **Then** the system fetches IB holdings and merges with existing Binance data
2. **Given** holdings exist from multiple sources, **When** the user views portfolio, **Then** they see complete list of all assets ordered by value
3. **Given** multiple portfolio sources exist, **When** the user views allocation breakdown, **Then** they see percentage allocation by asset and by source (Binance vs IB)
4. **Given** a user updates an account (e.g., buys stock), **When** the next sync occurs, **Then** holdings are updated and portfolio statistics recalculated

---

### User Story 3 - AI-Powered Portfolio Analysis & Reports (Priority: P3)

For each asset (crypto or stock) and for the portfolio overall, the system generates AI-powered analysis including performance summary, risk assessment, and forecast. Users can view asset-specific reports and an overall portfolio summary.

**Why this priority**: AI analysis is the core value proposition per product vision. Delivers actionable insights and recommendations. Depends on Stories 1 & 2 for holdings data.

**Independent Test**: Can be tested by connecting investment accounts, allowing AI analysis to generate, and verifying reports are displayed. Delivers insights that help users make decisions.

**Acceptance Scenarios**:

1. **Given** the user has multiple holdings, **When** they click "AI Analysis" for a specific asset, **Then** the system displays a report with: performance summary, risk factors, forecast trend
2. **Given** holdings and market data are available, **When** the user views portfolio analysis, **Then** they see overall risk score, asset allocation recommendations, and diversification suggestions
3. **Given** the AI generates analysis, **When** the user views an asset report, **Then** it includes human-readable summary and actionable recommendations
4. **Given** market conditions change, **When** reports are regenerated, **Then** forecasts and recommendations are updated accordingly

### Edge Cases

- What happens if API keys are revoked or IP address blacklisted? → System detects failed syncs, shows "Reauthorization required", and disables auto-sync
- What if market prices are unavailable for some assets (penny stocks, delisted)? → System shows last known price with staleness indicator ("price as of X hours ago")
- What if a user holds fractional shares or micro-transactions? → System accumulates quantities with proper decimal precision
- What if exchange rates fluctuate significantly between syncs? → System shows both current and previous day conversion rates for reference

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST support Binance API v3 for fetching account balances and holdings
- **FR-002**: System MUST support Interactive Brokers API for fetching positions and portfolio data
- **FR-003**: API credentials (keys, tokens) MUST be encrypted at rest using AES-256 and never logged
- **FR-004**: System MUST fetch real-time (or near-real-time) market prices for all held assets
- **FR-005**: System MUST automatically sync holdings on a configurable schedule (default: every 15-30 minutes for crypto, every 60 minutes for stocks)
- **FR-006**: System MUST aggregate holdings from multiple sources and calculate portfolio metrics: total value, allocation %, gains/losses
- **FR-007**: System MUST support multi-currency portfolio (automatically convert all assets to a single base currency)
- **FR-008**: System MUST calculate portfolio risk metrics: volatility, concentration risk, diversification score
- **FR-009**: System MUST generate AI-powered analysis for individual assets and portfolio overall (via LLM integration)
- **FR-010**: AI analysis MUST generate human-readable reports with insights and actionable recommendations
- **FR-011**: System MUST handle failed syncs gracefully and implement exponential backoff retry logic
- **FR-012**: Each user's portfolio data MUST be isolated; holdings/analysis from one user are never visible to another

### Key Entities *(include if feature involves data)*

- **InvestmentAccount**: Represents a user's connected investment account (Binance, Interactive Brokers, etc.)
  - Attributes: account_id, user_id, platform (binance/interactive_brokers), api_key_encrypted, account_status (active/failed/reauth_required), last_sync_timestamp, holdings_count
  - Relationships: one-to-many with Asset, one-to-many with SyncJob

- **Asset**: Represents a single investment (crypto coin, stock, ETF, bond)
  - Attributes: asset_id, account_id, symbol, asset_type (crypto/stock/etf/bond), quantity, cost_basis, current_price, current_value, purchase_date, currency
  - Relationships: many-to-one with InvestmentAccount, one-to-many with PriceHistory

- **PriceHistory**: Historical price data for tracking asset value changes
  - Attributes: price_history_id, asset_id, price_date, price, change_percent_1d, change_percent_7d, change_percent_30d
  - Relationships: many-to-one with Asset

- **SyncJob**: Represents a single synchronization attempt with an investment platform
  - Attributes: sync_job_id, account_id, started_at, completed_at, status (success/failed/retrying), assets_synced, error_message, retry_count
  - Relationships: many-to-one with InvestmentAccount

- **PortfolioAnalysis**: AI-generated analysis for portfolio or individual assets
  - Attributes: analysis_id, user_id, asset_id (null for portfolio-level), analysis_date, risk_score, allocation_recommendations, forecast_summary, ai_insights_json, generated_at
  - Relationships: references Asset, one-to-one relationship with portfolio snapshot

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Investment accounts sync and display holdings within 2 minutes of user connecting them
- **SC-002**: 98%+ of scheduled syncs succeed without manual intervention over a 30-day period
- **SC-003**: Portfolio view displays aggregated holdings from multiple sources with sub-second response time
- **SC-004**: Market prices update within 1-minute latency of actual market data
- **SC-005**: AI analysis generates reports for any asset within 30 seconds of user requesting
- **SC-006**: Portfolio supports minimum 500+ assets without performance degradation
- **SC-007**: Gains/losses calculations are accurate to within 0.01% for all currencies and conversions
- **SC-008**: Users view complete portfolio analysis (all assets + overall metrics) in under 3 seconds on initial load
- **SC-009**: AI-generated insights are human-readable and provide at least one actionable recommendation per report

## Assumptions

- **APIs Available**: Binance API v3 and Interactive Brokers API are available and documented. Test credentials/sandbox are available for development.
- **Price Data Source**: Real-time or near-real-time price data is available via market data provider (CoinGecko, Alpha Vantage, or exchange APIs).
- **Exchange Rates**: Currency conversion rates are available via a reliable service (ECB, Fixer.io, or exchange APIs).
- **LLM Integration**: OpenAI API or compatible LLM service is available for AI-powered analysis. Prompts are documented and versioned.
- **User Authentication**: User authentication is already implemented. Investment tracking assumes authenticated user context.
- **No Trading**: System does NOT execute trades; it only reads holdings and metadata. All trading occurs through original platforms.

## Notes

- [NEEDS CLARIFICATION: Should the system support additional platforms beyond Binance and Interactive Brokers (e.g., Kraken, Coinbase, Fidelity)? This affects extensibility/adapter pattern design.]
- [NEEDS CLARIFICATION: For AI-powered analysis, what specific LLM model should be used (GPT-3.5, GPT-4, open-source alternative)? This affects cost and response time.]
- Feature does not include portfolio backtesting, historical performance simulations, or tax reporting—these are separate features.
- Real-time price updates (WebSocket integration) are NOT required for MVP; polling every 1 minute is acceptable for V1.
