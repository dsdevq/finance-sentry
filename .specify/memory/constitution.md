<!-- SYNC IMPACT REPORT - Constitution 1.1.1
Version: 1.1.0 → 1.1.1
Bump Type: PATCH (Clarifications to Principles I and Testing Discipline; no new principles)
Principles Modified:
  - I. Modular Monolith Architecture: Added explicit adapter interface mandate
    (external integrations MUST expose a domain-defined interface, not be consumed as concretes)
  - Testing Discipline: Clarified "Contract Tests" to explicitly cover REST API endpoint
    contracts in addition to external API integration contracts
Sections Added: None
Sections Removed: None
Templates Updated:
  ✅ .specify/templates/spec-template.md — Added ## Notes section for integration decisions
  ✅ .specify/templates/tasks-template.md — Corrected "OPTIONAL" label on contract tests
     to reflect that external API contract tests are mandatory per constitution
  ✅ .specify/templates/plan-template.md — No changes required (generic template unaffected)
  ✅ .specify/templates/agent-file-template.md — No changes required
Follow-up TODOs:
  - specs/001-bank-account-sync/spec.md: Resolve [NEEDS CLARIFICATION] webhook note
    (decision was made in tasks.md; spec must be updated to record the decision formally)
  - specs/001-bank-account-sync/plan.md: Update Constitution Check table to reference v1.1.1
    and add Branching Strategy / Versioning compliance row
  - specs/001-bank-account-sync/tasks.md: Add IBankProvider interface task (C2 from analyze)
    and Phase 4/5 contract test tasks (C3 from analyze)
-->

# Finance Sentry Constitution

## Core Principles

### I. Modular Monolith Architecture

All backend services MUST be organized as a modular monolith using ASP.NET 9+ with
domain-driven design. Each financial integration (banks, brokers, crypto) is a distinct,
self-contained module with clear contracts. Modules communicate through well-defined
service boundaries, never coupling directly to external APIs.

All external service integrations (bank APIs, broker APIs, crypto exchanges) MUST be
accessed via a domain-defined interface (e.g., `IBankProvider`, `IBrokerAdapter`).
No module may reference a concrete external adapter directly — concrete implementations
are registered in Infrastructure and resolved via dependency injection. This ensures
each integration is independently swappable, mockable in tests, and replaceable without
changing module business logic.

Modules are independently deployable and testable.

### II. Code Quality Enforcement (NON-NEGOTIABLE)

Strict code quality is non-negotiable. Backend: ESLint + Prettier (absolute enforcement),
StyleCop + code analyzers, zero-warning builds. Frontend: Angular linting (strict mode),
tsconfig strict: true, CSS linting. All commits fail pre-commit hooks if standards
violated. No exceptions in pull requests—violations block merge until resolved.

### III. Multi-Source Financial Integration

The system MUST reliably aggregate data from multiple financial sources: bank APIs,
Interactive Brokers, Binance, and others. Each integration module MUST have isolated
data synchronization, error handling, and retry logic. Data consistency across sources
is verified through reconciliation tasks. APIs are treated as potentially unreliable—
failures are graceful.

### IV. AI-Driven Analytics & Insights

AI analysis is the core value proposition. Portfolio analysis, risk assessment, and
forecasts must be AI-backed. LLM integration (via documented API patterns) generates
asset reports and portfolio-level insights. Analytical results are cached and versioned.
Both individual asset and portfolio-level analytics require AI summarization and
recommendations.

### V. Security-First Financial Data Handling

All financial data is encrypted at rest and in transit. Authentication is enforced at
API boundary and per-module. User data isolation is absolute—queries, caching, and
reports must be user-scoped. Secrets are never logged. Audit logs record all data
access. No shortcuts on security—violations require explicit team lead approval.

## Tech Stack Minimums

**Backend**: .NET Core 9+, ASP.NET with OpenAPI/Swagger documentation
**Frontend**: Angular 20+, TypeScript with strict mode, RxJS for reactive patterns
**Database**: PostgreSQL 14+
**Message Queue/Async**: RabbitMQ or built-in hosted service (if monolith only)
**Containerization**: Docker for all services; Docker Compose for local development
**AI/LLM**: OpenAI API or compatible; documented prompts and request patterns
**Testing**: xUnit/.NET test framework for backend, Jasmine/Karma for frontend
**Monitoring**: ELK (Elasticsearch, Logstash, Kibana) or Application Insights for
structured logging

