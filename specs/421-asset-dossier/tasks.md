# Tasks: Asset Dossier

**Input**: specs/421-asset-dossier/spec.md + plan.md

---

## Phase 1: US1 — Aggregate Read Endpoint (P1) 🎯 This session

**Goal**: `GET /api/v1/research/assets/{symbol}/dossier` returns all per-ticker data in one response.

**Surface area**: Research/Domain/Ports, Research/API/Responses, Research/Application/Queries,
Research/API/Controllers, Integration adapters + registration, contract test.

- [x] T001 Create `IHoldingTaxLotsReader` port in `backend/src/FinanceSentry.Modules.Research/Domain/Ports/IHoldingTaxLotsReader.cs`
- [x] T001b Create `IAssetSignalReader` port in `backend/src/FinanceSentry.Modules.Research/Domain/Ports/IAssetSignalReader.cs`
- [x] T002 Create `AssetDossierResult.cs` response DTOs in `backend/src/FinanceSentry.Modules.Research/API/Responses/AssetDossierResult.cs`
- [x] T003 Create `GetAssetDossierQuery` handler in `backend/src/FinanceSentry.Modules.Research/Application/Queries/GetAssetDossierQuery.cs`
- [x] T004 Create `AssetDossierController` in `backend/src/FinanceSentry.Modules.Research/API/Controllers/AssetDossierController.cs`
- [x] T005 Create `HoldingTaxLotsAdapter` in `backend/src/FinanceSentry.Integration/HoldingTaxLotsAdapter.cs`
- [x] T005b Create `AssetSignalAdapter` in `backend/src/FinanceSentry.Integration/AssetSignalAdapter.cs`
- [x] T006 Register new ports in `backend/src/FinanceSentry.Integration/CrossModulePortRegistration.cs`
- [x] T007 Add BrokerageSync project reference to `backend/src/FinanceSentry.Integration/FinanceSentry.Integration.csproj`
- [x] T008 Create contract test `backend/tests/FinanceSentry.Tests.Integration/Research/AssetDossierContractTests.cs` — 4/4 passing
- [x] T009 `dotnet build FinanceSentry.sln` — 0 warnings, 0 errors
- [x] T010 `dotnet test --filter "Category!=Integration"` — all suites green, 4 new dossier tests pass

---

## Phase 2: US2 — Dossier UI Page (P1) — Next session

**Goal**: `/assets/:symbol` Angular page renders all dossier sections, degrades gracefully.

**Surface area**: AppRoute enum, new `assets` module (model, service, store, page), app.routes.ts,
holdings page (click navigation), Playwright spec.

- [x] T011 Add `AssetDossier` and `AssetDossierParam` to `AppRoute` enum
- [x] T012 Create `dossier.model.ts` mirroring backend DTOs in `frontend/src/app/modules/assets/models/`
- [x] T013 Create `dossier.service.ts` HTTP service in `frontend/src/app/modules/assets/services/`
- [x] T014 Create NgRx SignalStore (state/computed/methods/effects/store) in `frontend/src/app/modules/assets/store/`
- [x] T015 Create `asset-dossier.component.ts` + template in `frontend/src/app/modules/assets/pages/asset-dossier/`
- [x] T016 Add lazy-loaded route to `app.routes.ts`
- [x] T017 Add click navigation on holding rows in `holdings.component.html`
- [x] T018 Write Playwright spec: click holding → dossier renders → back navigation
- [x] T019 `ng lint` — zero errors; `npx playwright test` — 18/18 pass (7 new dossier tests)

### US2 polish (increment 4)

