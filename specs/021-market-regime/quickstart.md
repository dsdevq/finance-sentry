# Quickstart: Market Regime Scanner (021)

Backend-only feature, inside the Radar module. No frontend.

## Configure

`FRED_API_KEY` (free, from https://fred.stlouisfed.org/docs/api/api_key.html) → `docker/.env.sops` and the compose env, mapped to `Regime__Fred__ApiKey`:

```yaml
# docker/docker-compose.dev.yml (api service env)
Regime__Fred__ApiKey: ${FRED_API_KEY:-}
```

- **Keyless** (blank): the rates axis is silently unavailable; the volatility axis (Yahoo `^VIX`) still works out of the box.
- Thresholds are all overridable under the `Regime` config section (see `data-model.md`), e.g. `Regime__VixStressedMax=28`.

## Run the daily job

Registered as Hangfire recurring job `regime-compute` (default 23:00 UTC, after Radar's compute). To trigger manually in a running stack, use the Hangfire dashboard (`/hangfire`) or wait for the schedule.

## Verify

1. **Build/test** (host lacks .NET 10 — use the sdk container):
   ```bash
   docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
     dotnet build backend/FinanceSentry.sln -c Debug
   docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
     dotnet test backend/tests/FinanceSentry.Modules.Radar.Tests
   docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
     dotnet test backend/tests/FinanceSentry.Modules.Research.Tests
   ```
2. **Migration applied**: after a stack up, confirm `regime_readings` exists in schema `radar` and the row appears in `__ef_migrations_history_radar`.
3. **MCP tool**: call `get_market_regime()` — expect both axes with raw readings; rates `available:false` if keyless.
4. **Signals**: query `radar_signals` for scanner `market_regime` — daily `info` per axis; a `regime_change` notable only on a band cross.
5. **019 coupling**: score a candidate (`score_candidate`) — the returned scorecard carries a `regime` context with raw vs adjusted structure score and a rationale; the persisted `StructureScore` is the raw value.

## Key invariants to eyeball

- Two axes, never one combined label.
- Unavailable axis ⇒ `available:false` + null band (never a fabricated regime).
- Regime never raises cash / sells / blocks a promotion — it only reranks scoring.
