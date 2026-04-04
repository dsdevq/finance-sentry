# Feature 002: Multi-Source Investment Tracking - Tasks

**Feature**: Investment Tracking (Binance + Interactive Brokers)  
**Tech Stack**: .NET 9 backend, Angular 20+ frontend, PostgreSQL database  
**AI Integration**: DEFERRED to Phase 2 (future work - see Phase 6)  
**MVP Scope**: Phase 1-2 infrastructure + Phase 3 (US1 Binance only)  

---

## Phase 1: Setup & Scaffolding

Project initialization, database schema, Entity Framework, and frontend module structure.

### Backend Project Structure
- [ ] T001 Create .NET 9 class library `backend/src/Modules/Investment/Investment.Domain.csproj`
- [ ] T002 Create .NET 9 class library `backend/src/Modules/Investment/Investment.Application.csproj`
- [ ] T003 Create .NET 9 class library `backend/src/Modules/Investment/Investment.Infrastructure.csproj`
- [ ] T004 Create ASP.NET Core project reference in `backend/src/Api/Api.csproj` for Investment module
- [ ] T005 Add .NET solution folder structure: Domain/, Application/, Infrastructure/ under `backend/src/Modules/Investment/`

### Database Schema & Migrations
- [ ] T006 Create Investment context class in `backend/src/Modules/Investment/Infrastructure/Persistence/InvestmentContext.cs`
- [ ] T007 Create `InvestmentAccount` entity in `backend/src/Modules/Investment/Domain/Entities/InvestmentAccount.cs` with fields: Id, UserId, AccountType (Binance/InteractiveBrokers), ExchangeName, PublicKey, EncryptedSecretKey, ConnectionStatus, CreatedAt, UpdatedAt
- [ ] T008 Create `AssetHolding` entity in `backend/src/Modules/Investment/Domain/Entities/AssetHolding.cs` with fields: Id, AccountId, Symbol, Quantity, CurrentPrice, Value, LastUpdatedAt, Source
- [ ] T009 Create `PriceHistory` entity in `backend/src/Modules/Investment/Domain/Entities/PriceHistory.cs` with fields: Id, Symbol, ExchangeSource, Price, Timestamp, Currency
- [ ] T010 Create `SyncJob` entity in `backend/src/Modules/Investment/Domain/Entities/SyncJob.cs` with fields: Id, AccountId, StartedAt, CompletedAt, Status, ErrorMessage, HoldingsCount
- [ ] T011 Create `PortfolioMetric` entity in `backend/src/Modules/Investment/Domain/Entities/PortfolioMetric.cs` with fields: Id, UserId, TotalValue, AllocationByExchange (JSON), RiskScore, CalculatedAt
- [ ] T012 Create Entity Framework migrations file `backend/src/Modules/Investment/Infrastructure/Migrations/001_InitialSchema.cs` with all entities
- [ ] T013 Create `DbContextExtensions` in `backend/src/Modules/Investment/Infrastructure/Persistence/DbContextExtensions.cs` for seeding and startup

### Dependency Injection & Module Setup
- [ ] T014 Create `InvestmentModule.cs` in `backend/src/Modules/Investment/InvestmentModule.cs` for DI registration
- [ ] T015 Register Investment.Domain, Application, Infrastructure in Api project Startup
- [ ] T016 Create `IRepository<T>` interface in `backend/src/Modules/Investment/Domain/Repositories/IRepository.cs`
- [ ] T017 Create generic `EfRepository<T>` implementation in `backend/src/Modules/Investment/Infrastructure/Repositories/EfRepository.cs`

### Frontend Module Creation
- [ ] T018 Generate Angular module `frontend/src/app/modules/investment-tracking/investment-tracking.module.ts`
- [ ] T019 Create routing module `frontend/src/app/modules/investment-tracking/investment-tracking-routing.module.ts` with routes: /accounts, /portfolio, /holdings/:id
- [ ] T020 Create shared components folder structure: `frontend/src/app/modules/investment-tracking/components/shared/`
- [ ] T021 Create services folder: `frontend/src/app/modules/investment-tracking/services/`

---

## Phase 2: Foundational Infrastructure

Core services for encryption, retry logic, caching, scheduling, and error handling.

