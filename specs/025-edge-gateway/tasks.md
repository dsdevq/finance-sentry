# Tasks: Edge Gateway

**Feature**: 025-edge-gateway | **Branch**: `025-edge-gateway`
**Input**: plan.md, spec.md, research.md, data-model.md, contracts/gateway-routes.md, quickstart.md

Tech: C# 14 / .NET 10, `Yarp.ReverseProxy` 2.3.0, `LettuceEncrypt` 1.3.2 (config-gated),
ASP.NET Core RateLimiter, OpenTelemetry→Prometheus. New project `FinanceSentry.Gateway`.
Additive to Docker Compose (dev + prod); **no** production TLS cutover, **no** direct-port removal.

**Gates (every code task)**: after any `.cs` change run `dotnet build backend/` → zero warnings.
Tests via `dotnet test`. Commit per logical task (`feat(025): ...` + co-author trailer).

---

## Phase 1: Setup (project scaffolding)

- [x] T001 Create the gateway project `backend/src/FinanceSentry.Gateway/FinanceSentry.Gateway.csproj` (`Microsoft.NET.Sdk.Web`, net10.0 inherited from `Directory.Build.props`) referencing `Yarp.ReverseProxy` and `LettuceEncrypt`; add its `<PackageVersion>` pins (`Yarp.ReverseProxy` 2.3.0, `LettuceEncrypt` 1.3.2) to `backend/Directory.Packages.props`.
- [x] T002 Add `FinanceSentry.Gateway` (and later its test project) to `backend/FinanceSentry.sln` with build configurations, so `dotnet build backend/` compiles it.
- [x] T003 [P] Create the xUnit test project `backend/tests/FinanceSentry.Gateway.Tests/FinanceSentry.Gateway.Tests.csproj` referencing the gateway project + `Microsoft.AspNetCore.Mvc.Testing`; add to the solution.

**Checkpoint**: `dotnet build backend/` succeeds with the empty gateway host.

---

## Phase 2: Foundational (blocking prerequisites for all stories)

- [x] T004 Implement the gateway host skeleton in `backend/src/FinanceSentry.Gateway/Program.cs`: `WebApplication` builder, load `ReverseProxy` config via `AddReverseProxy().LoadFromConfig(Configuration.GetSection("ReverseProxy"))`, `MapReverseProxy()`, and a `/gateway/health` liveness endpoint. Bind Kestrel to `http://+:8080` (dev default).
- [x] T005 Add `UseForwardedHeaders` to the gateway pipeline (ForwardedFor + Proto + Host; clear `KnownNetworks`/`KnownProxies` to trust the single container-network hop) so the rate-limiter partition and backend logs see the real client IP (FR-006).
- [x] T006 Create `backend/src/FinanceSentry.Gateway/appsettings.json` with the declarative `ReverseProxy` Routes + Clusters for dev per contracts/gateway-routes.md: clusters `api`(`http://api:5000`), `mcp`(`http://mcp:5100`), `frontend`(`http://frontend:4200`); routes for `/api/v1/auth/**`, `/api/webhook/**`, `/api/**`, `/hangfire/**`, `/mcp/**` (strip prefix transform), and the `/{**catch-all}` → frontend fallback with correct `Order`.

**Checkpoint**: gateway proxies `/api/...` and `/` to running dev containers (bare routing works).

---

## Phase 3: User Story 1 — One front door (P1) 🎯 MVP

**Goal**: All inbound traffic (SPA, API, MCP) enters through the single gateway and routes to the
right internal service; unknown paths return clean 404s.
**Independent test**: reach SPA + an API endpoint + MCP through `:8080`; unknown path → clean 404.

