# Developer Quick Start: Bank Account Sync Feature

**Duration**: 15 minutes to first working API call  
**Prerequisites**: Docker, .NET 9 SDK, Node.js 18+, Git

---

## 1. Clone & Setup

```bash
# Clone repository
git clone https://github.com/finance-sentry/backend.git
cd finance-sentry

# Checkout feature branch
git checkout 001-bank-account-sync

# Copy environment template
cp .env.example .env

# Fill in Plaid credentials (from Plaid dashboard)
# PLAID_CLIENT_ID=...
# PLAID_SECRET=...
# PLAID_PUBLIC_KEY=...  (for Link UI)
```

---

## 2. Start Services (Docker)

```bash
# Start PostgreSQL + Redis + Plaid mock (localstack)
docker-compose up -d

# Verify services
docker ps
# Should see: postgres, redis, localstack (Plaid mock)

# Run database migrations
dotnet ef database update -p backend/src

# Verify database
docker exec finance-sentry-postgres psql -U postgres -d finance_sentry -c "SELECT * FROM bank_accounts LIMIT 5;"
```

---

## 3. Run Backend (.NET)

```bash
# Install dependencies
cd backend
dotnet restore

# Build
dotnet build

# Run (starts on http://localhost:5000)
dotnet run --project src/FinanceSentry.API/FinanceSentry.API.csproj

# In another terminal, verify API is running:
curl http://localhost:5000/health
# Response: { "status": "healthy" }
```

---

## 4. Run Frontend (Angular)

```bash
# In another terminal
cd frontend

# Install dependencies
npm install

# Start dev server (runs on http://localhost:4200)
ng serve

# Open browser: http://localhost:4200
```

---

## 5. Seed Test Data (Optional)

```bash
# Connect to database
docker exec -it finance-sentry-postgres psql -U postgres -d finance_sentry

# Insert test user
INSERT INTO users (user_id, email, created_at)
VALUES ('550e8400-e29b-41d4-a716-446655440000'::uuid, 'test@example.com', NOW());

# Insert test bank account (with Plaid item_id from sandbox)
INSERT INTO bank_accounts (
  account_id, user_id, plaid_item_id, bank_name, account_type,
  account_number_last4, owner_name, currency, current_balance,
  sync_status, created_at, updated_at
)
VALUES (
  '660e8500-f29c-52e5-b827-556766551111'::uuid,
  '550e8400-e29b-41d4-a716-446655440000'::uuid,
  'item-sandbox-test123',
  'AIB Ireland',
  'checking',
  '1234',
  'Test User',
  'EUR',
  5000.00,
  'active',
  NOW(),
  NOW()
);

# Exit psql
\q
```

---

## 6. Test Workflows

### Workflow 1: Connect a Bank Account

```bash
# Terminal 1: Backend running on :5000
# Terminal 2: Run this

# Step 1: Get Link Token (generate UI token for Plaid Link)
curl -X POST http://localhost:5000/api/bank-sync/accounts/connect \
  -H "Authorization: Bearer test-jwt-token" \
  -H "Content-Type: application/json" \
  -d '{}'

# Response:
# {
#   "linkToken": "link-sandbox-xxx",
#   "expiresIn": 600
# }

# Step 2: Exchange Public Token (after user completes Plaid Link)
# In real flow: frontend calls Plaid Link SDK with linkToken
# Then sends public_token to this endpoint
curl -X POST http://localhost:5000/api/bank-sync/accounts/link \
  -H "Authorization: Bearer test-jwt-token" \
  -H "Content-Type: application/json" \
  -d '{"publicToken": "public-sandbox-xxx"}'

# Response:
# {
#   "accountId": "660e8500-f29c-52e5-b827-556766551111",
#   "bankName": "AIB Ireland",
#   "accountType": "checking",
#   "syncStatus": "pending",
#   "message": "Account linked. Syncing initial transaction history..."
# }
```