### Encryption Service
- [ ] T022 [P] Create `IEncryptionService` interface in `backend/src/Modules/Investment/Domain/Services/IEncryptionService.cs`
- [ ] T023 [P] Implement AES-256 encryption service in `backend/src/Modules/Investment/Infrastructure/Services/AesEncryptionService.cs` with Encrypt(plaintext) and Decrypt(ciphertext) methods
- [ ] T024 [P] Add encryption service to DI in `InvestmentModule.cs`
- [ ] T025 [P] Unit test AES encryption in `backend/tests/Modules/Investment/Infrastructure/Services/AesEncryptionServiceTests.cs`

### Retry Policy & Resilience
- [ ] T026 [P] Add Polly NuGet package to `Investment.Infrastructure.csproj`
- [ ] T027 [P] Create `IHttpClientFactory` registration with retry policy in `backend/src/Modules/Investment/Infrastructure/Http/HttpClientConfiguration.cs` (exponential backoff: max 3 retries, 1s base delay)
- [ ] T028 [P] Create `ResiliencePolicy.cs` in `backend/src/Modules/Investment/Infrastructure/Policies/ResiliencePolicy.cs` with Polly pipeline configuration
- [ ] T029 [P] Unit test retry logic in `backend/tests/Modules/Investment/Infrastructure/Policies/ResiliencePolicyTests.cs`

### Redis Caching Service
- [ ] T030 [P] Add StackExchange.Redis NuGet package to `Investment.Infrastructure.csproj`
- [ ] T031 [P] Create `ICacheService` interface in `backend/src/Modules/Investment/Domain/Services/ICacheService.cs` with Get, Set, Remove, Exists methods
- [ ] T032 [P] Implement `RedisCacheService` in `backend/src/Modules/Investment/Infrastructure/Services/RedisCacheService.cs` with TTL support
- [ ] T033 [P] Register Redis in DI with connection string from `appsettings.json`
- [ ] T034 [P] Unit test cache service in `backend/tests/Modules/Investment/Infrastructure/Services/RedisCacheServiceTests.cs`
- [ ] T035 [P] Integration test Redis connectivity in `backend/tests/Modules/Investment/Infrastructure/Services/RedisCacheServiceIntegrationTests.cs`

### Hangfire Job Scheduling
- [ ] T036 [P] Add Hangfire NuGet package to `Investment.Infrastructure.csproj`
- [ ] T037 [P] Configure Hangfire in `backend/src/Api/Startup.cs` with PostgreSQL storage
- [ ] T038 [P] Create `ISyncJobService` interface in `backend/src/Modules/Investment/Application/Services/ISyncJobService.cs`
- [ ] T039 [P] Implement `SyncJobService` in `backend/src/Modules/Investment/Application/Services/SyncJobService.cs` with ScheduleSyncJob(accountId, interval) method
- [ ] T040 [P] Create recurring job registration for crypto sync (15-min interval) in `backend/src/Modules/Investment/Infrastructure/Jobs/CryptoSyncJob.cs`
- [ ] T041 [P] Create recurring job registration for stock sync (60-min interval) in `backend/src/Modules/Investment/Infrastructure/Jobs/StockSyncJob.cs`

### Error Handling & Logging
- [ ] T042 [P] Create `DomainException` base class in `backend/src/Modules/Investment/Domain/Exceptions/DomainException.cs`
- [ ] T043 [P] Create `SyncFailedException` in `backend/src/Modules/Investment/Domain/Exceptions/SyncFailedException.cs`
- [ ] T044 [P] Create `InvalidAccountException` in `backend/src/Modules/Investment/Domain/Exceptions/InvalidAccountException.cs`
- [ ] T045 [P] Create custom `ErrorHandlingMiddleware` in `backend/src/Api/Middleware/ErrorHandlingMiddleware.cs` to catch and log exceptions
- [ ] T046 [P] Configure logging in `backend/src/Api/Startup.cs` with Serilog (file + console sinks)
- [ ] T047 [P] Create logger configuration in `backend/src/Api/Configuration/LoggingConfiguration.cs`

