<!-- SYNC IMPACT REPORT - Constitution 1.0.0
Version: 1.0.0 (NEW)
Bump Type: MAJOR (Initial ratification)
Principles Added: I. Modular Monolith Architecture, II. Code Quality Enforcement, III. Multi-Source Financial Integration, IV. AI-Driven Analytics & Insights, V. Security-First Financial Data Handling
Sections Added: Tech Stack Minimums, Development Workflow & Quality Gates
Status: ✅ Templates aligned - plan.md, spec.md, tasks.md reviewed and aligned with modular + quality-first discipline
Deferred: None
-->

# Finance Sentry Constitution

## Core Principles

### I. Modular Monolith Architecture

All backend services MUST be organized as a modular monolith using ASP.NET 9+ with domain-driven design. Each financial integration (banks, brokers, crypto) is a distinct, self-contained module with clear contracts. Modules communicate through well-defined service boundaries, never coupling directly to external APIs. Modules are independently deployable and testable.

### II. Code Quality Enforcement (NON-NEGOTIABLE)

Strict code quality is non-negotiable. Backend: ESLint + Prettier (absolute enforcement), StyleCop + code analyzers, zero-warning builds. Frontend: Angular linting (strict mode), tsconfig strict: true, CSS linting. All commits fail pre-commit hooks if standards violated. No exceptions in pull requests—violations block merge until resolved.

### III. Multi-Source Financial Integration

The system MUST reliably aggregate data from multiple financial sources: bank APIs, Interactive Brokers, Binance, and others. Each integration module MUST have isolated data synchronization, error handling, and retry logic. Data consistency across sources is verified through reconciliation tasks. APIs are treated as potentially unreliable—failures are graceful.

### IV. AI-Driven Analytics & Insights

AI analysis is the core value proposition. Portfolio analysis, risk assessment, and forecasts must be AI-backed. LLM integration (via documented API patterns) generates asset reports and portfolio-level insights. Analytical results are cached and versioned. Both individual asset and portfolio-level analytics require AI summarization and recommendations.

### V. Security-First Financial Data Handling

All financial data is encrypted at rest and in transit. Authentication is enforced at API boundary and per-module. User data isolation is absolute—queries, caching, and reports must be user-scoped. Secrets are never logged. Audit logs record all data access. No shortcuts on security—violations require explicit team lead approval.

## Tech Stack Minimums

**Backend**: .NET Core 9+, ASP.NET with OpenAPI/Swagger documentation  
**Frontend**: Angular 20+, TypeScript with strict mode, RxJS for reactive patterns  
**Database**: PostgreSQL 14+  
**Message Queue/Async**: RabbitMQ or built-in hosted service (if monolith only)  
**Containerization**: Docker for all services; Docker Compose for local development  
**AI/LLM**: OpenAI API or compatible; documented prompts and request patterns  
**Testing**: xUnit/.NET test framework for backend, Jasmine/Karma for frontend  
**Monitoring**: ELK (Elasticsearch, Logstash, Kibana) or Application Insights for structured logging

*Non-negotiable versions*: .NET 9+, Angular 20+, PostgreSQL 14+. Downgrades require team lead approval.

## Development Workflow & Quality Gates

### Code Review & Compliance

Every PR MUST verify compliance with Core Principles I–V. Violations block merge:
- Failing linter checks → automatic block
- Missing or incomplete tests → automatic block
- Non-encrypted data handling → automatic block
- Coupling between modules → automatic block

Code review checklist includes security, testability, and adherence to modular boundaries.

### Testing Discipline

- **Unit Tests**: Required for all business logic (>80% coverage target)
- **Integration Tests**: Required for inter-module contracts and API boundaries
- **Contract Tests**: Required for external API integrations (banks, brokers, Binance)
- Test-First: Tests written and passing BEFORE feature implementation (TDD)
- All tests must run in CI/CD pipeline; red pipeline blocks merge

### Deployment Process

1. Feature branch → Pull Request (CI: build + lint + tests)
2. Code review approval (MUST verify compliance)
3. Merge to `main`
4. Automated deployment to staging (via Docker)
5. Manual verification in staging
6. Production deployment (manual gate—team lead approval)

Rollback procedure documented and tested monthly.

## Governance

The constitution supersedes all other development practices. Amendments require documentation of rationale, impact on current tasks, and approval by team lead (Denys).

**Compliance Enforcement**: All PRs are subject to automated compliance checks (linting, tests, security) and manual verification per architecture. Violations require explicit remediation or team lead approval. 

**Principles Trump Convenience**: When speed conflicts with a principle, the principle wins. Exceptions are documented and tracked.

**Version Policy**: MAJOR bump for principle additions/removals, MINOR for new sections or guidance expansions, PATCH for clarifications or typos. Each version increments last amended date.

**Version**: 1.0.0 | **Ratified**: 2026-03-21 | **Last Amended**: 2026-03-21
