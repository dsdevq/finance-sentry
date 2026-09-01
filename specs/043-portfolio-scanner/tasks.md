# Tasks: Portfolio Scanner (043)

## [US1] Daily portfolio-state signals

- [x] Add portfolio scanner + signal type constants to `RadarConstants.cs`
- [x] Define `IPortfolioScanDataReader` port + DTOs in `Modules.Radar/Domain/Ports/`
- [x] Implement `ComputePortfolioSignalsCommand` handler (iterate users → emit 4 signal types)
- [x] Implement `PortfolioScannerJob` Hangfire job
- [x] Register job in `RadarModule`
- [x] Implement `PortfolioScanDataReader` adapter in `Integration/`
- [x] Register adapter in `CrossModulePortRegistration`
- [x] Write unit tests (`Modules.Radar.Tests/Portfolio/PortfolioScannerTests.cs`)
- [x] Build passes (`dotnet build FinanceSentry.sln`) — 0 warnings, 0 errors
- [x] Tests pass (`dotnet test --filter Category!=Integration`) — 17 new tests pass; only pre-existing date-drift failure in unit suite
- [x] Commit spec artifacts + code

## [US2] Queryable via existing MCP tools

- [ ] Verify `list_signals?scanner=portfolio_scanner` returns portfolio signals (manual check or integration test)
