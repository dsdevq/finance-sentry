# Architecture Rules

## ARCH-001 — Modular monolith with DDD
**Category**: architecture
**Source**: constitution § I
**Added**: 2026-04-18

All backend services are organized as a modular monolith using ASP.NET 9+.
Each financial integration is a distinct, self-contained module.
Apply domain-driven design: entities, repositories, domain services in Domain layer;
EF Core, HTTP clients in Infrastructure layer; MediatR commands/queries in Application layer.

---

## ARCH-002 — Modules are independently deployable and testable
**Category**: architecture
**Source**: constitution § I
**Added**: 2026-04-18

Each module must be testable in isolation without depending on other modules' internals.
Shared code lives in a dedicated shared kernel, not in any single module.

---

## ARCH-003 — CQRS via MediatR
**Category**: architecture
**Source**: CLAUDE.md
**Added**: 2026-04-18

Use MediatR for all command/query dispatching in the Application layer.
- Commands: mutate state, return void or a result ID
- Queries: read-only, return DTOs

Never put business logic in controllers. Controllers dispatch to MediatR only.

---

## ARCH-004 — Docker for all services
**Category**: architecture
**Source**: constitution § Tech Stack
**Added**: 2026-04-18

All services run in Docker. Local development uses:
```bash
cd docker && docker compose -f docker-compose.dev.yml up -d --build
```

Never change `Host=postgres` to `localhost` — fix Docker instead.
Never modify connection strings as workarounds.

---

## ARCH-005 — No markdown files at repo root (except README.md and CLAUDE.md)
**Category**: architecture
**Source**: CLAUDE.md
**Added**: 2026-04-18

Do not create session artifacts, debug notes, or how-to docs at the repo root.
Put relevant content in `README.md` or the appropriate `.specify/` artifact.
