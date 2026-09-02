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

## [US2] Cash-sweep proposal on idle cash excess (future)

- [ ] Add `CashSweepProposal` AlertType + CompanionEventKind + MaterialityPolicy mapping
- [ ] Add `GenerateCashSweepProposalAlertAsync` to IAlertGeneratorService + AlertGeneratorService
- [ ] Extend `ActionTicketsGeneratorJob` with cash-sweep branch (inject risk rule set query handler)
- [ ] Unit tests for cash-sweep branch

## [US3] One-tap acknowledgement (future)

- [ ] `AcknowledgeProposalCommand` + handler in Alerts module
- [ ] `PATCH /alerts/{id}/acknowledge` endpoint in AlertsController
- [ ] Unit/contract tests

## [US4] MCP tool — get_action_tickets (future)

- [ ] `GetActionTicketsTool` in `FinanceSentry.Mcp/Tools/`
- [ ] MCP contract test