### Exchange Rate & Conversion Service
- [ ] T048 [P] Create `IExchangeRateService` interface in `backend/src/Modules/Investment/Domain/Services/IExchangeRateService.cs`
- [ ] T049 [P] Implement `ExchangeRateService` in `backend/src/Modules/Investment/Application/Services/ExchangeRateService.cs` (cached calls to external API, default fallback rates)
- [ ] T050 [P] Unit test conversion logic in `backend/tests/Modules/Investment/Application/Services/ExchangeRateServiceTests.cs`

---

## Phase 3 (US1): Binance Integration - Connect & View Holdings

Connect to Binance API, fetch holdings, display real-time prices.

### Backend: Binance Adapter
- [ ] T051 [US1] Create `IBinanceApiAdapter` interface in `backend/src/Modules/Investment/Domain/Adapters/IBinanceApiAdapter.cs` with methods: ValidateCredentials, GetBalances, GetTickerPrices
- [ ] T052 [US1] Implement `BinanceApiAdapter` in `backend/src/Modules/Investment/Infrastructure/Adapters/BinanceApiAdapter.cs` with Binance REST API client calls (spot wallet endpoint, prices endpoint)
- [ ] T053 [US1] Add Binance API models in `backend/src/Modules/Investment/Infrastructure/Adapters/Models/BinanceModels.cs`: AccountResponse, BalanceModel, TickerModel
- [ ] T054 [US1] Unit test BinanceApiAdapter in `backend/tests/Modules/Investment/Infrastructure/Adapters/BinanceApiAdapterTests.cs` with mock API responses
- [ ] T055 [US1] Integration test BinanceApiAdapter with Binance Testnet in `backend/tests/Modules/Investment/Infrastructure/Adapters/BinanceApiAdapterIntegrationTests.cs`

### Backend: Investment Services
- [ ] T056 [US1] Create `IInvestmentAccountService` interface in `backend/src/Modules/Investment/Application/Services/IInvestmentAccountService.cs` with methods: CreateAccount, ListAccounts, GetAccountById, ValidateConnection
- [ ] T057 [US1] Implement `InvestmentAccountService` in `backend/src/Modules/Investment/Application/Services/InvestmentAccountService.cs` with account creation and credential encryption
- [ ] T058 [US1] Create `IAssetHoldingService` interface in `backend/src/Modules/Investment/Application/Services/IAssetHoldingService.cs` with methods: FetchHoldings, UpdatePrices, GetHoldingsByAccount
- [ ] T059 [US1] Implement `AssetHoldingService` in `backend/src/Modules/Investment/Application/Services/AssetHoldingService.cs` that calls BinanceAdapter and persists holdings
- [ ] T060 [US1] Unit test InvestmentAccountService in `backend/tests/Modules/Investment/Application/Services/InvestmentAccountServiceTests.cs`
- [ ] T061 [US1] Unit test AssetHoldingService in `backend/tests/Modules/Investment/Application/Services/AssetHoldingServiceTests.cs`

### Backend: REST Endpoints
- [ ] T062 [US1] Create `InvestmentAccountsController` in `backend/src/Api/Controllers/InvestmentAccountsController.cs`
- [ ] T063 [US1] Implement `POST /api/investment-accounts` (CreateAccount) endpoint with AccountType, ExchangeName, PublicKey, SecretKey, request validation
- [ ] T064 [US1] Implement `GET /api/investment-accounts` (ListAccounts) endpoint with UserId filter, pagination
- [ ] T065 [US1] Implement `GET /api/investment-accounts/{id}` (GetAccountById) endpoint with holdings summary
- [ ] T066 [US1] Implement `POST /api/investment-accounts/{id}/sync` (ManualSync) endpoint that triggers sync job
- [ ] T067 [US1] Unit test controllers in `backend/tests/Api/Controllers/InvestmentAccountsControllerTests.cs`

### Frontend: Account Connection Page
- [ ] T068 [US1] Create `connect-account.component.ts` in `frontend/src/app/modules/investment-tracking/components/connect-account/`
- [ ] T069 [US1] Create `connect-account.component.html` with form: exchange dropdown (Binance/IB), API key input, secret key input, submit button
- [ ] T070 [US1] Create `connect-account.component.scss` with responsive styling
- [ ] T071 [US1] Create `InvestmentAccountService` (Angular) in `frontend/src/app/modules/investment-tracking/services/investment-account.service.ts` with createAccount(), listAccounts(), getAccount() methods
- [ ] T072 [US1] Add form validation and error display in connect-account component
- [ ] T073 [US1] Add success notification after account creation

