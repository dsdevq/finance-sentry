# Quickstart: Auth Flow (003-auth-flow)

**Date**: 2026-04-11

---

## Prerequisites

- Docker Compose stack running (`docker compose -f docker/docker-compose.dev.yml up -d --build`)
- Or: API + DB in Docker + frontend via `ng serve`

---

## Running the Auth Flow Locally

### 1. Register a user

```bash
curl -s -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"denys@finance.local","password":"Admin1234!"}' | jq .
```

Expected: `201` with `{ "token": "...", "expiresAt": "...", "userId": "..." }`

### 2. Log in

```bash
curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"denys@finance.local","password":"Admin1234!"}' | jq .
```

Expected: `200` with same shape.

### 3. Call a protected endpoint

```bash
TOKEN="<paste token here>"
curl -s http://localhost:5000/api/v1/accounts \
  -H "Authorization: Bearer $TOKEN" | jq .
```

Expected: `200` with accounts list (empty if no accounts linked yet).

### 4. Verify token rejection

```bash
curl -s http://localhost:5000/api/v1/accounts | jq .
```

Expected: `401` `{"error":"Authentication required.","errorCode":"UNAUTHORIZED"}`

---

## Frontend Verification

1. Navigate to `http://localhost:4200` — should redirect to `/login`
2. Register a new account on the register page
3. After registration, app redirects to `/accounts`
4. Accounts page loads (empty list is fine; no 401 errors in browser console)
5. Refresh the page — still authenticated (token persisted in localStorage)
6. Check browser DevTools → Application → Local Storage → `fs_auth_token` should be set
7. Log out — redirected to `/login`; `fs_auth_token` removed from localStorage

---

## Running Migrations

EF Core migrations run automatically on API startup (if configured) or manually:

```bash
# From backend directory
dotnet ef database update \
  --project src/FinanceSentry.Modules.Auth \
  --startup-project src/FinanceSentry.API \
  --context AuthDbContext
```

---

## Running Tests

```bash
# Backend (from backend/)
dotnet test

# Frontend (from frontend/)
ng test --watch=false --browsers=ChromeHeadless
```

---

## Key Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `JWT_SECRET` | HMAC-SHA256 signing key | `your-256-bit-secret-here` |
| `JWT_EXPIRATION_MINUTES` | Token lifetime | `60` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | `Host=postgres;Database=finance_sentry;...` |
