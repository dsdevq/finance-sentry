# Tasks — Spec 045: Net Worth Projection from Savings Contributions

## [US1] Twelve-month projection tile with a market-return assumption

- [x] `dashboard.state.ts` — add `projectionReturnRate: number` (default `0`)
- [x] `dashboard.methods.ts` — add `setProjectionReturnRate(rate: number)`
- [x] `dashboard.constants.ts` — add `PROJECTION_RETURN_RATES` (0 / 3 / 5 / 7%),
      `PROJECTION_HORIZON_MONTHS` and `MIN_PROJECTION_MONTHS`
- [x] `dashboard.computed.ts` — median of complete-month net savings, `hasProjection` (≥3
      months), `projectedNetWorthFormatted`, `medianMonthlySavingsFormatted`,
      `projectionBasisLabel` (carries the sample size), `projectionAssumptionLabel`
- [x] `dashboard.component.ts` — projection tile gated on `store.hasProjection()`, rate toggle
      bound to `store.setProjectionReturnRate()`
- [x] `dashboard.computed.spec.ts` — 9 tests: median (not mean) selection, even-length sample,
      the <3-month gate, the exactly-3 turn-on, in-progress month excluded, a negative-median
      month, the 0% path, brokerage+crypto-only compounding, latest-snapshot base, and the
      no-snapshot degradation
- [x] `e2e/net-worth-projection.spec.ts` — Playwright: tile visible with 3+ complete months,
      absent below 3, rate toggle changes the figure without refetching

## Verification

- [x] `npm run lint` (`ng lint --max-warnings 0`) — all files pass
- [x] `npm run format:check` — all matched files use Prettier code style
- [x] `npx ng test finance-sentry --configuration ci` — 195 passed (34 files)
- [x] `npx ng build --configuration=production` — bundle generated
- [x] `npx playwright test --reporter=json` — 14 expected, 0 unexpected (3 new)

The repo's declared `verifyCmd` (`devclaw.json`) already ends in
`npx playwright test --reporter=json`, so the new browser layer is covered by the existing
gate with no change to it.