### Frontend: Holdings Display
- [ ] T074 [US1] Create `holdings-list.component.ts` in `frontend/src/app/modules/investment-tracking/components/holdings-list/`
- [ ] T075 [US1] Create `holdings-list.component.html` with table: Symbol, Quantity, Current Price, Total Value, Last Updated
- [ ] T076 [US1] Create `holdings-list.component.scss` with responsive table styling
- [ ] T077 [US1] Implement real-time price updates using polling (WebSocket optional for MVP) every 30 seconds
- [ ] T078 [US1] Create `holding.model.ts` in `frontend/src/app/modules/investment-tracking/models/`
- [ ] T079 [US1] Add manual sync button and loading state in holdings component
- [ ] T080 [US1] Unit test holdings component in `frontend/tests/modules/investment-tracking/components/holdings-list.component.spec.ts`

### Frontend: Account Details Page
- [ ] T081 [US1] Create `account-details.component.ts` in `frontend/src/app/modules/investment-tracking/components/account-details/`
- [ ] T082 [US1] Display account info: Exchange name, connection status, last sync time, total holdings value
- [ ] T083 [US1] Add link to full holdings list from account details
- [ ] T084 [US1] Add disconnect button (soft delete) with confirmation

### Integration Tests (US1)
- [ ] T085 [US1] Create end-to-end test scenario in `backend/tests/Modules/Investment/Integration/BinanceE2ETests.cs`: create account → validate → fetch holdings → verify persistence
- [ ] T086 [US1] Create frontend integration test in `frontend/tests/modules/investment-tracking/integration/binance-flow.spec.ts`

---

## Phase 4 (US2): Interactive Brokers Integration - Aggregate Holdings

Connect to Interactive Brokers, aggregate multi-source holdings, calculate allocation and risk metrics.

### Backend: Interactive Brokers Adapter
- [ ] T087 [US2] Create `IInteractiveBrokersAdapter` interface in `backend/src/Modules/Investment/Domain/Adapters/IInteractiveBrokersAdapter.cs` with methods: ValidateCredentials, GetAccounts, GetPositions, Authenticate
- [ ] T088 [US2] Implement `InteractiveBrokersAdapter` in `backend/src/Modules/Investment/Infrastructure/Adapters/InteractiveBrokersAdapter.cs` with OAuth 2.0 flow and IB API Gateway calls
- [ ] T089 [US2] Add Interactive Brokers API models in `backend/src/Modules/Investment/Infrastructure/Adapters/Models/InteractiveBrokersModels.cs`: AccountResponse, PositionModel, SecurityModel
- [ ] T090 [US2] Implement OAuth token refresh mechanism in `backend/src/Modules/Investment/Infrastructure/Adapters/InteractiveBrokersAdapter.cs`
- [ ] T091 [US2] Unit test InteractiveBrokersAdapter in `backend/tests/Modules/Investment/Infrastructure/Adapters/InteractiveBrokersAdapterTests.cs` with mock responses
- [ ] T092 [US2] Integration test with IB Demo account in `backend/tests/Modules/Investment/Infrastructure/Adapters/InteractiveBrokersAdapterIntegrationTests.cs`

### Backend: Aggregation & Risk Services
- [ ] T093 [US2] Create `IPortfolioAggregationService` interface in `backend/src/Modules/Investment/Application/Services/IPortfolioAggregationService.cs` with methods: AggregateHoldings, CalculateAllocations, SumByExchange, SumByCurrency
- [ ] T094 [US2] Implement `PortfolioAggregationService` in `backend/src/Modules/Investment/Application/Services/PortfolioAggregationService.cs` that merges Binance + IB holdings by symbol/currency
- [ ] T095 [US2] Create `IRiskMetricsService` interface in `backend/src/Modules/Investment/Application/Services/IRiskMetricsService.cs` with methods: CalculateVolatility, CalculateDiversification, CalculateConcentration
- [ ] T096 [US2] Implement `RiskMetricsService` in `backend/src/Modules/Investment/Application/Services/RiskMetricsService.cs` with basic metrics: position concentration %, asset class diversification, exchange concentration
- [ ] T097 [US2] Create `IPortfolioMetricService` interface in `backend/src/Modules/Investment/Application/Services/IPortfolioMetricService.cs` with methods: CalculateDailyMetrics, GetMetricsHistory
- [ ] T098 [US2] Implement `PortfolioMetricService` in `backend/src/Modules/Investment/Application/Services/PortfolioMetricService.cs` for daily portfolio calculations
- [ ] T099 [US2] Unit test aggregation service in `backend/tests/Modules/Investment/Application/Services/PortfolioAggregationServiceTests.cs`
- [ ] T100 [US2] Unit test risk metrics in `backend/tests/Modules/Investment/Application/Services/RiskMetricsServiceTests.cs`

