# Quickstart: Edge Gateway

## Run locally (dev)

The gateway is **additive** — the existing direct ports (`4200`, `5001`) stay published, so you can
use either the gateway path or bypass it.

```bash
cd docker
docker compose -f docker-compose.dev.yml up -d --build gateway
# full stack:
docker compose -f docker-compose.dev.yml up -d --build
```

| Entry | URL |
|---|---|
| Gateway (single front door) | http://localhost:8080 |
| — SPA via gateway | http://localhost:8080/ |
| — API via gateway | http://localhost:8080/api/v1/health |
| — MCP via gateway | http://localhost:8080/mcp |
| — Gateway metrics | http://localhost:8080/metrics |
| — Gateway health | http://localhost:8080/gateway/health |
| Direct bypass (unchanged) | http://localhost:4200 · http://localhost:5001/api/v1 |

## Verify the golden paths

```bash
# B2 — API proxied, identical to direct
curl -s http://localhost:8080/api/v1/health         # {"status":"healthy"}
curl -s http://localhost:5001/api/v1/health         # same

# B1 — SPA served through the gateway
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/   # 200

# B12 — real client IP forwarded (check api logs show your IP, not the bridge)
docker logs finance-sentry-api --tail 20 | grep -i "RemoteIp\|X-Forwarded"

# B8/B9 — fast 503 on backend down, auto-recover
docker stop finance-sentry-api
time curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/api/v1/health  # 503 in < 2s
docker start finance-sentry-api
# wait ~10s for the active health probe, then:
curl -s http://localhost:8080/api/v1/health         # healthy again

# B6 — rate limit on login (default 10/min/IP)
for i in $(seq 1 15); do \
  curl -s -o /dev/null -w "%{http_code} " -X POST http://localhost:8080/api/v1/auth/login \
    -H 'Content-Type: application/json' -d '{"email":"x@y.z","password":"nope"}'; \
done; echo    # first ~10 → 400/401, remainder → 429
```

## Configuration toggles

- **TLS off (default dev / Tailscale-terminated prod)**: leave `LettuceEncrypt:DomainNames` empty —
  gateway serves plain HTTP; no HTTPS redirect.
- **TLS on (public-domain ACME)**: set `LettuceEncrypt:DomainNames=["gateway.example.com"]`,
  `LettuceEncrypt:EmailAddress`, `LettuceEncrypt:AcceptTermsOfService=true`, publish ports 80+443,
  point DNS at the host. HTTPS redirect activates automatically.
- **Rate limits**: `Gateway:RateLimits:Auth:PermitPerMinute`,
  `Gateway:RateLimits:Webhook:PermitPerMinute`.
- **Add a replica (future)**: add a second `Destinations` address under a cluster — YARP load-balances
  across healthy destinations, no code change.

## Production cutover — OPERATOR-APPROVED, NOT done by this feature

This implementation ships the gateway **alongside** the existing stack. Making it the sole
entrypoint is a deliberate, reversible, separate step Denys must approve:

1. **Decide TLS mode**: Tailscale-terminated (point `tailscale serve` at the gateway's HTTP port) —
   OR — public domain (register A record, enable LettuceEncrypt, publish 80/443).
2. **Repoint ingress** to the gateway (Tailscale Serve target → `gateway:8080`, or DNS → host 443).
3. **Smoke-test every golden path** through the gateway (login, sync, dashboards, webhooks, MCP).
4. **Close direct ports**: in `docker-compose.prod.yml` remove the host `ports:` publishing on
   `frontend`, `api`, and `mcp` (keep them on the internal `finance-sentry` network only). Leave
   `postgres`/observability loopback bindings as-is unless also fronting them.
5. **Re-register external callback URLs** that used direct ports (e.g. TrueLayer/Plaid
   `PublicApiBaseUrl`, redirect URIs) to the gateway origin.
6. **Deploy + verify** no backend port is reachable from outside the host (SC-001); external port
   scan of the old ports returns closed (US1-3).
7. **Rollback**: re-add the `ports:` blocks and repoint ingress — no data migration involved.