- [x] T007 [US1] Verify/refine the path-strip transform on the `/mcp/**` route so MCP streamable-HTTP maps at the upstream root (`http://mcp:5100`), preserving WebSocket upgrade + streaming (edge case: MCP transport).
- [x] T008 [US1] Raise Kestrel `Limits.MaxRequestBodySize` (unlimited or large) on the gateway so data-export payloads aren't truncated (edge case), and confirm YARP response streaming (no body buffering).
- [x] T009 [P] [US1] Create `docker/Dockerfile.gateway` — multi-stage `dotnet publish` of `FinanceSentry.Gateway`, linux/arm64-capable, small runtime image, exposes 8080.
- [x] T010 [US1] Add the `gateway` service to `docker/docker-compose.dev.yml` (build from `Dockerfile.gateway`, publish `8080:8080`, `depends_on` frontend+api+mcp, on `finance-sentry-network`, `restart: unless-stopped`). **Keep** existing `4200`/`5001` direct ports (dev parity, FR-008).
- [x] T011 [P] [US1] Add gateway config-binding unit tests in `backend/tests/FinanceSentry.Gateway.Tests/` asserting: every route's `ClusterId` resolves to a defined cluster; exactly one lowest-priority catch-all → frontend; auth+webhook routes carry a rate-limiter policy (data-model validation rules).

**Checkpoint**: US1 independently testable — SPA/API/MCP reachable via `:8080`; unknown path 404s; direct ports still work.

---

## Phase 4: User Story 2 — TLS at the edge (P1)

**Goal**: HTTPS everywhere with an auto-renewing cert; HTTP redirects to HTTPS.
**Independent test**: with TLS enabled, HTTPS serves a valid cert and HTTP redirects; with TLS
disabled (dev/Tailscale), plain HTTP serves normally.

- [x] T012 [US2] Wire `LettuceEncrypt` in `Program.cs` gated by config: when `LettuceEncrypt:DomainNames` is non-empty AND `AcceptTermsOfService=true`, `AddLettuceEncrypt()` + Kestrel HTTPS binding + persist cert cache to a directory (Docker volume); otherwise skip entirely (dev/Tailscale plain HTTP).
- [x] T013 [US2] Apply `UseHttpsRedirection` only when TLS is enabled (FR-003) so dev/HTTP isn't force-redirected; add `LettuceEncrypt` config block (empty defaults) to `appsettings.json` and document the ACME + Tailscale-terminated modes.
- [x] T014 [P] [US2] Add a Docker volume for the ACME cert cache and the (commented, disabled-by-default) 80/443 port mapping + `LettuceEncrypt` env in `docker-compose.prod.yml`'s new gateway service, so enabling TLS is a config flip at cutover — not a code change.

**Checkpoint**: TLS path exists and is toggle-driven; dev keeps working on plain HTTP.

---

## Phase 5: User Story 3 — Rate limiting (P2)

**Goal**: Per-client rate limits on auth + webhook routes; normal traffic unaffected; 429 on abuse.
**Independent test**: exceed the login limit from one client → 429; a normal login is unaffected.

- [x] T015 [US3] Create `backend/src/FinanceSentry.Gateway/GatewayRateLimitPolicies.cs` with named policy constants (`auth`, `webhook`) — no magic strings scattered in `Program.cs`.
- [x] T016 [US3] Register the ASP.NET Core `RateLimiter` in `Program.cs`: fixed-window policies `auth` + `webhook` partitioned by real client IP (post-forwarded-headers), limits bound from `Gateway:RateLimits:*` config, `RejectionStatusCode = 429`; call `UseRateLimiter()` after `UseForwardedHeaders`.
- [x] T017 [US3] Bind the `auth` policy to the `/api/v1/auth/**` route and `webhook` to `/api/webhook/**` via YARP route `Metadata.RateLimiterPolicy` in `appsettings.json`; add the `Gateway:RateLimits` defaults (Auth 10/min, Webhook 60/min).
- [x] T018 [P] [US3] Add rate-limiter tests in `FinanceSentry.Gateway.Tests` asserting policy registration + limit values bind from config, and that auth/webhook routes reference the correct policy names.

**Checkpoint**: US3 independently testable — >limit login requests → 429; well-behaved traffic passes.

---

## Phase 6: User Story 4 — Health-based routing (P3)

**Goal**: Gateway health-checks backends, fails fast (503) when a backend is down, recovers
automatically, and is replica-ready (multi-destination clusters).
**Independent test**: stop the API container → immediate 503 (not a timeout); restart → recovers.

