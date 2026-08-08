# Implementation Plan: Edge Gateway

**Branch**: `025-edge-gateway` | **Date**: 2026-08-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/025-edge-gateway/spec.md`

## Summary

Introduce a single reverse-proxy entrypoint — **YARP** (`Yarp.ReverseProxy`), an ASP.NET Core
host — in front of the frontend (nginx SPA), the API, and the MCP HTTP transport. The gateway
does path-based routing, TLS termination (ACME auto-renewal, config-toggled), per-client rate
limiting on unauthenticated routes, active/passive health-based routing with multi-destination
clusters ready for future replicas, X-Forwarded-* propagation, and Prometheus metrics.

The gateway is delivered as an **additive** service in Docker Compose (dev + prod). Existing
per-service port publishing is **left intact**. The destructive production cutover (making the
gateway the sole published entrypoint, closing direct ports, DNS + ACME issuance) is documented
as a separate operator-approved step and is explicitly out of this implementation's scope.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend host; matches `Directory.Build.props` `net10.0`)
**Primary Dependencies**: `Yarp.ReverseProxy` (reverse proxy), `LettuceEncrypt` (ACME/Let's Encrypt, config-gated), ASP.NET Core built-in `RateLimiter`, OpenTelemetry (metrics → Prometheus exporter) — the same OTel stack the API already uses
**Storage**: None (stateless proxy). ACME certificate cache persisted to a Docker volume when TLS is enabled.
**Testing**: xUnit (`FinanceSentry.Gateway.Tests`) for config binding + route/policy wiring; manual `curl` golden-path smoke via quickstart.md
**Target Platform**: Linux container (linux/arm64 on the VPS, matching other prod images)
**Project Type**: Web service (reverse proxy host) — new project `FinanceSentry.Gateway`, additive to the existing modular monolith solution
**Performance Goals**: < 10 ms p95 added overhead per request (SC-003); backend-down error surfaced in < 2 s (SC-004)
**Constraints**: Must not break dev parity (FR-008); must not remove existing direct ports in prod compose (guardrail); WebSocket/SSE (MCP streamable HTTP, dev HMR) must proxy; large export payloads must not be truncated; real client IP must reach backends
**Scale/Scope**: Single host, single real upstream per cluster today; cluster config supports N destinations for the future orchestration/replicas feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies? | Compliance |
|---|---|---|
| I. Modular Monolith / domain interfaces | Partial | Gateway is infrastructure, not a domain module. It holds **no** business logic and references **no** other module — it only proxies HTTP. No external-integration domain interface applies (it is not a financial integration). |
| II. Code Quality (zero warnings) | Yes | New `.cs` files must build with zero `dotnet build` warnings. Gateway config is declarative JSON where possible to minimize C#. |
| III. Multi-Source Integration | No | Not a data integration. |
| IV. AI Analytics | No | N/A. |
| V. Security-First | Yes | TLS termination at the edge (HTTPS everywhere), HTTP→HTTPS redirect, rate limiting on auth/webhook. Token storage unchanged — JWT validation **stays in the API** (spec OUT OF SCOPE). Cookies/headers pass through unmodified so the SameSite=Strict refresh cookie survives. Secrets (ACME account key) never logged. |
| VI. Frontend State Discipline | No | No frontend code changes. |
| Testing Discipline | Yes | Gateway route/policy config binding covered by unit tests. REST endpoint contract tests do not apply (gateway exposes no new business endpoints — it proxies existing ones); an integration smoke via quickstart validates the proxy path. |
| Versioning | Yes | New API-adjacent host + a one-line `UseForwardedHeaders` addition to `FinanceSentry.API` → bump `FinanceSentry.API.csproj` version. Gateway is a new deployable; no client-facing API contract change. |

**Result**: PASS. No violations requiring Complexity Tracking. The gateway adds a new top-level
project, justified because a reverse proxy is a distinct deployable process, not a module of the
monolith — it is intentionally decoupled ingress infrastructure (the whole point of the feature).

## Project Structure

### Documentation (this feature)

```text
specs/025-edge-gateway/
├── plan.md              # This file
├── research.md          # Phase 0 output — technology decisions
├── data-model.md        # Phase 1 output — Route/Cluster config entities
├── quickstart.md        # Phase 1 output — run + verify golden paths
├── contracts/
│   └── gateway-routes.md # Phase 1 output — routing table + policy contract
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── FinanceSentry.Gateway/          # NEW — YARP reverse-proxy host
│       ├── FinanceSentry.Gateway.csproj
│       ├── Program.cs                  # YARP + rate limiter + forwarded headers + TLS + metrics wiring
│       ├── GatewayRateLimitPolicies.cs # named policy constants (auth, webhook)
│       ├── appsettings.json            # ReverseProxy Routes/Clusters (declarative, dev defaults)
│       └── appsettings.Production.json # prod cluster hostnames + health-check tuning
└── tests/
    └── FinanceSentry.Gateway.Tests/    # NEW — xUnit config-binding + policy wiring tests
        └── FinanceSentry.Gateway.Tests.csproj

backend/src/FinanceSentry.API/Program.cs  # MODIFIED — add UseForwardedHeaders (honor X-Forwarded-* per FR-006)

docker/
├── Dockerfile.gateway                  # NEW — multi-stage build of the gateway host
├── docker-compose.dev.yml              # MODIFIED — add `gateway` service (additive; direct ports kept)
├── docker-compose.prod.yml             # MODIFIED — add `gateway` service (additive; direct ports kept)
└── observability/prometheus/prometheus.yml  # MODIFIED — add gateway scrape job
```

**Structure Decision**: New standalone ASP.NET Core project `FinanceSentry.Gateway` under
`backend/src/`, added to `FinanceSentry.sln`. It is intentionally **not** a `Modules.*` project —
it is ingress infrastructure with no domain coupling. Routing/clusters are declarative JSON in
`appsettings*.json` (versioned in repo per FR-002); only cross-cutting middleware (rate limiter,
forwarded headers, metrics, ACME) is C#.

## Complexity Tracking

> No constitution violations require justification. Table intentionally empty.