### Workflow 2: List Connected Accounts

```bash
curl http://localhost:5000/api/bank-sync/accounts \
  -H "Authorization: Bearer test-jwt-token"

# Response:
# {
#   "accounts": [
#     {
#       "accountId": "660e8500-...",
#       "bankName": "AIB Ireland",
#       "currency": "EUR",
#       "currentBalance": 5000.00,
#       "syncStatus": "active",
#       "lastSyncTimestamp": "2026-03-21T12:35:00Z"
#     }
#   ],
#   "totalCount": 1
# }
```

### Workflow 3: Fetch Transactions

```bash
curl "http://localhost:5000/api/bank-sync/accounts/660e8500-f29c-52e5-b827-556766551111/transactions?start_date=2026-01-01&limit=10" \
  -H "Authorization: Bearer test-jwt-token"

# Response:
# {
#   "transactions": [
#     {
#       "transactionId": "abc12345-...",
#       "amount": 12.34,
#       "transactionType": "debit",
#       "postedDate": "2026-03-21",
#       "isPending": false,
#       "description": "Starbucks Dublin #1234",
#       "merchantCategory": "COFFEE_SHOPS"
#     }
#   ],
#   "pagination": {
#     "totalCount": 42,
#     "offset": 0,
#     "limit": 10,
#     "hasNextPage": true
#   }
# }
```

### Workflow 4: Trigger Manual Sync

```bash
curl -X POST http://localhost:5000/api/bank-sync/accounts/660e8500-f29c-52e5-b827-556766551111/sync \
  -H "Authorization: Bearer test-jwt-token" \
  -H "Content-Type: application/json" \
  -d '{}'

# Response:
# {
#   "syncJobId": "a1b2c3d4-e5f6-...",
#   "status": "in_progress",
#   "message": "Sync started. Fetching latest transactions...",
#   "estimatedCompletionTime": 5
# }
```

### Workflow 5: Check Dashboard/Aggregated View

```bash
curl "http://localhost:5000/api/bank-sync/dashboard/aggregated?start_date=2026-01-01&end_date=2026-03-21" \
  -H "Authorization: Bearer test-jwt-token"

# Response:
# {
#   "accountSummary": {
#     "totalActiveAccounts": 3,
#     "accountsByStatus": {
#       "active": 3,
#       "reauth_required": 0
#     }
#   },
#   "balanceByCurrency": {
#     "EUR": { "total": 5000.00 },
#     "GBP": { "total": 2000.00 }
#   },
#   "monthlyFlow": [
#     { "month": "2026-01", "inflows": 3000, "outflows": 1500, "netFlow": 1500 }
#   ]
# }
```

---

## 7. Run Tests

### Unit Tests

```bash
cd backend

# Run all unit tests
dotnet test tests/FinanceSentry.Tests.Unit/FinanceSentry.Tests.Unit.csproj

# Run specific test class
dotnet test tests/FinanceSentry.Tests.Unit/FinanceSentry.Tests.Unit.csproj -k "TransactionDeduplicationTests"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### Integration Tests

```bash
# Integration tests use testcontainers + real PostgreSQL
# Make sure Docker is running

dotnet test tests/FinanceSentry.Tests.Integration/FinanceSentry.Tests.Integration.csproj

# Run specific test
dotnet test tests/FinanceSentry.Tests.Integration/FinanceSentry.Tests.Integration.csproj -k "PlaidAdapterTests"
```

### End-to-End Tests (Frontend)

```bash
cd frontend

# Run Jasmine unit tests
ng test

# Run E2E tests (Cypress or Protractor)
ng e2e
```

---

## 8. Common Issues & Troubleshooting

### Issue: PostgreSQL container won't start

```bash
# Check Docker logs
docker logs finance-sentry-postgres

# Solution: Remove old volume and restart
docker-compose down -v
docker-compose up -d

