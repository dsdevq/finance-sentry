# Feature Specification: Net Worth Projection from Savings Contributions

**Feature Branch**: `045-net-worth-projection`

**Created**: 2026-09-02

**Status**: Implemented

**GitHub Issue**: #539

## Context

Denys asked for a net-worth projection off the last three months' trend. Projecting the
net-worth *line* would forecast the market, not behaviour: 91% of net worth is market-marked
(brokerage $9.8k + crypto $4.7k of $15.9k on 2026-08-31), daily swings run ±$500 against
~$1k/mo of savings, and snapshots only start 2026-06-30. A trend fitted to that line would be
driven almost entirely by whatever the market did inside the sample window, then presented as
a savings forecast.

So the projection is built from **contributions** — the money the user actually moves — with
market return held separate and explicitly labelled as an assumption the user selects.

**Scope decided 2026-08-31**: fixed 12-month horizon, no persisted goal. "When do I hit $X" is
deliberately out of scope — it needs a goal entity that does not exist anywhere in the backend
today.

---

## User Scenarios

### [US1] Twelve-month projection tile with a market-return assumption (P1)

The dashboard carries a tile that projects net worth 12 months forward from the **median**
monthly net savings (inflow − outflow, USD) over the trailing complete months, plus a
market-return toggle that defaults to 0%.

Median, not mean: n is small and June 2026 carries the duplicate-salary artifact from the #400
audit, which drags a mean but not a median.

**Acceptance Scenarios**:

1. **Given** 3+ complete months of cash flow, **When** the dashboard loads, **Then** the tile
   shows `current net worth + median monthly net savings × 12` and the sample size ("based on
   N complete months").
2. **Given** fewer than 3 complete months, **When** the dashboard loads, **Then** the tile does
   not render at all — a projection off one or two months is noise wearing a number's clothes.
3. **Given** complete months of −200, +1000 and +5000 (a duplicate-salary month), **When** the
   projection is computed, **Then** the baseline is +1000 (the median), not +1933 (the mean).
4. **Given** the market-return toggle is at its 0% default, **When** the projection is computed,
   **Then** no sleeve compounds and the figure is contributions only.
5. **Given** the user selects a non-zero return, **When** the projection is computed, **Then**
   only `brokerageTotal + cryptoTotal` of the latest snapshot compounds at that annual rate;
   banking cash and the projected contributions do not.
6. **Given** any selected rate, **When** the tile renders, **Then** it states the assumption in
   words next to the number.
7. **Given** the in-progress month, **When** the projection is computed, **Then** it is excluded
   — the baseline is the existing `completeMonths()` window.

---

### [US2] The projection shows its parts, not just its total (P1)

US1 shipped a single blended figure plus a prose sentence naming the assumption. But the
premise of the whole feature is that contributions are *behaviour* and market return is a
*guess*, and a blended total hides exactly the distinction the feature exists to draw — the
US1 code says so in as many words ("the whole point of splitting return out of the projection
is that the reader can see which part is their own behaviour and which part is a guess about
the market"), and then renders one number.

Concretely: at 7% on a $10k market sleeve the headline moves $700 with no visible attribution,
so the reader cannot tell a good savings month from a generous assumption. The tile therefore
breaks the projection into its addends — today's net worth, projected contributions, assumed
market return — which also makes the 0% default *visibly* flat (`$0`) rather than merely
described as flat.

**Acceptance Scenarios**:

1. **Given** the tile renders, **When** the reader looks at it, **Then** the projection is shown
   as three addends — today's net worth, contributions over the horizon, assumed market return
   — that sum to the headline figure.
2. **Given** the 0% default, **When** the tile renders, **Then** the market-return addend reads
   `$0` and the entire difference from today is the contributions line.
3. **Given** a non-zero rate, **When** the tile renders, **Then** only the market-return addend
   changes; today's net worth and the contributions line are untouched by the selection.
4. **Given** a negative median month, **When** the tile renders, **Then** the contributions
   addend carries an explicit minus sign — an addend column that sums to a total must not
   render a withdrawal as if it were a credit.

---

## Functional Requirements

- **FR-001**: Baseline is the **median** of per-month `inflowUsd − outflowUsd` over the
  complete-month window. Even-length samples average the two middle values.
- **FR-002**: The horizon is fixed at 12 months.
- **FR-003**: The tile renders nothing below `MIN_PROJECTION_MONTHS = 3` complete months.
- **FR-004**: The market-return toggle defaults to 0%. A non-zero rate compounds only the
  market-marked sleeves (`brokerageTotal + cryptoTotal` of the latest valid snapshot) over the
  horizon. Banking cash never compounds; neither do projected contributions, because the app
  does not know where new savings will land.
- **FR-005**: With no valid snapshot there is no market-marked base, so a non-zero rate has no
  effect. The tile says so rather than silently ignoring the selection.
- **FR-006**: Frontend-only. `monthlyFlow`, `totalNetWorthUsd` and the per-sleeve snapshot
  totals are already on the dashboard payload — no backend change, no migration, no new
  endpoint.
- **FR-007**: Projection arithmetic is covered in `dashboard.computed.spec.ts`: median
  selection, the <3-month gate, the 0% path, and one non-zero-return path.
- **FR-008**: The tile renders the projection as three addends — today's net worth, projected
  contributions (`median × horizon`), assumed market return — which sum to the headline figure.
  Signed addends carry an explicit `+`/`−`; a zero market return renders `$0`, not `+$0`.
- **FR-009**: The rate selection moves the market-return addend only. Today's net worth and the
  contributions addend are independent of it, which is what makes "held separate" checkable by
  the reader rather than a claim in prose.

---

## Out of Scope

- "When do I hit $X" — needs a goal entity the backend does not have.
- Any change to `Liquidity/Application/Services/CashFlowProjectionService`. It projects a
  single account's balance 30 days out from upcoming subscription charges: different question,
  different horizon.
- Persisting the chosen return rate across sessions or in the URL.
