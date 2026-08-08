# Data Model: Edge Gateway

The gateway is stateless — it has **no database entities**. Its "data model" is the declarative
YARP configuration bound from `appsettings*.json` (`ReverseProxy` section) plus the rate-limiter
policy set. These are the Key Entities named in the spec.

## Entity: Route

A match rule mapping an inbound request to a cluster, with optional per-route policies.

| Field | Type | Notes |
|---|---|---|
| `RouteId` | string | Unique id (`api-route`, `hangfire-route`, `mcp-route`, `frontend-route`). |
| `ClusterId` | string | Target cluster (`api`, `mcp`, `frontend`). |
| `Match.Path` | string | Path pattern, e.g. `/api/{**catch-all}`. |
| `Match.Hosts` | string[] | Optional host match (unused for single-domain path routing). |
| `Order` | int | Lower = higher priority. Fallback `/{**catch-all}` has the highest number. |
| `Metadata.RateLimiterPolicy` | string? | Named policy (`auth`, `webhook`) or absent (no limit). |
| `Transforms` | list | e.g. path prefix strip for `/mcp`; forwarded-header transforms. |

**Validation / rules**
- Every `ClusterId` referenced by a route MUST exist in Clusters.
- Exactly one lowest-priority catch-all route (fallback → frontend) MUST exist.
- Auth + webhook routes MUST carry a `RateLimiterPolicy` (FR-004).

## Entity: Cluster (Upstream)

A named backend with one or more destination addresses and a health-check definition.

| Field | Type | Notes |
|---|---|---|
| `ClusterId` | string | `api`, `mcp`, `frontend`. |
| `Destinations` | map<string, {Address}> | ≥1 address. Single today; N-ready for replicas (FR-005). |
| `HealthCheck.Active.Enabled` | bool | `true` for api/frontend. |
| `HealthCheck.Active.Path` | string | e.g. `/api/v1/health`. |
| `HealthCheck.Active.Policy` | string | `ConsecutiveFailures`. |
| `HealthCheck.Active.Interval` | duration | ~`00:00:10`. |
| `HealthCheck.Active.Timeout` | duration | ~`00:00:05`. |
| `HealthCheck.Passive.Enabled` | bool | `true` — `TransportFailureRate`. |

**Validation / rules**
- Each cluster MUST have ≥1 destination.
- No healthy destination ⇒ gateway returns 503 fast (FR-005 / SC-004).
- Destination `Address` values are internal service names on the compose network
  (`http://api:5000`, `http://mcp:5100`, `http://frontend:4200`) — never host-published ports.

## Entity: RateLimiterPolicy (gateway-owned)

Named ASP.NET Core rate-limiter policies referenced by route metadata.

| Policy | Algorithm | Partition | Limit (default) | Applied to |
|---|---|---|---|---|
| `auth` | Fixed window | real client IP | 10 / min | `/api/v1/auth/*` |
| `webhook` | Fixed window | real client IP | 60 / min | `/api/webhook/*` |

**Rules**
- Partition key = client IP resolved **after** `UseForwardedHeaders` (FR-006).
- Rejection status = 429 (SC-005). Rejections are counted in gateway metrics (FR-007).
- Limits are configurable via `appsettings` (`Gateway:RateLimits:*`) so per-route tuning stays
  declarative (FR-004).

## Configuration surface (bound at startup)

```
ReverseProxy:
  Routes:    { <RouteId>: { ClusterId, Match{Path}, Order, Metadata{RateLimiterPolicy}, Transforms } }
  Clusters:  { <ClusterId>: { Destinations{ d1{Address} }, HealthCheck{Active, Passive} } }
Gateway:
  RateLimits: { Auth{PermitPerMinute}, Webhook{PermitPerMinute} }
LettuceEncrypt:
  DomainNames: []            # empty ⇒ TLS/ACME disabled (dev + Tailscale-terminated prod)
  EmailAddress: ""
  AcceptTermsOfService: false
```