# Verify database is ready
docker exec finance-sentry-postgres pg_isready -U postgres
```

### Issue: Plaid API calls return 401

```bash
# Solution: Verify credentials in .env
cat .env | grep PLAID_

# Make sure you're using Plaid sandbox credentials (for local testing)
# PLAID_CLIENT_ID=your_client_id_from_plaid_dashboard
# PLAID_SECRET=your_secret_from_plaid_dashboard

# If using real Plaid:
# Change PLAID_ENV=production in code (currently set to sandbox for local dev)
```

### Issue: JWT token validation fails

```bash
# Generate a valid test JWT token for local testing
# Use a JWT generator: https://jwt.io
# Header: { "alg": "HS256", "typ": "JWT" }
# Payload: { "sub": "550e8400-e29b-41d4-a716-446655440000", "exp": 2000000000 }
# Secret: (use same secret in backend appsettings.json)

# Then use in Authorization header:
curl http://localhost:5000/api/bank-sync/accounts \
  -H "Authorization: Bearer {generated-jwt-token}"
```

### Issue: Transaction deduplication not working

```bash
# Check SyncJob to verify deduplication counts
docker exec finance-sentry-postgres psql -U postgres -d finance_sentry -c \
  "SELECT sync_job_id, status, transaction_count_fetched, transaction_count_deduped FROM sync_jobs ORDER BY started_at DESC LIMIT 5;"

# Check unique_hash collisions
docker exec finance-sentry-postgres psql -U postgres -d finance_sentry -c \
  "SELECT unique_hash, COUNT(*) as count FROM transactions GROUP BY unique_hash HAVING COUNT(*) > 1;"
```

---

## 9. Debugging Tips

### Enable Verbose Logging

```bash
# Backend: Edit appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "FinanceSentry.Modules.BankSync": "Debug"
    }
  }
}

# Run backend with debug output
dotnet run --launch-profile Development
```

### Monitor Database Queries

```bash
# Enable PostgreSQL query logging
docker exec finance-sentry-postgres psql -U postgres -d finance_sentry -c \
  "ALTER DATABASE finance_sentry SET log_statement='all';"

# View logs
docker logs -f finance-sentry-postgres | grep "LOG:"
```

### Trace Sync Job Execution

```bash
# Get correlation ID from sync job
docker exec finance-sentry-postgres psql -U postgres -d finance_sentry -c \
  "SELECT sync_job_id, correlation_id, status, error_message FROM sync_jobs ORDER BY started_at DESC LIMIT 1;"

# Find all logs for that correlation ID
docker logs finance-sentry-api 2>&1 | grep "{correlation-id}"
```

---

## 10. Deployment Checklist

Before deploying to staging/production:

- [ ] All tests pass locally (`dotnet test`)
- [ ] No database migrations pending (`dotnet ef database update`)
- [ ] Environment variables configured (Plaid credentials, encryption keys)
- [ ] Secrets Manager set up (AWS Secrets Manager for encryption keys)
- [ ] API load tested (1000 concurrent requests)
- [ ] Webhook endpoint accessible (public URL registered in Plaid)
- [ ] Database backups configured
- [ ] Monitoring/alerting set up (APM tool, ELK stack)
- [ ] Rollback procedure tested
- [ ] Team lead approves deployment

---

## Quick Reference Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker logs finance-sentry-api -f

# Run migrations
dotnet ef database update -p backend/src

# Run tests
dotnet test

# Build Docker image
docker build -f backend/Dockerfile -t finance-sentry-api:latest .

# Push to registry
docker push finance-sentry-api:latest

# SSH into container
docker exec -it finance-sentry-postgres bash
```

---

## References

- **Plaid Docs**: https://plaid.com/docs/
- **EF Core Migrations**: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations
- **Angular CLI**: https://angular.io/cli
- **Docker Compose**: https://docs.docker.com/compose/
- **PostgreSQL**: https://www.postgresql.org/docs/
