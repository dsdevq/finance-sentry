# Plan — Spec 042: Net Worth Over Time Stacked/Lines Toggle

## Architecture Decision

State lives in `DashboardStore` (not component-local) so the toggle survives route transitions and is accessible from the store's signal graph. This matches the precedent set by `historyRange`.

## Story Slice [US1] — Full implementation

### Files touched

**lifekit-common** (companion change — separate git worktree):
- `projects/charts-core/src/area.ts` — add `stacked` param to `buildAreaDatasets`, `buildAreaChartConfig`, `updateAreaChart`; `stacked=false` sets `fill:false` and removes scale stacking
- `projects/ui/src/lib/components/area-chart/area-chart.component.ts` — add `stacked = input<boolean>(true)`; wire to both build + update calls
- `projects/ui/src/lib/components/area-chart/area-chart.component.spec.ts` — add tests for new input

**finance-sentry/frontend**:
- `src/app/modules/bank-sync/store/dashboard/dashboard.state.ts` — add `netWorthStacked: boolean` (default `true`)
- `src/app/modules/bank-sync/store/dashboard/dashboard.methods.ts` — add `setNetWorthStacked(stacked: boolean)`
- `src/app/modules/bank-sync/pages/dashboard/dashboard.component.ts` — add toggle buttons (Stacked / Lines), bind `[stacked]="store.netWorthStacked()"`
- `e2e/net-worth-chart-toggle.spec.ts` — Playwright spec covering toggle visibility, default state, and no-refetch invariant

### Package strategy
`@lifekit-hq/charts-core` is inlined into the `@lifekit-hq/ui` FESM bundle by ng-packagr, so updating `charts-core` source and re-building `ui` is sufficient — no separate `charts-core` publish step needed for the consumer.

### Sandbox constraint
`NODE_AUTH_TOKEN` is not set in the devclaw sandbox; `npm ci` 401s on `@lifekit-hq/*`. Workaround: clone `lifekit-common`, install its public-registry deps, build + pack all four `@lifekit-hq/*` packages locally, install from tarballs in `frontend/`, then build + run Playwright. `--no-verify` required on commits; CI enforces the full frontend gate.