- [x] T019 [US4] Add YARP `HealthCheck` config to clusters in `appsettings.json`: active checks (`api` → `/api/v1/health`, `frontend` → `/`, `ConsecutiveFailures`, ~10s interval / 5s timeout) + passive `TransportFailureRate`; confirm a no-healthy-destination cluster returns a fast 503 (SC-004, US4).
- [x] T020 [US4] Document/verify multi-destination readiness: cluster `Destinations` is a map that accepts N addresses with no code change (FR-005), noted in `appsettings.json` comments and data-model.md.

**Checkpoint**: US4 independently testable — API down → <2s 503; API up → auto-recover.

---

## Phase 7: Backend FR-006 honoring (API change)

- [x] T021 Add `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = XForwardedFor | XForwardedProto | XForwardedHost, KnownNetworks/KnownProxies cleared })` early in `backend/src/FinanceSentry.API/Program.cs` so the API honors the gateway's X-Forwarded-* in Serilog logs + auth decisions (FR-006). Verify build zero-warnings.
- [x] T022 Bump `<Version>` in `backend/src/FinanceSentry.API/FinanceSentry.API.csproj` (MINOR — new middleware behavior) per the constitution versioning gate.

---

## Phase 8: Metrics (FR-007) & Prometheus wiring

- [x] T023 Add OpenTelemetry metrics to the gateway `Program.cs` (ASP.NET Core + runtime instrumentation + YARP meters) with the Prometheus exporter mapped at `/metrics`; ensure rate-limiter rejections are observable.
- [x] T024 [P] Add a `finance-sentry-gateway` scrape job (`targets: ['gateway:8080']`, `metrics_path: /metrics`) to `docker/observability/prometheus/prometheus.yml`.
- [x] T025 [P] Add the `gateway` service to `docker-compose.prod.yml` — additive, build from `Dockerfile.gateway`, on `finance-sentry` network, `restart: unless-stopped`, gateway HTTP bound `127.0.0.1:8080` (behind Tailscale Serve), TLS 80/443 mapping present-but-commented. **Do NOT remove** existing frontend/api/mcp direct ports.

---

## Phase 9: Polish & cross-cutting

- [x] T026 [P] Run `csharp-quality` sweep on the gateway + API changes; `dotnet build backend/` zero warnings; ensure file-scoped namespaces, no unused usings, explicit access modifiers.
- [x] T027 [P] Run the quickstart.md golden-path smoke against the local dev stack (API proxied, SPA served, 503-on-down + recover, login 429) and record results.
- [x] T028 Update `CLAUDE.md` "Current App State" with a 025 edge-gateway summary (additive gateway, dev `:8080`, cutover deferred) and confirm the production-cutover steps in quickstart.md are complete + accurate.

---

## Dependencies & execution order

- **Setup (T001–T003)** → blocks everything.
- **Foundational (T004–T006)** → blocks all user stories.
- **US1 (T007–T011)** = MVP; depends on Foundational. Independently deliverable.
- **US2 (T012–T014)** depends on Foundational; independent of US1 behavior.
- **US3 (T015–T018)** depends on Foundational + T005 (forwarded headers for IP partition).
- **US4 (T019–T020)** depends on Foundational (cluster config exists).
- **Phase 7 (T021–T022)** API change; independent, can land anytime after Setup.
- **Phase 8 (T023–T025)** depends on gateway host (T004) + compose (T010).
- **Phase 9** last.

## Parallel opportunities

- T003 ∥ T001/T002 wrap-up (separate project).
- Within US1: T009 (Dockerfile) ∥ T011 (tests) ∥ core routing tasks.
- T018 (US3 tests) ∥ policy wiring; T024 ∥ T025 (different files); T026/T027 ∥ in polish.

## MVP scope

**US1 alone** (Phase 1→2→3) delivers the single front door — the architectural point of the
feature — and is independently shippable. US2 (TLS), US3 (rate limiting), US4 (health) layer on.
