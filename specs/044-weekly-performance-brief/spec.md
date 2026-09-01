# Feature Specification: Weekly Performance Brief

**Feature Branch**: `044-weekly-performance-brief`
**GitHub Issue**: #414
**Created**: 2026-09-01
**Status**: Implementing

## Goal

Deliver a verdict-first weekly Telegram digest that tells Denys at a glance how the book performed versus SPY, whether allocation has drifted, and what (if anything) needs attention — all in ≤12 lines, no noise.

## Destination

M1 — Ledger earns its keep. The weekly brief is the delivery end of the M1 loop: Finance Sentry briefs Denys concisely on what matters and stays silent otherwise.

## Depends on

- **#412 / feature 043** (book performance numbers + portfolio scanner signals) — both already implemented
- Companion event → Telegram dispatch path — already implemented (031)

## User Stories

### [US1] Weekly digest fires and reaches Telegram

**As** Denys  
**I want** a weekly Telegram message every Monday that leads with a verdict ("Outperform / Inline / Underperform vs SPY") and shows the 1W/1M/3M/1Y TWR scoreboard plus the most notable allocation drift when available  
**So that** I know at a glance whether the book is earning its keep without opening the app.

**Acceptance criteria:**
- Hangfire job `book-performance-brief` fires weekly (Monday 08:00 UTC, already registered)
- Message is ≤12 lines; headline is the verdict + delta vs SPY
- At least one accumulated trend line (allocation drift from portfolio scanner signals) is appended when a Notable signal exists within the last 30 days
- Fires via the existing Alert → CompanionEvent → Telegram path (PerformanceBrief type must be mapped in `MaterialityPolicy` and present in `CompanionEventKind`)
- Notification modes respected: Quiet suppresses, Digest holds for batching, Scan/Realtime delivers immediately
- 6-day silence window prevents duplicate delivery on re-runs

### [US2] Cron failure alerting

**As** Denys  
**I want** a Telegram alert if the weekly brief job fails consecutively  
**So that** a silent failure is not mistaken for a silent week.

**Acceptance criteria:**
- Job is already registered in the global `ConsecutiveFailureAlertFilter`; no per-job wiring needed
- The `JobFailure` alert type (already mapped in MaterialityPolicy) delivers via OperationalFailure kind

## Out of scope

- IPS-driven action suggestions (a future Ledger reasoning step, not a Finance Sentry computation)
- Trend charts or sparklines
- Per-ticker breakdown

## Message format

```
Weekly brief: Outperform (+2.5% vs SPY)

1W: Book +3.1% | SPY +0.6% (Δ +2.5%)
1M: Book +6.8% | SPY +4.2% (Δ +2.6%)
3M: Book +12.1% | SPY +9.4% (Δ +2.7%)
1Y: Book +28.4% | SPY +22.1% (Δ +6.3%)

Drift: Equity OverBand (+8.3% vs target)
```

Max 8 lines in the typical case (headline + blank + 4 periods + blank + 1 trend line); up to 12 if all 4 drift sleeves are Notable.
