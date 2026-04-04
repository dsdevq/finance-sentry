# Investment Tracking: Data Model & Schema

**Document Version**: 1.0  
**Created**: 2026-03-21  
**Last Updated**: 2026-03-21  
**Database**: PostgreSQL 14+  
**ORM**: Entity Framework Core 9+  

---

## Entity Relationship Diagram Summary

```
User (1) ──── (N) InvestmentAccount ──── (N) AssetHolding
              ──── (N) SyncJob
              ──── (N) PortfolioMetric
              ──── (N) AIAnalysisReport

AssetHolding (1) ──── (N) PriceHistory
              ──── (N) AIAnalysisReport
```

---

## Entities & Schema

### 1. InvestmentAccount

Represents a user's connected investment platform account (Binance, Interactive Brokers, etc.).

```sql
CREATE TABLE investment_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    account_name VARCHAR(255) NOT NULL, -- e.g., "My Binance", "IB Trading Account"
    platform VARCHAR(50) NOT NULL, -- enum: binance, interactive_brokers, kraken, coinbase (future)
    api_key_encrypted BYTEA NOT NULL, -- AES-256 encrypted
    api_secret_encrypted BYTEA NOT NULL, -- AES-256 encrypted
    account_status VARCHAR(50) NOT NULL DEFAULT 'pending', 
        -- enum: pending, active, failed, reauth_required, disabled
    last_sync_timestamp TIMESTAMP,
    last_sync_status VARCHAR(50), -- enum: success, failed, partial
    sync_error_message TEXT,
    holdings_count INTEGER DEFAULT 0,
    total_portfolio_value NUMERIC(20, 8), -- in base currency
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP -- soft delete
);

CREATE INDEX idx_investment_accounts_user_id ON investment_accounts(user_id);
CREATE INDEX idx_investment_accounts_platform ON investment_accounts(platform);
CREATE INDEX idx_investment_accounts_status ON investment_accounts(account_status);
```

**Validation Rules** (from FR-003, FR-011):
- `api_key_encrypted`, `api_secret_encrypted` MUST be encrypted using AES-256-GCM
- `platform` MUST be one of supported platforms (enum enforced at app layer)
- `account_status` transitions: pending → active OR pending → failed OR active → reauth_required
- `last_sync_timestamp` auto-updated on successful sync
- Keys never logged; only key fingerprint (first 4 chars) logged for audit

---

### 2. AssetHolding

Represents a single investment held in an InvestmentAccount.

```sql
CREATE TABLE asset_holdings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    investment_account_id UUID NOT NULL REFERENCES investment_accounts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    symbol VARCHAR(20) NOT NULL, -- e.g., BTC, AAPL, VTSAX
    asset_name VARCHAR(255) NOT NULL, -- e.g., Bitcoin, Apple Inc., Vanguard Total Stock
    asset_type VARCHAR(50) NOT NULL,
        -- enum: crypto_coin, stock, etf, bond, commodity, derivatives, other
    quantity NUMERIC(30, 8) NOT NULL, -- supports fractional shares, micro-transactions
    quantity_decimal_places INTEGER, -- precision tracking
    cost_basis_total NUMERIC(20, 8), -- cost basis in original currency
    cost_basis_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    purchase_date DATE,
    current_price NUMERIC(20, 8) NOT NULL,
    current_price_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    current_price_timestamp TIMESTAMP NOT NULL, -- when price was last updated
    current_value NUMERIC(20, 8) NOT NULL, -- quantity * current_price
    value_base_currency NUMERIC(20, 8) NOT NULL, -- current_value converted to portfolio base currency
    base_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    gain_loss NUMERIC(20, 8), -- current_value - cost_basis_total
    gain_loss_percent NUMERIC(10, 4),
    is_pinned BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP -- soft delete when removed from platform
);

CREATE INDEX idx_asset_holdings_account_id ON asset_holdings(investment_account_id);
CREATE INDEX idx_asset_holdings_user_id ON asset_holdings(user_id);
CREATE INDEX idx_asset_holdings_symbol ON asset_holdings(symbol);
CREATE INDEX idx_asset_holdings_asset_type ON asset_holdings(asset_type);
```

**Validation Rules** (from FR-001, FR-004, FR-006, FR-007):
- `quantity` MUST support at least 8 decimal places for crypto
- `cost_basis_total` calculated from quantity * purchase_price
- `current_value` = quantity * current_price (recalculated on each sync)
- `value_base_currency` converted using exchange rates at `current_price_timestamp`
- `gain_loss_percent` = (current_value - cost_basis_total) / cost_basis_total * 100
- `symbol` uniquely identifies asset within platform (BTC, AAPL, etc.)
- Supports multiple instances of same symbol (e.g., BTC in Binance and custody wallet)
- `current_price_timestamp` tracks staleness; if > 1 hour old, flag as stale data