### Backend: REST Endpoints (Multi-Source)
- [ ] T101 [US2] Create `PortfolioController` in `backend/src/Api/Controllers/PortfolioController.cs`
- [ ] T102 [US2] Implement `GET /api/portfolio/summary` endpoint: total value, value by exchange, value by currency
- [ ] T103 [US2] Implement `GET /api/portfolio/allocations` endpoint: % allocation by asset class, exchange, currency
- [ ] T104 [US2] Implement `GET /api/portfolio/metrics` endpoint: risk score, diversification index, concentration ratio
- [ ] T105 [US2] Implement `GET /api/portfolio/holdings-aggregated` endpoint: all holdings from all accounts merged by symbol
- [ ] T106 [US2] Implement `GET /api/portfolio/history` endpoint: historical metrics with date range filter
- [ ] T107 [US2] Unit test portfolio endpoints in `backend/tests/Api/Controllers/PortfolioControllerTests.cs`

### Frontend: Portfolio Dashboard
- [ ] T108 [US2] Create `portfolio-dashboard.component.ts` in `frontend/src/app/modules/investment-tracking/components/portfolio-dashboard/`
- [ ] T109 [US2] Create `portfolio-dashboard.component.html` with layout: summary cards (total value, accounts), allocation chart, risk widget, holdings table
- [ ] T110 [US2] Create `portfolio-dashboard.component.scss` with responsive grid layout
- [ ] T111 [US2] Implement summary cards showing total portfolio value, allocation by exchange, allocation by currency
- [ ] T112 [US2] Add pie/donut charts for allocations using ng-apexcharts or similar
- [ ] T113 [US2] Display risk metrics card: risk score, diversification index, concentration alerts
- [ ] T114 [US2] Create `PortfolioService` (Angular) in `frontend/src/app/modules/investment-tracking/services/portfolio.service.ts` with getSummary(), getAllocations(), getMetrics() methods
- [ ] T115 [US2] Add real-time update polling for dashboard data every 60 seconds
- [ ] T116 [US2] Unit test portfolio dashboard in `frontend/tests/modules/investment-tracking/components/portfolio-dashboard.component.spec.ts`

### Frontend: Multi-Source Accounts View
- [ ] T117 [US2] Update `holdings-list.component.ts` to show source column (Binance/IB)
- [ ] T118 [US2] Add account filter dropdown to holdings list
- [ ] T119 [US2] Create `accounts-panel.component.ts` in `frontend/src/app/modules/investment-tracking/components/accounts-panel/` to list all connected accounts with last sync status
- [ ] T120 [US2] Add bulk sync button in accounts panel (sync all accounts)
- [ ] T121 [US2] Update routing to include /portfolio and /accounts sub-routes

### Integration Tests (US2)
- [ ] T122 [US2] Create end-to-end aggregation test in `backend/tests/Modules/Investment/Integration/MultiSourceAggregationE2ETests.cs`: Binance + IB → aggregate → calculate metrics → verify allocations
- [ ] T123 [US2] Create frontend integration test for portfolio dashboard in `frontend/tests/modules/investment-tracking/integration/portfolio-dashboard.spec.ts`

---

## Phase 5: Polish & Cross-Cutting Concerns

Error handling, rate limiting, validation, documentation, load testing, and health checks.