*Non-negotiable versions*: .NET 9+, Angular 20+, PostgreSQL 14+. Downgrades require
team lead approval.

## Development Workflow & Quality Gates

### Branching Strategy

All work MUST follow per-task feature branching discipline:

- **Branch Naming**: `<feature-id>-<description>` (e.g., `001-bank-account-sync`,
  `T211-bank-account-tests`)
- **Isolation**: Each task/story gets a dedicated branch from the current `main` or
  feature branch parent
- **Per-Task Branching**: Do NOT commit multiple independent tasks to a single branch.
  Each logical task/story must have its own branch for independent review and potential
  rollback
- **Lifecycle**:
  1. Create branch from `main` (or current feature parent if sub-task)
  2. Implement task, pass all tests, update versions if required (see Versioning &
     Tagging Policy)
  3. Open PR to `main` (or parent feature branch)
  4. Pass CI/CD (linting, tests, coverage)
  5. Code review approval (MUST verify compliance with Principles I–V)
  6. Merge to `main`
  7. **Delete branch immediately after merge** (enforce via GitHub setting:
     "Automatically delete head branches")
  8. **Create new branch from updated `main`** if continuing work on next task

### Code Review & Compliance

Every PR MUST verify compliance with Core Principles I–V. Violations block merge:

- Failing linter checks → automatic block
- Missing or incomplete tests → automatic block
- Non-encrypted data handling → automatic block
- Coupling between modules → automatic block
- External integration accessed without a domain interface (Principle I) → automatic block
- Version NOT bumped on frontend/API changes → automatic block (see Versioning & Tagging
  Policy)
- Tag NOT created for version bump → automatic block (see Versioning & Tagging Policy)

Code review checklist includes security, testability, adherence to modular boundaries,
and version/tagging compliance.

### Testing Discipline

- **Unit Tests**: Required for all business logic (>80% coverage target)
- **Integration Tests**: Required for inter-module contracts and API boundaries
- **Contract Tests**: Two mandatory categories:
  1. **External API contracts**: Required for every integration with an external service
     (banks via Plaid, Interactive Brokers, Binance, etc.). Tests validate that the
     external API still conforms to the shape the domain interface expects.
  2. **REST endpoint contracts**: Required for every REST endpoint exposed by the
     backend. Tests validate request/response schema and status codes independently
     of business logic. Each new endpoint MUST ship with a corresponding contract test
     in the same PR.
- **Test-First**: Tests written and passing BEFORE feature implementation (TDD)
- All tests must run in CI/CD pipeline; red pipeline blocks merge

### Versioning & Tagging Policy

#### Frontend (Angular SPA) Versioning

- **Location**: `frontend/package.json` - `"version"` field
- **Trigger**: Any change to Angular components, services, models, styling, or routing
- **Semver Scheme**: MAJOR.MINOR.PATCH
  - **MAJOR**: Breaking UI changes, major API contract change (requires backend
    coordination)
  - **MINOR**: New component/feature, new service method, non-breaking changes
  - **PATCH**: Bug fixes, dependency updates, style refinements
- **Requirement**: Version bump MUST be committed in the same PR as the feature/fix
- **CI/CD Check**: Pipeline validates that version in `package.json` matches the commit
  (MUST increment if any .ts/.html/.scss changed under `frontend/src/`)

#### API (Backend) Versioning

- **Location**: `FinanceSentry.API.csproj` - `<PropertyGroup><Version>` field, and
  OpenAPI/Swagger documentation
- **Trigger**: Any change to REST API contract (new endpoint, parameter addition,
  response schema change, deprecation)
- **Semver Scheme**: MAJOR.MINOR.PATCH
  - **MAJOR**: Breaking endpoint changes (e.g., parameter removal, response structure
    incompatible with clients)
  - **MINOR**: New endpoints, new optional parameters, new response fields
  - **PATCH**: Bug fixes, security updates, endpoint improvements (no client impact)
- **Requirement**: Version bump MUST be committed in the same PR as the API change
- **CI/CD Check**: Pipeline validates version increment on PR
- **OpenAPI/Swagger Update**: API documentation in `docs/SWAGGER.md` (or
  auto-generated) MUST reflect version

#### GitHub Tags

