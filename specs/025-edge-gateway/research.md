# Research: Edge Gateway

Phase 0 output. All decisions below resolve the Technical Context; no NEEDS CLARIFICATION remain.

## D1. Proxy technology — YARP

- **Decision**: `Yarp.ReverseProxy` (Microsoft's YARP), hosted as a standalone ASP.NET Core
  process `FinanceSentry.Gateway` on `net10.0`.
- **Rationale**: Pre-decided (task + spec Notes). C#/ASP.NET fits the in-house "production-practice
  ladder"; YARP gives first-class declarative routing (`ReverseProxy` config section), active +
  passive health checks, per-route rate-limiter policy metadata, and full request/header transform
  control — everything FR-001…FR-007 need — while staying in the team's stack.
- **Alternatives rejected**: nginx / Caddy / Traefik — explicitly out of scope; they'd remove the
  C# practice value that is the entire point of this rung.
- **Version**: `Yarp.ReverseProxy` 2.3.0 (latest stable 2.x; runs on net10.0). Pinned centrally in
  `backend/Directory.Packages.props`.

## D2. Routing model — path-based, single listener

- **Decision**: One gateway listener; route by **path prefix**:

  | Path match | Cluster (upstream) | Transform |
  |---|---|---|
  | `/api/{**catch-all}` | `api` → `api:5000` | none (keep `/api/...` — API is mounted under `/api/v1`) |
  | `/hangfire/{**catch-all}` | `api` → `api:5000` | none |
  | `/metrics` (gateway-local) | — | served by the gateway itself, not proxied |
  | `/mcp/{**catch-all}` | `mcp` → `mcp:5100` | strip `/mcp` prefix (MCP maps at root) |
  | `/{**catch-all}` (fallback) | `frontend` → `frontend:4200` | none (nginx serves SPA + its own `/api` note) |
- **Rationale**: Same-origin path routing keeps the SameSite=Strict refresh cookie valid (all
  traffic shares one origin), mirrors the existing nginx `location /api/` split, and needs no host
  DNS per service. Host-based routing is available in YARP but unnecessary for a single domain.
- **Fallback route (FR / edge case)**: lowest-priority catch-all → frontend. Unknown *API* paths
  return the API's own clean 404; unknown top-level paths hit the SPA (Angular router 404), never
  leaking topology.
- **Note on nginx**: The frontend image still contains its own `location /api/` proxy. Behind the
  gateway the browser talks to the gateway origin, so `/api/*` is caught by the gateway route
  **before** ever reaching nginx. nginx's internal `/api` block is harmless (dead path) and is left
  untouched to preserve the direct-port bypass in dev.

## D3. TLS / certificate strategy — ACME via LettuceEncrypt, config-gated

- **Decision**: Wire `LettuceEncrypt` (Let's Encrypt ACME client for Kestrel). It auto-issues and
  auto-renews certs bound to Kestrel and answers the HTTP-01 challenge on port 80. **Gated by
  config**: active only when `LettuceEncrypt:DomainNames` is non-empty **and**
  `LettuceEncrypt:AcceptTermsOfService=true`. Cert cache persisted to a Docker volume.
- **HTTP→HTTPS redirect** (FR-003, US2): `UseHttpsRedirection` when TLS is enabled.
- **Dev / no-domain**: LettuceEncrypt stays **off**; the gateway listens on plain HTTP
  (`http://+:8080` dev). No self-signed pain for local work; dev parity (FR-008) preserved.
- **Production reality (documented, not blocking)**: the VPS currently terminates TLS via
  **Tailscale Serve** and binds container ports to `127.0.0.1`. Two viable prod modes, chosen at
  cutover time by the operator:
  1. **Tailscale-terminated** — keep Tailscale Serve doing TLS on the tailnet FQDN; point Serve at
     the gateway's HTTP port instead of at frontend:4200. No ACME needed. Zero cert management.
  2. **Public-domain ACME** — register a public A record, publish gateway ports 80/443, enable
     LettuceEncrypt. This is the spec's Assumptions path and requires a real domain (ops
     prerequisite).
  The implementation supports **both**; the choice is a deploy-time config toggle, so the plan is
  not blocked on domain provisioning (per task decision #2).
- **Alternatives rejected**: Certbot sidecar / mounting host certs — pushes cert lifecycle outside
  the C# host and loses the in-process auto-renew practice value.
- **Version**: `LettuceEncrypt` 1.3.2.

## D4. Rate-limiting — ASP.NET Core RateLimiter, per-route YARP policy

- **Decision**: Use the built-in `Microsoft.AspNetCore.RateLimiting` middleware in the gateway
  (same primitive the API already uses). Define **named policies** and attach them to specific YARP
  routes via route `Metadata`/`RateLimiterPolicy`:
  - `auth` policy — fixed window, partitioned **by real client IP**, applied to `/api/v1/auth/*`
    (login, register, refresh, google). e.g. 10 req/min/IP.
  - `webhook` policy — fixed window by client IP, applied to `/api/webhook/*`. e.g. 60 req/min/IP.
  - Other routes: no limit (the API keeps its own in-app authenticated limiter).
- **Partition key**: `HttpContext.Connection.RemoteIpAddress` **after** `UseForwardedHeaders`, so
  the partition is the true client IP (X-Forwarded-For), not a shared upstream address (FR-006).
- **Rejection**: HTTP 429 (SC-005, US3). Throttle events are counted in gateway metrics (FR-007)
  and appear in request logs.
- **Rationale**: One rate-limiting mental model across API + gateway; YARP natively supports
  per-route rate-limiter policy binding, so the "configurable per route" requirement (FR-004) is
  declarative.
- **Alternatives rejected**: Redis-backed distributed limiter — overkill for a single host; revisit
  with the orchestration/replicas feature.

## D5. Health-based routing — YARP active + passive health checks

- **Decision**: Per-cluster `HealthCheck` config:
  - **Active**: `Enabled=true`, `Path=/api/v1/health` (api cluster), `Policy=ConsecutiveFailures`,
    `Interval≈10s`, `Timeout≈5s`. Frontend cluster active-checks `/` (nginx 200). MCP cluster uses
    passive-only (no cheap health path on the MCP HTTP transport) — or active GET `/` tolerating
    the MCP handshake status.
  - **Passive**: `Enabled=true`, `Policy=TransportFailureRate`, reactivation after a cooldown.
  - When a cluster has **no healthy destination**, YARP returns **503 immediately** (no hang) —
    satisfies SC-004 (< 2 s) and US4 fast-fail. Recovery is automatic when the active probe passes.
  - Multiple destinations per cluster are supported now (config lists `destinations`), so the future
    replicas feature just adds addresses — FR-005.
- **Rationale**: YARP's built-in health checks meet FR-005 with declarative config and no custom
  code.
- **Alternatives rejected**: external load balancer / custom polling — reinvents what YARP ships.

## D6. Forwarded headers / real client IP — FR-006

- **Decision**: Gateway sets `X-Forwarded-For` / `X-Forwarded-Proto` / `X-Forwarded-Host` on every
  proxied request (YARP does this by default via `RequestHeaderOriginalHost` + forwarded transform).
  The **API** adds `app.UseForwardedHeaders(...)` early in its pipeline so
  `HttpContext.Connection.RemoteIpAddress` and scheme reflect the real client — honored in Serilog
  request logs and available to auth decisions. `KnownNetworks`/`KnownProxies` cleared for the
  container bridge (trust the single in-network gateway hop).
- **Rationale**: Without this, all backend logs and any IP-based logic would see the gateway's
  bridge IP. This is the one small, necessary change to `FinanceSentry.API` and triggers an API
  version bump.
- **Note**: The gateway itself also runs `UseForwardedHeaders` so its **own** rate-limiter partition
  key is the real client IP when it eventually sits behind Tailscale Serve.

## D7. Metrics — OpenTelemetry → Prometheus at /metrics (FR-007)

- **Decision**: Reuse the API's OpenTelemetry pattern: `AddOpenTelemetry().WithMetrics(...)` with
  ASP.NET Core + runtime instrumentation and the Prometheus exporter mapped at `/metrics` on the
  gateway. Exposes request counts, per-route/cluster proxy latency (YARP emits
  `Yarp.ReverseProxy` meters), and rate-limiter rejection counts. A new Prometheus scrape job
  `finance-sentry-gateway` targets `gateway:8080`.
- **Rationale**: Consistent with the 023 observability stack; Grafana can add a gateway panel later.
- **Alternatives rejected**: bespoke metrics endpoint — inconsistent with existing exporter.

## D8. Edge cases

- **WebSocket / SSE / MCP streamable HTTP**: YARP proxies WebSocket upgrades and streaming
  responses by default; no buffering of the response body. MCP `/mcp` route inherits this.
- **Large payloads (data export)**: Kestrel `MaxRequestBodySize` raised (or set unlimited) on the
  gateway; YARP streams responses so exports aren't truncated.
- **Plaid webhooks**: routed via `/api/webhook/*` with signature headers passed through unmodified;
  rate-limited by the `webhook` policy but with a generous limit so the provider isn't throttled.
- **Gateway as SPOF**: `restart: unless-stopped`, a lightweight image, and a fast startup (no DB, no
  migrations) so it comes up before/faster than the services it fronts. Its own `/health` endpoint
  lets compose/monitoring watch it.

## D9. Dev parity — additive gateway, direct ports kept (FR-008)

- **Decision**: Add a `gateway` service to **both** compose files. In dev it publishes `8080:8080`
  (HTTP) while the existing `4200` (frontend) and `5001` (api) direct ports stay published — dev can
  use the gateway path **or** bypass it. In prod the gateway is added **additively**; existing
  `127.0.0.1:...` direct bindings are **left intact** (guardrail). The destructive cutover is
  deferred to an operator-approved step (see quickstart "Production cutover").
- **Rationale**: Zero-friction local dev + a safe, reversible prod rollout.
