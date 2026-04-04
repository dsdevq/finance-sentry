# Investment Tracking: Developer Quickstart Guide

**Document Version**: 1.0  
**Created**: 2026-03-21  
**Last Updated**: 2026-03-21  
**Target Audience**: Backend (.NET) and Frontend (Angular) developers  

---

## Table of Contents

1. [Environment Setup](#environment-setup)
2. [Local Development](#local-development)
3. [Connecting to Binance Testnet](#connecting-to-binance-testnet)
4. [Connecting to Interactive Brokers Demo](#connecting-to-interactive-brokers-demo)
5. [Testing Multi-Source Aggregation](#testing-multi-source-aggregation)
6. [Running Tests](#running-tests)
7. [Troubleshooting](#troubleshooting)

---

## Environment Setup

### Prerequisites

- **Backend**: .NET 9 SDK, Docker, Docker Compose
- **Frontend**: Node.js 20+, npm 10+
- **Database**: PostgreSQL 14+ (via Docker)
- **Cache**: Redis (via Docker)
- **Editors**: VS Code or Visual Studio 2022

### Clone Repository

```bash
cd C:\Users\denys\Work\finance-sentry
git clone <repo-url>
cd finance-sentry
```

### Environment Variables

Create `.env` file in project root:

```env
# Backend
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:5001
DATABASE_URL=Server=localhost:5432;Database=finance_sentry;User Id=postgres;Password=postgres;
REDIS_URL=localhost:6379
JWT_SECRET=your-secret-key-here-min-32-characters
ENCRYPTION_KEY=your-encryption-key-32-chars

# APIs
BINANCE_API_KEY=your-testnet-key
BINANCE_API_SECRET=your-testnet-secret
OPENAI_API_KEY=your-openai-key
ANTHROPIC_API_KEY=your-anthropic-key

# Frontend
ANGULAR_API_URL=https://localhost:5001/api
ANGULAR_ENV=development
```

### Docker Compose (Local Services)

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: finance_sentry
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  # Optional: IB Gateway for development
  ibgateway:
    image: ghcr.io/waytrade/ib-gateway:stable
    ports:
      - "5000:5000"
    environment:
      IBCPORTPAPER: 5000

volumes:
  postgres_data:
```

**Start services**:

```bash
docker-compose up -d
```

**Verify services are running**:

```bash
# Test PostgreSQL
psql -h localhost -U postgres -d finance_sentry -c "SELECT 1"

# Test Redis
redis-cli ping  # Should return PONG

# Test IB Gateway
curl http://localhost:5000/v1/api/portfolio/accounts  # Should return 401 or valid response
```

---

## Local Development

### Backend Setup (.NET)

```bash
cd backend

# Restore dependencies
dotnet restore

# Apply migrations
dotnet ef database update

# Build
dotnet build

# Run development server
dotnet run --project FinanceSentry.API
```

**Backend should be running at**: `https://localhost:5001`

### Frontend Setup (Angular)

```bash
cd frontend

# Install dependencies
npm install

# Start development server
ng serve

# Open browser: http://localhost:4200
```

**Frontend should be running at**: `http://localhost:4200`

### Testing API Connectivity

```bash
# Get JWT token (adjust endpoint/credentials as needed)
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' \
  -k  # Skip SSL verification for local development

# Use token to test portfolio endpoint
curl -H "Authorization: Bearer {token}" \
  https://localhost:5001/api/v1/portfolio/ \
  -k
```

---

## Connecting to Binance Testnet

### Step 1: Create Testnet Account

1. Visit: https://testnet.binance.vision
2. Click "Generate HMAC_SHA256 Key"
3. Save API Key and Secret

### Step 2: Fund Testnet Account

1. Click "Deposit" on testnet website
2. Transfer fake USDT and BTC to your account
3. Should see balances in testnet

### Step 3: Configure Application

Update `.env`:

```env
BINANCE_API_KEY=your-testnet-api-key
BINANCE_API_SECRET=your-testnet-api-secret
BINANCE_USE_TESTNET=true
BINANCE_BASE_URL=https://testnet.binance.vision/api
```

### Step 4: Test Connection

```bash
# Backend logs should show:
# [INFO] Connecting to Binance Testnet
# [DEBUG] Account balance: BTC=1.5, USDT=5000

curl -H "Authorization: Bearer {token}" \
  -X POST https://localhost:5001/api/v1/portfolio/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Testnet Binance",
    "platform": "binance",
    "apiKey": "your-testnet-key",
    "apiSecret": "your-testnet-secret"
  }' \
  -k
```

### Expected Response

```json
{
  "data": {
    "id": "uuid-123",
    "name": "My Testnet Binance",
    "platform": "binance",
    "status": "pending",
    "message": "Account registered. Starting initial sync..."
  }
}
```

### Polling for Sync Status

```bash
# Wait 5-10 seconds, then check sync status
curl -H "Authorization: Bearer {token}" \
  https://localhost:5001/api/v1/portfolio/accounts/uuid-123 \
  -k

# Response should show:
# "status": "active"
# "holdingsCount": 2
# "lastSyncStatus": "success"
```

---

## Connecting to Interactive Brokers Demo

### Step 1: Create Demo Account

1. Visit: https://www.interactivebrokers.com/en/index.php?f=45631
2. Fill out form, use demo username/password
3. Access demo trading platform

### Step 2: Enable IB Gateway API

1. Download and install IB Trader Workstation or IB Gateway
2. Login with demo credentials
3. Go to Edit → Settings → API
4. Enable API and set port to 7497 (or 5000 if using Docker)

### Step 3: Configure Application

Update `.env`:

```env
IB_USE_DEMO=true
IB_API_URL=http://localhost:5000
IB_USERNAME=your-demo-username
IB_PASSWORD=your-demo-password
```

### Step 4: Test Connection

```bash
# Start IB Gateway (or use Docker container)
# Then test connection:

curl -H "Authorization: Bearer {token}" \
  -X POST https://localhost:5001/api/v1/portfolio/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My IB Demo Account",
    "platform": "interactive_brokers",
    "accountId": "DU123456",
    "apiKey": "",
    "apiSecret": ""
  }' \
  -k
```

### Setup with Docker

```yaml
# Add to docker-compose.yml
ibgateway:
  image: ghcr.io/waytrade/ib-gateway:stable
  ports:
    - "5000:5000"
  environment:
    IBCPORTPAPER: 5000
    TWS_USERID: your-demo-username
    TWS_PASSWORD: your-demo-password
  restart: always
```

---

## Testing Multi-Source Aggregation

### Scenario: Connect Both Binance and Interactive Brokers

**Step 1**: Connect Binance testnet (see above)

**Step 2**: Connect IB demo account (see above)

**Step 3**: Verify both accounts are synced

```bash
curl -H "Authorization: Bearer {token}" \
  https://localhost:5001/api/v1/portfolio/accounts?page=1&pageSize=10 \
  -k

# Should return 2 accounts:
# - My Testnet Binance (status: active, 2 holdings)
# - My IB Demo Account (status: active, 5 holdings)
```

**Step 4**: View aggregated portfolio

```bash
curl -H "Authorization: Bearer {token}" \
  https://localhost:5001/api/v1/portfolio/ \
  -k

# Response should show:
# {
#   "totalValue": 300000.00,
#   "numberOfAssets": 7,
#   "numberOfPlatforms": 2,
#   "allocationByPlatform": {
#     "binance": 33,
#     "interactive_brokers": 67
#   }
# }
```

**Step 5**: View all holdings

```bash
curl -H "Authorization: Bearer {token}" \
  https://localhost:5001/api/v1/portfolio/holdings?page=1&pageSize=50 \
  -k

# Should return mixed holdings from both platforms
```

### Testing Portfolio Analysis

```bash
# Request portfolio-level AI analysis
curl -H "Authorization: Bearer {token}" \
  -X POST https://localhost:5001/api/v1/portfolio/analysis/portfolio \
  -H "Content-Type: application/json" \
  -d '{"analysisType":"portfolio_analysis"}' \
  -k

# Poll for results after 3-5 seconds
```

---

## Running Tests

### Backend Tests

```bash
cd backend

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter ClassName=PortfolioControllerTests

# Run with code coverage
dotnet test /p:CollectCoverage=true

# Watch mode (auto-rerun on changes)
dotnet watch test
```

### Frontend Tests

```bash
cd frontend

# Run all tests
ng test

# Run specific test file
ng test --include='**/portfolio.service.spec.ts'

# Run with code coverage
ng test --code-coverage

# Run in headless mode (CI)
ng test --browsers=ChromeHeadless --watch=false
```

### Integration Tests

Test multi-source aggregation end-to-end:

```bash
cd backend

# Run integration tests only
dotnet test --filter Category=Integration

# Expected: All accounts sync, all holdings aggregated, metrics calculated correctly
```

---

## Troubleshooting

### Common Issues

#### 1. **Database Connection Failed**

```
Error: Cannot connect to database at localhost:5432
```

**Solution**:
```bash
# Check PostgreSQL is running
docker-compose ps

# If stopped, start it
docker-compose up -d postgres

# Check logs
docker-compose logs postgres
```

#### 2. **Binance API Key Invalid**

```
Error: Unauthorized - Invalid API key or signature
```

**Solution**:
- Verify API key/secret in `.env`
- Ensure testnet credentials (not production)
- Check IP whitelist in Binance settings
- Regenerate keys if needed

#### 3. **IB Gateway Connection Refused**

```
Error: Connection refused localhost:5000
```

**Solution**:
```bash
# Verify IB Gateway is running
curl http://localhost:5000/

# If not running, start Docker container
docker-compose up -d ibgateway

# Check logs
docker-compose logs ibgateway

# Verify credentials in .env are correct
```

#### 4. **Redis Connection Error**

```
Error: Cannot connect to Redis at localhost:6379
```

**Solution**:
```bash
# Start Redis
docker-compose up -d redis

# Verify it's running
redis-cli ping  # Should return PONG
```

### Performance Issues

#### Slow Portfolio Load

If portfolio endpoint takes > 5 seconds:

1. Check Redis is running: `redis-cli info`
2. Check database query performance: `EXPLAIN ANALYZE SELECT ...`
3. Verify indexes exist on frequently queried tables
4. Check if sync jobs are running (try manual sync)

#### Sync Job Hangs

If sync takes > 30 seconds:

1. Check API connectivity: `curl https://api.binance.com/api/v3/ping`
2. Check rate limits: Review API response headers
3. Review backend logs for timeouts
4. Increase request timeout in `appsettings.json`

### Debugging

#### Enable Verbose Logging

Update `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "FinanceSentry": "Debug"
    }
  }
}
```

#### Check Database State

```bash
# Connect to PostgreSQL
psql -h localhost -U postgres -d finance_sentry

# View recent sync jobs
SELECT id, account_id, status, error_message, created_at 
FROM sync_jobs 
ORDER BY created_at DESC 
LIMIT 10;

# View holdings for user
SELECT symbol, quantity, current_price, current_value 
FROM asset_holdings 
WHERE user_id = 'user-uuid' 
ORDER BY current_value DESC;
```

#### Test Cache

```bash
# Connect to Redis CLI
redis-cli

# Check cached data
GET "user:uuid:portfolio:holdings"
GET "crypto:BTC"

# Clear cache if needed
FLUSHDB
```

---

## Next Steps

After successful setup:

1. **Explore API Contracts**: Read binance-api.md, interactive-brokers-api.md
2. **Review Data Model**: Read data-model.md for schema and relationships
3. **Study Research**: Read research.md for architectural decisions (excluding deferred AI decisions)
4. **Start Implementation**: Follow tasks.md for feature development
5. **Run Full Test Suite**: Ensure all tests pass before submitting PR

---

## Additional Resources

- [Binance API Docs](https://binance-docs.github.io/apidocs/)
- [Interactive Brokers API](https://www.interactivebrokers.com/en/index.php?f=5041)
- [OpenAI API Docs](https://platform.openai.com/docs/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Angular Documentation](https://angular.io/docs)

---

## Support & Questions

- **Backend Issues**: Check backend/README.md
- **Frontend Issues**: Check frontend/README.md
- **Database Issues**: Check specs/002-investment-tracking/data-model.md
- **API Contracts**: Check specs/002-investment-tracking/*.md

Good luck with development! 🚀

