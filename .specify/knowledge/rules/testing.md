# Testing Rules

## TEST-001 — Unit tests required for all business logic
**Category**: testing
**Source**: constitution § Testing Discipline
**Added**: 2026-04-18

Coverage target: >80% for all business logic.
Backend: xUnit. Frontend: Vitest.

---

## TEST-002 — Integration tests required for inter-module contracts
**Category**: testing
**Source**: constitution § Testing Discipline
**Added**: 2026-04-18

Any communication across module boundaries must have an integration test
covering the full contract (request + response shape + error cases).

---

## TEST-003 — Contract tests required for every external API integration
**Category**: testing
**Source**: constitution § Testing Discipline
**Added**: 2026-04-18

Every integration with an external service (Plaid, Interactive Brokers, Binance) must
have a contract test that validates the external API still conforms to the shape the
domain interface expects. These tests run in CI.

---

## TEST-004 — Contract tests required for every REST endpoint (same PR)
**Category**: testing
**Source**: constitution § Testing Discipline
**Added**: 2026-04-18

Every new REST endpoint must ship with a contract test in the same PR.
The contract test validates request/response schema and status codes independently
of business logic.

---

## TEST-005 — Tests written before implementation (TDD)
**Category**: testing
**Source**: constitution § Testing Discipline
**Added**: 2026-04-18

Follow strict TDD: write the test first, watch it fail, then implement.
Tasks in tasks.md that add test tasks before implementation tasks must be
executed in that order.
