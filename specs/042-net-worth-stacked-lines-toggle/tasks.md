# Tasks — Spec 042: Net Worth Over Time Stacked/Lines Toggle

## [US1] Stacked/Lines toggle on the Net Worth Over Time chart

### Library (lifekit-hq/lifekit-common — PREREQUISITE, NOT YET MERGED)
- [ ] `projects/charts-core/src/area.ts` — add `stacked` param to `buildAreaDatasets`, `buildAreaChartConfig`, `updateAreaChart` (default `true`)
- [ ] `projects/ui/src/lib/components/area-chart/area-chart.component.ts` — add `stacked = input<boolean>(true)`, wire to build + update calls
- [ ] `projects/ui/src/lib/components/area-chart/area-chart.component.spec.ts` — add tests for `stacked` input
- [ ] Publish `@lifekit-hq/ui@0.2.3` to GitHub Packages

### finance-sentry (this repo — code complete, CI blocked by library)
- [x] `dashboard.state.ts` — add `netWorthStacked: boolean = true`
- [x] `dashboard.methods.ts` — add `setNetWorthStacked(stacked: boolean)`
- [x] `dashboard.component.ts` — add Stacked/Lines toggle buttons; bind `[stacked]="store.netWorthStacked()"` on `cmn-area-chart`
- [x] `e2e/net-worth-chart-toggle.spec.ts` — 3 Playwright tests (toggle visibility, default active, no-refetch invariant)
- [ ] Update `package-lock.json` to `@lifekit-hq/ui@0.2.3` after library is published; CI will pass then

### Status
finance-sentry code is correct. CI build will fail until the library PR merges and publishes `0.2.3`.
Library diff (charts-core/src/area.ts + area-chart.component.ts) is committed locally at
`/tmp/lifekit-common` branch `feat/406-area-chart-stacked-input` — Denys must push and merge it.
