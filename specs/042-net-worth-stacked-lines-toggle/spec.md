# Spec 042 — Net Worth Over Time: Stacked/Lines Toggle

**Source issue**: #406  
**Destination**: Dashboard IA — Net Worth Over Time card

## Problem

The Net Worth Over Time chart is stacked, so each visible line is a running sum (purple top = banking + brokerage, orange top = total). This is correct for composition view but makes individual sleeve trends unreadable — a bottom-band move (e.g., salary landing in banking) visually shifts every line above it, which has repeatedly read as "all sleeves move together".

## Solution

A small **Stacked / Lines** toggle on the chart card.

- **Stacked** (default): current view — composition bands plus total.
- **Lines**: each sleeve plotted from zero (`stacked: false`, no fill), so crypto/brokerage/banking trends are independently readable.

## Acceptance Criteria

- The Net Worth Over Time card carries a Stacked / Lines toggle.
- Stacked is the default and renders today's view unchanged — composition bands plus total.
- Lines plots each sleeve from zero (`stacked: false`, no fill), so a move in a bottom band no longer visually displaces the sleeves above it and per-sleeve trends read independently.
- `cmn-area-chart` gains a `stacked` input defaulting to `true`, so every existing call site renders exactly as before without change.
- Toggling re-renders the series already loaded; it does not refetch.
