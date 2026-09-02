# Tasks: Action Tickets (044)

## [US1] Rebalance proposal on IPS band breach

- [x] Add `RebalanceProposal` constant to `AlertType.cs` (`FinanceSentry.Modules.Alerts/Domain/`)
- [x] Add `RebalanceProposal` to `CompanionEventKind` enum (`FinanceSentry.Modules.Companion/Domain/`)
- [x] Add `"RebalanceProposal" => CompanionEventKind.RebalanceProposal` case in `MaterialityPolicy.ClassifyAlert`
- [x] Add `GenerateRebalanceProposalAlertAsync(Guid userId, int orderCount, string orderSummary, CancellationToken ct)` to `IAlertGeneratorService` (Core)
- [x] Implement `GenerateRebalanceProposalAlertAsync` in `AlertGeneratorService` (24 h silence, MD5 referenceId, Warning severity)
- [x] Create `ActionTicketsGeneratorJob` in `Modules.Research/Infrastructure/Jobs/`
- [x] Register `ActionTicketsGeneratorJob` in `ResearchModule` DI + `JobRegistrar` at `Cron.Daily(4)`
- [x] Add stub `GenerateRebalanceProposalAlertAsync` to `FakeOpportunityAlertGenerator` in `OpportunityFakes.cs`
- [x] Add stub `GenerateRebalanceProposalAlertAsync` to `FakeAlertGeneratorService` in `RunThesisMonitorHandlerTests.cs`
- [x] Write unit tests for `ActionTicketsGeneratorJob` in `FinanceSentry.Modules.Research.Tests/Jobs/ActionTicketsGeneratorJobTests.cs` (8 tests)
- [x] Build passes (`dotnet build FinanceSentry.sln`) — 0 errors, 3 pre-existing CS1587 warnings only
- [x] Tests pass (`dotnet test --filter Category!=Integration`) — 8 new tests green, 0 failures
- [x] Commit spec artifacts + code

## [US2] Cash-sweep proposal on idle cash excess

- [x] Add `CashSweepProposal` AlertType + CompanionEventKind + MaterialityPolicy mapping
- [x] Add `GenerateCashSweepProposalAlertAsync` to IAlertGeneratorService + AlertGeneratorService (24h silence, MD5 per-user anchor)
- [x] Add ProjectReference from Research → Risk (direct query handler injection; no cycle)
- [x] Extend `ActionTicketsGeneratorJob` with cash-sweep branch: inject `IQueryHandler<GetRiskRuleSetQuery, RiskRuleSetDto?>`, compute excess when `CashUsd > minBufferPct% × TotalValueUsd`
- [x] Add stub `GenerateCashSweepProposalAlertAsync` to `FakeOpportunityAlertGenerator` + `FakeAlertGeneratorService`
- [x] Add ProjectReference from Research.Tests → Risk (for mock setup)
- [x] Write 6 unit tests for cash-sweep branch (cash exceeds, below, no rules, zero pct, error isolation, both proposals at once)
- [x] Build passes — 0 errors, 3 pre-existing CS1587 warnings only
- [x] Tests pass — all green, 214 Research tests total

## [US3] One-tap acknowledgement

- [x] Add `AcknowledgementDecision` (nullable string, max 10) + `AcknowledgedAt` to `Alert` entity
- [x] Migration M003 (`20260902000000_M003_AddAcknowledgement.cs`) — adds both columns
- [x] Update `AlertsDbContext.OnModelCreating` + `AlertsDbContextModelSnapshot` for new columns
- [x] Add `AcknowledgeAsync(userId, alertId, decision)` to `IAlertRepository` + `AlertRepository` (Accept → resolve; Defer → record only)
- [x] Add `AcknowledgeByReferenceAsync(userId, alertType, referenceId, decision)` to `IAlertRepository` + `AlertRepository` (bot path using stable anchor from wake payload)
- [x] `AcknowledgeProposalCommand` + handler + `AcknowledgeProposalByReferenceCommand` + handler in Alerts module
- [x] `PATCH /alerts/{id}/acknowledge` — frontend/app path (alert row UUID)
- [x] `PATCH /alerts/by-reference/{referenceId}/acknowledge` — Telegram bot path (stable anchor from wake payload)
- [x] Extend `WebhookAgentWakeDispatcher.WakeAsync` payload: adds `requiresAcknowledgement: true` + `referenceId` for `RebalanceProposal` / `CashSweepProposal` events
- [x] Build passes — 0 errors
- [x] Tests pass — 0 failures

## [US4] MCP tool — get_action_tickets (future)

- [ ] `GetActionTicketsTool` in `FinanceSentry.Mcp/Tools/`
- [ ] MCP contract test