### Error Handling & Input Validation
- [ ] T124 Create `ValidationBehavior<TRequest, TResponse>` in `backend/src/Modules/Investment/Application/Behaviors/ValidationBehavior.cs` for MediatR pipeline
- [ ] T125 Add FluentValidation NuGet package to `Investment.Application.csproj`
- [ ] T126 Create validators for CreateAccountCommand in `backend/src/Modules/Investment/Application/Commands/CreateAccountCommandValidator.cs`
- [ ] T127 Create validators for SyncAccountCommand in `backend/src/Modules/Investment/Application/Commands/SyncAccountCommandValidator.cs`
- [ ] T128 Create custom exception filter `HttpExceptionFilter` in `backend/src/Api/Filters/HttpExceptionFilter.cs` with proper HTTP status mapping
- [ ] T129 Update `ErrorHandlingMiddleware` to include request correlation ID for tracing

### Rate Limiting
- [ ] T130 Add AspNetCoreRateLimit NuGet package to `Api.csproj`
- [ ] T131 Configure rate limiting in `backend/src/Api/Startup.cs`: 100 req/min per user, 10 req/min per IP for /sync endpoints
- [ ] T132 Create `RateLimitPolicy.cs` in `backend/src/Api/Configuration/RateLimitPolicy.cs` with specific rules per endpoint
- [ ] T133 Test rate limiting in `backend/tests/Api/RateLimitingTests.cs` with simulated concurrent requests

### API Documentation
- [ ] T134 Add Swashbuckle NuGet package to `Api.csproj` (Swagger)
- [ ] T135 Configure Swagger in `backend/src/Api/Startup.cs` with Investment API section
- [ ] T136 Add XML documentation comments to all controllers and endpoints: `/// <summary>`, `/// <param>`, `/// <response>`
- [ ] T137 Add Swagger annotations to controllers: `[OpenApiTag]`, `[OpenApiOperation]`
- [ ] T138 Generate Swagger JSON in `backend/docs/investment-api-swagger.json`
- [ ] T139 Create API documentation markdown in `backend/docs/INVESTMENT_API.md` with examples for each endpoint

### Load Testing
- [ ] T140 Create k6 load test script in `backend/tests/loadtests/holdings-fetch.js`: simulate 100 concurrent users fetching holdings over 2 minutes
- [ ] T141 Create k6 load test script in `backend/tests/loadtests/portfolio-summary.js`: simulate 50 concurrent users fetching portfolio summary over 2 minutes
- [ ] T142 Run load tests and document results in `backend/tests/loadtests/LOAD_TEST_RESULTS.md`

### Health Check & Monitoring
- [ ] T143 Create `HealthCheckController` in `backend/src/Api/Controllers/HealthCheckController.cs`
- [ ] T144 Implement `GET /health` endpoint: app status, database connection, Redis connection, external API connectivity
- [ ] T145 Add health check configuration in `backend/src/Api/Startup.cs` with HealthChecks.SqlServer and HealthChecks.Redis
- [ ] T146 Create monitoring dashboard placeholder documentation in `backend/docs/MONITORING.md`

### Security Hardening
- [ ] T147 Add HTTPS requirement in `backend/src/Api/Startup.cs` with HSTS headers
- [ ] T148 Add CORS policy for frontend domain in `backend/src/Api/Startup.cs`
- [ ] T149 Create security headers middleware in `backend/src/Api/Middleware/SecurityHeadersMiddleware.cs` (X-Content-Type-Options, X-Frame-Options, CSP)
- [ ] T150 Verify no secrets (API keys, connection strings) logged via `SecretsMaskingFormatter.cs` in logging pipeline
- [ ] T151 Unit test secret masking in `backend/tests/Logging/SecretsMaskingFormatterTests.cs`

### Frontend Enhancements
- [ ] T152 Add global error snackbar/toast component in `frontend/src/app/shared/components/error-notification/`
- [ ] T153 Implement HTTP interceptor for error handling in `frontend/src/app/core/interceptors/error.interceptor.ts`
- [ ] T154 Add loading spinners to all async operations (account creation, sync, data fetch)
- [ ] T155 Create responsive mobile layout for portfolio dashboard and holdings list
- [ ] T156 Add unit tests for error interceptor in `frontend/tests/core/interceptors/error.interceptor.spec.ts`
- [ ] T157 Add end-to-end Cypress tests in `frontend/tests/e2e/investment-tracking.cy.ts`: create account → view holdings → check portfolio

