# Quickstart: Testing Google OAuth Locally (004-adopt-oauth)

## Prerequisites

1. **Google Cloud Console setup**:
   - Go to [console.cloud.google.com](https://console.cloud.google.com) → APIs & Services → Credentials
   - Create (or use existing) OAuth 2.0 Client ID (Application type: Web application)
   - Add `http://localhost:5000/api/v1/auth/google/callback` as an authorized redirect URI
   - Note your `Client ID` and `Client Secret`

2. **Set environment variables** (do NOT commit these):
   ```bash
   # .env file at repo root (gitignored) or export in shell
   export GOOGLE_CLIENT_ID=your_client_id_here
   export GOOGLE_CLIENT_SECRET=your_client_secret_here
   ```

## Running the Stack

```bash
# Terminal 1 — start with OAuth env vars
cd docker
GOOGLE_CLIENT_ID=$GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET=$GOOGLE_CLIENT_SECRET \
  docker compose -f docker-compose.dev.yml up -d --build

# Or with a .env file in the docker/ directory:
docker compose -f docker-compose.dev.yml up -d --build
```

The `docker-compose.dev.yml` reads `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` from the host environment (or `.env` file) via variable substitution.

## Validating the Flow

### 1. Test the login redirect
```bash
curl -v "http://localhost:5000/api/v1/auth/google/login"
# Expect: 302 → Location: https://accounts.google.com/o/oauth2/v2/auth?...
```

### 2. Test the full browser flow
1. Open `http://localhost:4200/login`
2. Click "Continue with Google"
3. Approve the Google consent screen
4. Expect: redirect to `http://localhost:4200/accounts` with valid session

### 3. Test cancelled flow
1. Open `http://localhost:4200/login`
2. Click "Continue with Google"
3. On the Google consent screen, click "Cancel" or close the tab
4. Expect: redirect to `/login?info=google_cancelled` with info banner

### 4. Test Google-only account blocks password login
1. Sign up via Google first
2. On the login page, enter the same Gmail address and any password
3. Click "Log In"
4. Expect: error message "This account uses Google sign-in. Click 'Continue with Google' instead."

## Running Contract Tests
```bash
cd backend
dotnet test tests/FinanceSentry.Tests.Integration \
  --filter "FullyQualifiedName~GoogleOAuth" \
  --logger "console;verbosity=normal"
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| 302 Location is missing `client_id` | `GOOGLEOAUTH__CLIENTID` env var not set | Check docker-compose env or export the variable |
| 400 `INVALID_OAUTH_STATE` on callback | State expired or DB not migrated | Run `dotnet ef database update` or restart Docker |
| `redirect_uri_mismatch` from Google | Redirect URI not registered | Add `http://localhost:5000/api/v1/auth/google/callback` in Google Cloud Console |
| CORS error in browser | Missing CORS origin | Check `WithOrigins("http://localhost:4200")` in Program.cs |
