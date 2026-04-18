# Versioning Rules

## VER-001 — Bump frontend version for any Angular change
**Category**: versioning
**Source**: constitution § Versioning & Tagging
**Added**: 2026-04-18

Any change to Angular components, services, models, styling, or routing requires a
version bump in `frontend/package.json`.

- PATCH: bug fixes, style refinements
- MINOR: new component/feature, new service method
- MAJOR: breaking UI changes

---

## VER-002 — Bump backend version for any API contract change
**Category**: versioning
**Source**: constitution § Versioning & Tagging
**Added**: 2026-04-18

Any change to REST API contract (new endpoint, parameter addition, response schema change)
requires a version bump in `FinanceSentry.API.csproj` `<Version>` field.

- PATCH: bug fixes, security updates
- MINOR: new endpoints, new optional parameters
- MAJOR: breaking changes (parameter removal, incompatible response structure)

---

## VER-003 — Create GitHub tag after merge to main
**Category**: versioning
**Source**: constitution § Versioning & Tagging
**Added**: 2026-04-18

After merging to main, create a tag:
- Frontend-only changes: `frontend-v<VERSION>`
- Backend-only changes: `backend-v<VERSION>`
- Both: coordinate and create separate tags

Tags must include release notes from the PR description.
