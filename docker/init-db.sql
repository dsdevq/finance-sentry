-- Finance Sentry Database Initialization Script
-- Version: 1.0.0
-- Purpose: Initial schema and setup for bank sync feature

-- Create bank_account table
CREATE TABLE IF NOT EXISTS bank_account (
    account_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    plaid_item_id VARCHAR(24) NOT NULL UNIQUE,
    bank_name VARCHAR(255) NOT NULL,
    account_type VARCHAR(50) NOT NULL CHECK (account_type IN ('checking', 'savings', 'credit', 'investment')),
    account_number_last4 CHAR(4) NOT NULL,
    owner_name VARCHAR(255) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'EUR',
    current_balance DECIMAL(15, 2),
    sync_status VARCHAR(50) NOT NULL DEFAULT 'pending' CHECK (sync_status IN ('pending', 'syncing', 'active', 'failed', 'reauth_required')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by UUID,
    updated_by UUID
);

-- Create transaction table
CREATE TABLE IF NOT EXISTS transaction (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL REFERENCES bank_account(account_id) ON DELETE CASCADE,
    amount DECIMAL(15, 2) NOT NULL,
    posted_date DATE,
    transaction_date DATE NOT NULL,
    description TEXT NOT NULL,
    unique_hash VARCHAR(64) NOT NULL,
    is_pending BOOLEAN NOT NULL DEFAULT false,
    transaction_type VARCHAR(50),
    merchant_name VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(account_id, unique_hash)
);

-- Create sync_job table
CREATE TABLE IF NOT EXISTS sync_job (
    sync_job_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL REFERENCES bank_account(account_id) ON DELETE CASCADE,
    status VARCHAR(50) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'running', 'success', 'failed')),
    started_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE,
    error_message TEXT,
    error_code VARCHAR(50),
    transactions_synced INTEGER DEFAULT 0,
    last_transaction_date DATE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- Create encrypted_credential table
CREATE TABLE IF NOT EXISTS encrypted_credential (
    credential_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL UNIQUE REFERENCES bank_account(account_id) ON DELETE CASCADE,
    encrypted_data BYTEA NOT NULL,
    iv BYTEA NOT NULL,
    auth_tag BYTEA NOT NULL,
    key_version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- Create composite indexes for common queries
CREATE INDEX idx_bank_account_user_id ON bank_account(user_id);
CREATE INDEX idx_bank_account_sync_status ON bank_account(sync_status);
CREATE INDEX idx_transaction_account_id ON transaction(account_id);
CREATE INDEX idx_transaction_posted_date ON transaction(posted_date DESC);
CREATE INDEX idx_transaction_created_at ON transaction(created_at DESC);
CREATE INDEX idx_sync_job_account_id ON sync_job(account_id);
CREATE INDEX idx_sync_job_status ON sync_job(status);
CREATE INDEX idx_sync_job_created_at ON sync_job(created_at DESC);

-- Add comments for documentation
COMMENT ON TABLE bank_account IS 'Stores user-connected bank accounts from Plaid integration';
COMMENT ON TABLE transaction IS 'Stores individual transactions with deduplication via unique_hash';
COMMENT ON TABLE sync_job IS 'Tracks sync operation history and status for monitoring';
COMMENT ON TABLE encrypted_credential IS 'Stores encrypted Plaid access tokens with IV and auth tag';

-- Grant minimal privileges to app user
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO finance_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO finance_user;