### Documentation & Guides
- [ ] T158 Update `quickstart.md` in `specs/002-investment-tracking/` with setup steps (no AI/LLM sections)
- [ ] T159 Create `IMPLEMENTATION_GUIDE.md` in `specs/002-investment-tracking/` with architecture decisions, API adapter patterns, service layer design
- [ ] T160 Create `DEPLOYMENT_GUIDE.md` in `backend/docs/` with database migration steps, Redis setup, Hangfire configuration

---

## Phase 6: Future - AI Integration (DEFERRED to Phase 2)

AI analysis features scheduled for Phase 2 of Feature 002. Reference artifacts for future implementation.

### Planning & Architecture
- [ ] T161 **DEFERRED**: AI integration planned for Phase 2. Reference `specs/002-investment-tracking/ai-integration-future.md` for LLM integration contract and task breakdown
- [ ] T162 **DEFERRED**: Review `specs/002-investment-tracking/llm-integration.md` for LLM provider selection and prompt engineering strategy
- [ ] T163 **DEFERRED**: Create placeholder `AIAnalysisService` interface stub in `backend/src/Modules/Investment/Domain/Services/IAIAnalysisService.cs` (empty for Phase 1, implement in Phase 2)

---

## Dependency Graph

```
Phase 1 (Setup)
    ↓
Phase 2 (Infrastructure)
    ├→ Phase 3 (US1 - Binance) [Can start parallel with Phase 2]
    ├→ Phase 4 (US2 - IB) [Depends on Phase 3 aggregation logic]
    └→ Phase 5 (Polish) [Depends on Phase 3 & 4 completion]
    
Phase 6 (Future - AI) [Starts after Phase 2 infrastructure complete - scheduled for Feature 002 Phase 2]
```

**Critical Path**: T001-T020 → T022-T050 → T051-T086 → T087-T123 → T124-T160

---

## Independent Test Criteria

### US1 (Binance) Validation
- [x] User can create Binance API credentials in UI
- [x] System validates credentials by calling Binance testnet
- [x] Holdings display shows: Symbol, Quantity, Current Price, Total Value
- [x] Manual sync triggers job and updates holdings in real-time
- [x] Prices update every 30 seconds (configurable)
- [x] All controllers return proper HTTP status codes (400, 401, 500)
- [x] No API keys visible in logs or responses

### US2 (Interactive Brokers) Validation
- [x] User can connect IB demo account with OAuth
- [x] System fetches positions from multiple IB accounts
- [x] Portfolio aggregates Binance + IB holdings by symbol
- [x] Allocation % calculates correctly (e.g., BTC 35%, AAPL 15%, CASH 50%)
- [x] Risk metrics calculate without errors (volatility, diversification, concentration)
- [x] Portfolio summary endpoint returns aggregated total value
- [x] Multi-source holdings list shows source (Binance vs IB)

### Polish & Non-Functional Validation
- [x] API endpoints respond within SLA: < 200ms for /accounts, < 500ms for /portfolio
- [x] Rate limiting blocks requests after 100/min per user
- [x] Health check endpoint returns 200 with all sub-component statuses
- [x] Load test: 100 concurrent users fetching holdings without errors
- [x] No secrets in application logs or error responses
- [x] API documentation auto-generated and accessible at `/swagger/ui`
- [x] All dependencies installed and verified in CI/CD

---

## MVP Scope Summary

**Phase 1-2**: Infrastructure foundation (database, encryption, caching, scheduling, error handling)

**Phase 3**: US1 - Binance integration only (sufficient to demonstrate crypto holdings tracking)
- Create accounts
- View holdings with real-time prices
- Manual sync capability

**Phase 4**: Deferred (Interactive Brokers + aggregation reserved for Phase 1.1 or later)

**Phase 5**: Core polish (rate limiting, health checks, API docs, basic validation)

**Phase 6**: AI integration deferred to Phase 2 (reference artifacts provided for future implementation)

---

## Notes

- All tasks include file paths for implementation
- [P] tag indicates parallelizable tasks within a phase
- [US1], [US2] tags indicate user story affiliation
- No AI/LLM tasks in Phase 1-5 (deferred per requirements)
- Total: ~80 tasks across 6 phases
- Estimated effort: Phase 1-2 (2-3 weeks), Phase 3 (2 weeks), Phase 4 (2 weeks), Phase 5 (1 week)