- [x] T025 Add `navigateToDossier` to `accounts-list.component.ts/.html` — brokerage and crypto position rows click through to `/assets/:symbol`
- [x] T026 Bind `analysts.trends` in `asset-dossier.component.html` — recommendation trend table below recent actions
- [x] T027 Add inline SVG sparkline to Radar Signals card in `asset-dossier.component.html` — computed from signal timestamps + severity, with latest-reading header; `radarSparklinePoints` and `latestSignal` computed signals on the component class
- [x] T028 Unit tests: `dossier.state.spec.ts`, `dossier.methods.spec.ts`, `dossier.computed.spec.ts` — 198 total unit tests pass
- [x] T029 Playwright: 4 new tests (accounts-list navigation, recommendation trend table, sparkline SVG, total 21/21)
- [x] T030 Fix `patch-lifekit-ui.js` `.d.ts` pattern to support both 0.2.0 and 0.2.2 library layouts
- [x] T031 `dotnet build FinanceSentry.sln` — 3 pre-existing warnings, 0 errors; `dotnet test --filter Category!=Integration` — all suites green
- [x] T032 `ng test --configuration=ci` — 198/198 pass; `npx playwright test --reporter=json` — 21/21 pass

---

## Phase 3: US3 — Ledger's Read (P2) 🎯 This session

**Goal**: On-demand AI summary generated via 040 agent loop, cached server-side.

**Surface area**: Research Domain (entity, repository, `ILedgerNarrator` port) · Research
Application (composer, staleness rule, query + command) · Research API (result DTO, two routes) ·
Research Infrastructure (repository, DbContext, migration M014, model snapshot) ·
`FinanceSentry.API/Adapters` + one Program.cs registration · frontend `assets` module +
error registry · contract, unit and Playwright tests.

- [x] T020 Design cache schema and API shape (plan.md "US3 — Ledger's Read")
- [x] T021 Backend: POST endpoint triggers agent, persists result — `GenerateAssetLedgerReadCommand`
      + `LedgerNarratorAdapter` over `IAgentConversationService`; `research.asset_ledger_reads` (M014)
- [x] T022 Backend: GET returns cached read + staleness flag — `GetAssetLedgerReadQuery`
      + `LedgerReadStaleness` (24h age OR dossier-fingerprint mismatch)
- [x] T023 Frontend: "Generate" button + cached-read section in dossier page — store slice
      (state/computed/methods/effects), `AssetLedgerReadDto`, `LEDGER_READ_UNAVAILABLE` message
- [x] T024 Contract test for generate + cache endpoints — `AssetLedgerReadContractTests` (7 tests)
      + `LedgerReadComposerTests`/`LedgerReadStalenessTests` (9 unit tests)
- [x] T033 `dotnet build FinanceSentry.sln -c Release` — 0 warnings, 0 errors;
      `dotnet test FinanceSentry.sln -m:1` — full backend suite green
- [x] T034 Frontend: `ng lint`, `ng test --configuration ci`, `ng build`,
      `playwright test --reporter=json` — all green (5 new dossier e2e tests)

---

### Edge-case closure (increment 6)

**Surface area**: `assets/store/dossier.computed.ts` · `pages/asset-dossier/*.html|.ts` ·
`dossier.computed.spec.ts` · `e2e/asset-dossier.spec.ts`. Frontend only — no backend change.

- [x] T035 `hasDossierSections` computed + `cmn-empty-state` "no data" state for a symbol whose
      every section is null/empty (spec Edge Cases: unknown ticker) — previously the page rendered
      a lone header with nothing under it
- [x] T036 `isLedgerReadStale` requires an actual narrative — the API reports a *missing* cache as
      stale, which flagged a never-generated read "out of date" on every first visit
- [x] T037 Unit tests: 10 new `dossier.computed.spec.ts` cases (per-section truth table,
      crypto not-applicable valuation, absent-read staleness) — 231/231 pass
- [x] T038 Playwright: no-data state for an unknown ticker; no stale tag before first generation
- [x] T039 `ng lint`, `prettier --check`, `ng test --configuration ci`, `ng build --production`,
      `playwright test` — all green; `dotnet build -c Release` 0 warnings, `dotnet test -m:1` green

---

## Dependencies & Execution Order

- **US1 (Phase 1)**: No external dependencies — all backend data already exists. Done.
- **US2 (Phase 2)**: Depends on US1 (needs the API endpoint). Done.
- **US3 (Phase 3)**: Depends on US2. Done.

All three story-slices have landed; feature 421 is complete. The bank/connection dossier is
explicitly out of v1 — file it separately.
