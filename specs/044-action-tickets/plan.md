# Plan: Action Tickets (044)

## Architecture decisions

| Decision | Choice | Why |
|---|---|---|
| Job home | `Modules.Research/Infrastructure/Jobs/ActionTicketsGeneratorJob.cs` | Research already owns IPS + `GetAllocationDriftQueryHandler`; `IAlertGeneratorService` in Core is already reachable; no new project reference |
| Alert representation | Existing `Alert` entity, Message = formatted order list string | US1 needs no new DB table; structured order data (US3+) can come later when the accept endpoint needs it |
| User iteration | `IIpsRepository.GetUserIdsWithCurrentIpsAsync()` | Same precedent as `OpportunityScanJob` — only users with an IPS get proposals |
| Drift source | Inject `IQueryHandler<GetAllocationDriftQuery, AllocationDriftDto>` | CQRS handlers are auto-registered; already used in MCP tool + Integration adapter |
| Alert dedup referenceId | MD5(`"rebalance:portfolio:{userId}"`) | Portfolio-wide alert needs stable per-user anchor, no natural entity reference; mirrors `ViolationReferenceId` pattern |
| Silence window | 24 h | One proposal per day; mirrors LowBalance/ThesisBroken cadence |
| Cron slot | `Cron.Daily(4)` (04:00 UTC) | After PortfolioScanner (02:00) and Research macro (03:00) |
| Order sizing | `|ActualValueUsd − TargetPct% × TotalUsd|` | Direct from drift DTO; no additional API calls |

## US1 surface — files touched / created

- `FinanceSentry.Modules.Alerts/Domain/AlertType.cs` — add `RebalanceProposal`
- `FinanceSentry.Modules.Companion/Domain/CompanionEventKind.cs` — add `RebalanceProposal`
- `FinanceSentry.Modules.Companion/Application/Services/MaterialityPolicy.cs` — add switch case
- `FinanceSentry.Core/Interfaces/IAlertGeneratorService.cs` — add `GenerateRebalanceProposalAlertAsync`
- `FinanceSentry.Modules.Alerts/Application/Services/AlertGeneratorService.cs` — implement it
- `FinanceSentry.Modules.Research/Infrastructure/Jobs/ActionTicketsGeneratorJob.cs` — new job (create)
- `FinanceSentry.Modules.Research/ResearchModule.cs` — register job + DI
- `tests/.../Opportunity/OpportunityFakes.cs` — stub new method in `FakeOpportunityAlertGenerator`
- `tests/.../ThesisMonitor/RunThesisMonitorHandlerTests.cs` — stub new method in `FakeAlertGeneratorService`
- `tests/FinanceSentry.Tests.Unit/Research/ActionTicketsGeneratorJobTests.cs` — unit tests (create)

## US2 surface (future slice)

- New `GenerateCashSweepProposalAlertAsync` in IAlertGeneratorService + AlertGeneratorService
- `CashSweepProposal` AlertType + CompanionEventKind + MaterialityPolicy mapping
- Extend `ActionTicketsGeneratorJob.ProcessUserAsync` with cash-sweep logic
- Needs `GetRiskRuleSetQuery` injected (cross-module; existing pattern via port or direct query handler)

## US3 surface (future slice)

- `AcknowledgeProposalCommand(Guid userId, Guid alertId, string choice)` in Alerts module
- REST endpoint `PATCH /alerts/{id}/acknowledge` in AlertsController
- Records Accept/Defer on the CompanionEvent (or marks Alert resolved if Accepted)

## US4 surface (future slice)

- New `get_action_tickets` MCP tool in `FinanceSentry.Mcp/Tools/`
- Reads open (unresolved, unread) RebalanceProposal + CashSweepProposal alerts
- Returns: alertId, type, title, message, createdAt
