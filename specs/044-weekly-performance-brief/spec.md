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

### [US4] Track-record delta and a single policy-judged action

**As** Denys
**I want** the brief to also tell me whether my own calls are beating SPY, and — when the book is
outside policy — the one thing worth doing about it
**So that** the digest closes the loop from "how did the book do" to "what should I do".

Issue #414 names both ("track-record delta, at most one suggested action, judged against the IPS").
The original spec deferred them; this story delivers them.

**Acceptance criteria:**
- One track-record line reports hit rate + average excess return vs SPY over the thesis book,
  never blending terminal and active records (feature 020 R4), and carries the low-sample caveat
- At most **one** action line, derived from the Notable portfolio-scanner signals that already
  encode the IPS/risk boundary, in priority order: allocation drift (IPS bands) → cash-buffer
  floor → single-position cap. No breach ⇒ no action line (stay silent)
- The whole message (headline + body) stays ≤12 lines with both new lines present
- Radar does not reference Research: the track record arrives through a cross-module port with its
  adapter in `FinanceSentry.Integration` (the 039/043 precedent)

### [US5] The action line fires only on a real breach, and a dead brief is loud

**As** Denys
**I want** the suggested action to reflect an actual policy breach, and a week where no brief could
be produced at all to reach me as a failure alert
**So that** the digest keeps its "stay silent unless it matters" promise in both directions.

**Acceptance criteria:**
- Risk-rule limits reach the portfolio scanner in the unit `PortfolioScanData` declares
  (percentage points), so the cash floor and single-position cap fire on breaches and only on
  breaches — satisfying US4's "No breach ⇒ no action line"
- A run in which every active user's brief failed leaves Hangfire in a failed state so
  `ConsecutiveFailureAlertFilter` can accumulate the streak (US2); a run where only some users
  failed still delivers the rest

## Out of scope

- Trend charts or sparklines
- Per-ticker breakdown
- Free-form LLM-written commentary (the action line is a deterministic computation, not reasoning)

## Message format

```
Weekly brief: Outperform (+2.5% vs SPY)

1W: Book +3.1% | SPY +0.6% (Δ +2.5%)
1M: Book +6.8% | SPY +4.2% (Δ +2.6%)
3M: Book +12.1% | SPY +9.4% (Δ +2.7%)
1Y: Book +28.4% | SPY +22.1% (Δ +6.3%)

Calls: 58% of 12 closed beat SPY, avg Δ +3.2% (low sample)
Drift: Equity OverBand (+8.3pp vs target)

Action: Trim Equity by ~8.3pp (~$12.4k) to its 60% IPS target.
```

Budget: the delivered message (headline + body) is ≤12 lines. The scoreboard and the action line
are reserved first; drift trend lines fill whatever is left, up to 4.