---

### 3. PriceHistory

Historical price tracking for each asset to calculate performance and trend analysis.

```sql
CREATE TABLE price_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_holding_id UUID NOT NULL REFERENCES asset_holdings(id) ON DELETE CASCADE,
    symbol VARCHAR(20) NOT NULL,
    price_date DATE NOT NULL,
    price_timestamp TIMESTAMP NOT NULL, -- precise moment of price record
    price NUMERIC(20, 8) NOT NULL,
    price_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    high_24h NUMERIC(20, 8),
    low_24h NUMERIC(20, 8),
    volume_24h NUMERIC(30, 8),
    market_cap NUMERIC(30, 8), -- for crypto, null for stocks
    change_percent_1h NUMERIC(10, 4),
    change_percent_24h NUMERIC(10, 4),
    change_percent_7d NUMERIC(10, 4),
    change_percent_30d NUMERIC(10, 4),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_price_history_asset_id ON price_history(asset_holding_id);
CREATE INDEX idx_price_history_symbol ON price_history(symbol);
CREATE INDEX idx_price_history_date ON price_history(price_date);
CREATE INDEX idx_price_history_date_desc ON price_history(price_date DESC);
```

**Validation Rules** (from FR-004):
- One record per asset per day (aggregated from multiple intra-day snapshots)
- `price` normalized to 8 decimal places
- `change_percent_*` calculated against previous close
- Records immutable after creation (append-only history)
- Auto-purge records older than 5 years

---

### 4. SyncJob

Tracks each synchronization attempt with external platforms.

```sql
CREATE TABLE sync_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    investment_account_id UUID NOT NULL REFERENCES investment_accounts(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    sync_type VARCHAR(50) NOT NULL, -- enum: full_sync, incremental_sync, price_update
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
        -- enum: pending, in_progress, success, failed, partial_success
    started_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    duration_ms INTEGER, -- milliseconds to complete
    assets_synced INTEGER,
    assets_added INTEGER DEFAULT 0,
    assets_removed INTEGER DEFAULT 0,
    assets_updated INTEGER DEFAULT 0,
    error_message TEXT,
    error_code VARCHAR(50),
    retry_count INTEGER DEFAULT 0,
    next_retry_at TIMESTAMP,
    api_request_count INTEGER,
    api_response_code INTEGER,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_sync_jobs_account_id ON sync_jobs(investment_account_id);
CREATE INDEX idx_sync_jobs_user_id ON sync_jobs(user_id);
CREATE INDEX idx_sync_jobs_status ON sync_jobs(status);
CREATE INDEX idx_sync_jobs_created_at ON sync_jobs(created_at DESC);
```

**Validation Rules** (from FR-005, FR-011):
- `sync_type` determines expected frequency: full_sync (60min for stocks, 15min for crypto)
- Status state machine: pending → in_progress → (success OR partial_success OR failed)
- `duration_ms` must be recorded for performance monitoring (SLA: < 30s for < 100 assets)
- `retry_count` caps at 5; after 5 fails, account marked reauth_required
- Exponential backoff: retry_at = now() + (2 ^ retry_count) minutes (max 60 min)
- `error_code` standardized (e.g., INVALID_CREDENTIALS, RATE_LIMITED, API_DOWN)
- Failed jobs logged for alerting

---

### 5. PortfolioMetric

Aggregated portfolio statistics, calculated daily or on-demand.

```sql
CREATE TABLE portfolio_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    metric_date DATE NOT NULL,
    base_currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    total_value NUMERIC(20, 8) NOT NULL,
    total_cost_basis NUMERIC(20, 8),
    total_gain_loss NUMERIC(20, 8),
    total_gain_loss_percent NUMERIC(10, 4),
    portfolio_volatility NUMERIC(10, 4), -- annualized standard deviation
    concentration_risk NUMERIC(10, 4), -- Herfindahl index (0-100, higher = more concentrated)
    diversification_score NUMERIC(10, 4), -- custom metric (0-100, higher = better)
    top_asset_symbol VARCHAR(20),
    top_asset_percent NUMERIC(10, 4),
    allocation_by_type JSONB, -- { "crypto": 40, "stocks": 50, "bonds": 10 }
    allocation_by_platform JSONB, -- { "binance": 40, "interactive_brokers": 60 }
    number_of_assets INTEGER,
    number_of_platforms INTEGER,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    calculated_at TIMESTAMP NOT NULL
);

CREATE INDEX idx_portfolio_metrics_user_id ON portfolio_metrics(user_id);
CREATE INDEX idx_portfolio_metrics_date ON portfolio_metrics(metric_date DESC);
```

