# External API Contract: FRED `series/observations`

**Base**: `https://api.stlouisfed.org/fred/`
**Endpoint**: `GET series/observations`
**Auth**: `api_key` query parameter (free key from fred.stlouisfed.org). Keyless ⇒ source is silent (no request).

## Request

```
GET series/observations?series_id=DGS10&api_key=<KEY>&file_type=json&sort_order=desc&limit=8
GET series/observations?series_id=DGS2&api_key=<KEY>&file_type=json&sort_order=desc&limit=8
```

- `sort_order=desc` + `limit=8` → the most-recent 8 observations, so a long-weekend `.` tail still yields a valid latest value.
- `file_type=json` → the JSON contract below (FRED defaults to XML otherwise).

## Response (documented shape)

```json
{
  "realtime_start": "2026-08-08",
  "realtime_end": "2026-08-08",
  "observation_start": "1600-01-01",
  "observation_end": "9999-12-31",
  "units": "lin",
  "count": 15234,
  "offset": 0,
  "limit": 8,
  "observations": [
    { "realtime_start": "2026-08-08", "realtime_end": "2026-08-08", "date": "2026-08-08", "value": "3.71" },
    { "realtime_start": "2026-08-08", "realtime_end": "2026-08-08", "date": "2026-08-07", "value": "3.69" },
    { "realtime_start": "2026-08-08", "realtime_end": "2026-08-08", "date": "2026-08-06", "value": "." }
  ]
}
```

## Parse rules (asserted by `FredYieldCurveSourceTests`)

- Root must contain an `observations` array — otherwise `throw` (contract drift / challenge page), consistent with the Finnhub loud-on-broken-body precedent.
- Each observation: read `date` (yyyy-MM-dd) and `value` (string).
- **Skip `value == "."`** (FRED's no-observation placeholder) and any non-numeric value.
- The **latest valid** observation (first numeric row given `desc`) is the series' current value.
- `spread = DGS10.latest − DGS2.latest` (percentage points). If either series has no valid observation, the rates axis is unavailable for that run.

## Failure behaviour

- HTTP non-2xx or network error → rates axis unavailable that run (logged at debug/warning); the volatility axis and the run continue.
- 400/403 (bad/blocked key) → treated as unavailable + a warning (invalid key is an operator concern, but must not crash the daily job).
- Keyless (`ApiKey` blank) → no request issued at all (`IsConfigured == false`).