- **Naming Convention**: `v<MAJOR>.<MINOR>.<PATCH>` (e.g., `v0.2.0`, `v1.0.0-beta`)
- **Scope**: Tags MUST be created for **both** frontend and backend version bumps
  - If only frontend changes: tag as `frontend-v<VERSION>` (e.g., `frontend-v0.2.0`)
  - If only backend changes: tag as `backend-v<VERSION>` (e.g., `backend-v0.1.0`)
  - If both change in same PR: **coordination required**—create separate tags or single
    tag if coordinated release
- **Timing**: Tag MUST be created after merge to `main` (via GitHub release automation
  or manual `git tag` + push)
- **Release Notes**: Each tag MUST have release notes documenting changes (generated
  from PR description + linked issues)
- **Automation**: CI/CD SHOULD automatically create tag on merge if version detected
  (recommended: GitHub Actions workflow `on: push to main`)

#### Combined Example Workflow

```
1. Developer creates branch: git checkout -b T206-plaid-link-endpoint
2. Implements POST /accounts/link REST endpoint
3. Adds contract test for POST /accounts/link in same PR (mandatory per Testing
   Discipline)
4. Bumps backend version in FinanceSentry.API.csproj: 0.1.0 → 0.1.1
5. Commits: "feat: implement POST /accounts/link endpoint (closes T206)"
6. Pushes branch, opens PR
7. CI/CD:
   - ✅ Builds backend
   - ✅ Runs tests (coverage >80%)
   - ✅ Linting passes
   - ✅ Contract test for POST /accounts/link passes
   - ✅ Validates version bump: 0.1.0 → 0.1.1 detected in .csproj ✓
8. Code review approves
9. Merge to `main`
10. GitHub Actions detects version bump → creates tag `backend-v0.1.1`
11. Release notes auto-populated from PR
12. Branch deleted (automatic)
13. Developer creates new branch for next task: git checkout -b T207-get-accounts
```

### Deployment Process

1. Feature branch → Pull Request (CI: build + lint + tests + version/tag validation)
2. Code review approval (MUST verify compliance + version correctness)
3. Merge to `main`
4. **Automatic tag creation** (if version detected) or manual tag if required
5. Automated deployment to staging (via Docker)
6. Manual verification in staging
7. Production deployment (manual gate—team lead approval)

Rollback procedure documented and tested monthly. **Rollback includes tag management**:
if reverting commits, delete associated version tags and recreate if necessary.

## Issue Tracker Sync

All features developed via the speckit toolchain MUST be mirrored to Jira before implementation begins:

- **Trigger**: After `speckit.tasks` completes and `tasks.md` is finalized, every task in the file MUST have a corresponding Jira issue in the `SCRUM` project on `denyssychovdev.atlassian.net`.
- **Issue type mapping**: speckit Phases → Epics; individual tasks → Stories or Tasks depending on scope.
- **Sync method**: Use the `speckit.taskstoissues` skill (Jira variant via Atlassian MCP) or manually via MCP tools if the skill is not yet wired to Jira.
- **Blocking**: Implementation (`speckit.implement`) MUST NOT start until all tasks have Jira issues created.
- **Updates**: If tasks.md changes after sync (e.g., follow-up tasks added), Jira must be updated in the same session before the next implement run.

## Governance

The constitution supersedes all other development practices. Amendments require
documentation of rationale, impact on current tasks, and approval by team lead (Denys).

**Compliance Enforcement**: All PRs are subject to automated compliance checks (linting,
tests, security, version bumps, tagging) and manual verification per architecture.
Violations require explicit remediation or team lead approval.

**Principles Trump Convenience**: When speed conflicts with a principle, the principle
wins. Exceptions are documented and tracked.

**Branch Discipline**: Per-task branching is MANDATORY. Mixed multi-task branches
require explicit team lead approval (rare exception for coordinated refactors).

**Version & Tagging Enforcement**: Automatic CI/CD checks MUST validate version bumps
and tag creation. Missing version bump or tag blocks PR merge.

**Version Policy**:
- MAJOR bump for principle additions/removals or backward-incompatible API changes
- MINOR bump for new principles/sections or new API endpoints
- PATCH bump for clarifications, typos, or API bug fixes
- Each version change increments **Last Amended** date (ISO format)
- Applies to both constitution versioning and feature versioning (frontend/backend)

**Version**: 1.2.0 | **Ratified**: 2026-03-21 | **Last Amended**: 2026-04-11