**Validation Rules** (from FR-006, FR-008):
- `total_value` = SUM(asset_holding.value_base_currency) across all assets
- `total_gain_loss` = SUM(asset_holding.gain_loss)
- `portfolio_volatility` = std_dev(daily_returns) over 30 days * sqrt(252)
- `concentration_risk` = Herfindahl index = SUM(allocation% ^ 2) (0 = perfectly diversified, 100 = single asset)
- `diversification_score` = 100 * (number_of_assets / max_theoretical_assets) with platform weighting
- Calculated nightly at 00:00 UTC and on-demand during portfolio refresh

---

---

## Future Phase: AI Integration

**DEFERRED TO PHASE 2**: The `AIAnalysisReport` entity and AI-powered analysis features will be implemented in a future phase. This includes:

- `AIAnalysisReport` entity and `ai_analysis_reports` table
- Asset-level AI analysis
- Portfolio-level AI analysis
- Risk assessment and forecasting capabilities

For reference on planned AI integration architecture, see `ai-integration-future.md`.

---

## Database Constraints & Triggers

### Constraint: Unique Asset per Account

```sql
ALTER TABLE asset_holdings 
ADD CONSTRAINT uq_asset_per_account UNIQUE(investment_account_id, symbol);
```

### Constraint: Portfolio Metric Uniqueness

```sql
ALTER TABLE portfolio_metrics
ADD CONSTRAINT uq_portfolio_metric_daily UNIQUE(user_id, metric_date);
```

### Trigger: Update `updated_at` on InvestmentAccount

```sql
CREATE OR REPLACE FUNCTION update_investment_account_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_investment_account_updated_at
BEFORE UPDATE ON investment_accounts
FOR EACH ROW
EXECUTE FUNCTION update_investment_account_timestamp();
```

### Trigger: Validate SyncJob Status Transitions

```sql
CREATE OR REPLACE FUNCTION validate_sync_job_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Only allow valid transitions
    IF NEW.status = OLD.status THEN
        RETURN NEW;
    END IF;
    
    IF OLD.status = 'pending' AND NEW.status NOT IN ('in_progress', 'failed') THEN
        RAISE EXCEPTION 'Invalid sync job status transition from pending to %', NEW.status;
    END IF;
    
    IF OLD.status = 'in_progress' AND NEW.status NOT IN ('success', 'partial_success', 'failed') THEN
        RAISE EXCEPTION 'Invalid sync job status transition from in_progress to %', NEW.status;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_sync_job_status_validation
BEFORE UPDATE ON sync_jobs
FOR EACH ROW
EXECUTE FUNCTION validate_sync_job_status();
```

---

## Security & Data Isolation

### User Data Isolation

All queries MUST include `WHERE user_id = @current_user_id`:

```sql
-- DO: Correct
SELECT * FROM asset_holdings 
WHERE user_id = @current_user_id AND investment_account_id = @account_id;

-- DON'T: Missing user_id filter
SELECT * FROM asset_holdings 
WHERE investment_account_id = @account_id;
```

### Row-Level Security (PostgreSQL RLS)

```sql
ALTER TABLE asset_holdings ENABLE ROW LEVEL SECURITY;

CREATE POLICY asset_holdings_user_isolation ON asset_holdings
    USING (user_id = current_user_id)
    WITH CHECK (user_id = current_user_id);
```

### Encryption at Rest

- API keys in `investment_accounts` table encrypted with AES-256-GCM
- Sensitive analysis data encrypted at application layer
- Database backups encrypted with AWS KMS or equivalent

---

## Migration & Versioning

### Baseline Migration (v1)

All tables created in single transaction with FK constraints enabled.

```sql
BEGIN;
CREATE TABLE investment_accounts (...);
CREATE TABLE asset_holdings (...);
CREATE TABLE price_history (...);
CREATE TABLE sync_jobs (...);
CREATE TABLE portfolio_metrics (...);
COMMIT;
```

### Future Migrations

Versioned in Entity Framework Core `DbContext.OnModelCreating()`:
- v2: Add `market_cap_percent` to asset_holdings
- v3: Add `cost_per_share` history tracking

---

## Performance Considerations

### Indexing Strategy

- `asset_holdings` frequently filtered by `user_id`, `investment_account_id`, `asset_type` → composite index
- `price_history` queried by date range → BRIN index on `price_date`
- `sync_jobs` queried by status and account → composite index
- `portfolio_metrics` queried by user and date desc → covering index

### Query Performance Targets

- Portfolio summary page (10 assets): < 500ms
- Asset detail page (1 asset + 5 years price history): < 1s
- Dashboard (all accounts + metrics): < 2s

