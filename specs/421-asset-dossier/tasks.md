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

---

## Phase 3: US3 — Ledger's Read (P2) — Future session

**Goal**: On-demand AI summary generated via 040 agent loop, cached server-side.

- [ ] T020 Design cache schema and API shape (plan update)
- [ ] T021 Backend: POST endpoint triggers agent, persists result
- [ ] T022 Backend: GET returns cached read + staleness flag
- [ ] T023 Frontend: "Generate" button + cached-read section in dossier page
- [ ] T024 Contract test for generate + cache endpoints

---

## Dependencies & Execution Order

- **US1 (Phase 1)**: No external dependencies — all backend data already exists. This session.
- **US2 (Phase 2)**: Depends on US1 (needs the API endpoint). Next session.
- **US3 (Phase 3)**: Depends on US2. Future session.
