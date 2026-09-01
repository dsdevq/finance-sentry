# Tasks — Spec 042: Net Worth Over Time Stacked/Lines Toggle

## [US1] Stacked/Lines toggle on the Net Worth Over Time chart

### Library (lifekit-hq/lifekit-common — NOT YET MERGED into main)
- [x] `projects/charts-core/src/area.ts` — add `stacked` param to `buildAreaDatasets`, `buildAreaChartConfig`, `updateAreaChart` (default `true`)
- [x] `projects/ui/src/lib/components/area-chart/area-chart.component.ts` — add `stacked = input<boolean>(true)`, wire to build + update calls
- [ ] `projects/ui/src/lib/components/area-chart/area-chart.component.spec.ts` — add tests for `stacked` input
- [ ] Publish `@lifekit-hq/ui@0.2.3` to GitHub Packages (remove postinstall shim when done)

### finance-sentry (this repo)
- [x] `dashboard.state.ts` — add `netWorthStacked: boolean = true`
- [x] `dashboard.methods.ts` — add `setNetWorthStacked(stacked: boolean)`
- [x] `dashboard.component.ts` — add Stacked/Lines toggle buttons; bind `[stacked]="store.netWorthStacked()"` on `cmn-area-chart`
- [x] `e2e/net-worth-chart-toggle.spec.ts` — 3 Playwright tests (toggle visibility, default active, no-refetch invariant)
- [x] `frontend/scripts/patch-lifekit-ui.js` + `postinstall` in `package.json` — shim that patches `@lifekit-hq/ui@0.2.0` after `npm ci` until 0.2.3 is published

### Status
All finance-sentry code is complete and CI passes. The postinstall shim patches the published
`@lifekit-hq/ui@0.2.0` to add the `stacked` input, bridging the gap until the library ships 0.2.3.
Remove the shim (`frontend/scripts/patch-lifekit-ui.js` + `postinstall` in `package.json`) once
`@lifekit-hq/ui@0.2.3` is published and `package.json` is updated to reference it.
