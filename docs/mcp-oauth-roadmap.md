# Finance Sentry MCP OAuth Roadmap

## Current State

As of this branch:

- `stdio` MCP uses a local `MCP_TOKEN` fallback identity.
- HTTP MCP requires per-request JWT authentication using:
  - `Authorization: Bearer <jwt>`, or
  - the existing `fs_access_token` cookie
- Identity is resolved per request for HTTP and from the startup token for `stdio`.

This is a meaningful improvement over the previous boot-time-only identity model, but it is **not yet a full OAuth-based MCP integration**.

## What "Real OAuth MCP" Means

For a remote MCP server, the target model is:

1. The MCP host authenticates the user with Finance Sentry using a standard OAuth flow.
2. The host receives short-lived access tokens scoped for MCP use.
3. The host refreshes those tokens through an OAuth-compatible token flow.
4. The MCP server authorizes each request based on the presented token.

That is materially different from:

- a long-lived env token pasted into `.env`
- a browser cookie intended for the frontend SPA
- a host manually injecting a static bearer token forever

## Reference Ecosystem Patterns

Observed patterns from public MCP servers:

- **GitHub remote MCP**
  - Preferred: host-supported OAuth
  - Fallback: PAT in `Authorization: Bearer ...`
- **GitHub local MCP**
  - Browser OAuth on first use
  - Token kept in memory or refreshed by the local server
  - PAT fallback available
- **Google Drive local MCP**
  - Local browser OAuth flow
  - Refresh-capable credentials stored locally

Finance Sentry should follow the same split:

- `stdio`: local OAuth/device flow or local managed credential
- HTTP: request-based OAuth bearer tokens

## Recommended Target Architecture

### `stdio` transport

Best target:

- Add a local login/bootstrap command for MCP
- Run browser-based login or device-code login
- Store refresh-capable MCP credentials locally
- Mint short-lived MCP access tokens automatically

Fallback:

- Keep `MCP_TOKEN` support for headless/dev workflows

### HTTP transport

Best target:

- Add a dedicated OAuth-compatible authorization surface for MCP clients
- Issue short-lived access tokens with an MCP audience/scope
- Support token refresh through a token endpoint
- Require bearer auth on every MCP HTTP request

## Phased Delivery Plan

### Phase 1: Completed on this branch

- HTTP MCP uses request-based JWT auth
- Identity comes from `HttpContext.User` for HTTP
- `stdio` still works with local `MCP_TOKEN`

### Phase 2: Dedicated MCP access tokens

Implement:

- dedicated short-lived MCP access token issuance
- explicit MCP audience or scope validation on HTTP
- a clean distinction between frontend SPA tokens and MCP tokens

Suggested result:

- HTTP MCP stops accepting the frontend cookie as a first-class auth mechanism
- remote MCP clients use bearer tokens only

### Phase 3: OAuth-compatible endpoints for MCP hosts

Implement:

- authorization endpoint
- token endpoint
- refresh token support for MCP clients
- client registration strategy:
  - fixed first-party clients at first
  - dynamic registration only if truly needed later

### Phase 4: Local MCP OAuth UX

Implement:

- `mcp login` or equivalent helper
- browser callback flow for desktop/dev clients
- device-code fallback for headless terminals
- local secure token storage

## Code-Level Next Step

The next engineering step should be:

1. Introduce **dedicated short-lived MCP access tokens**
2. Validate an MCP-specific audience for HTTP MCP
3. Stop treating the frontend cookie as the long-term remote MCP auth model

This is the smallest step that moves Finance Sentry toward real OAuth instead of extending the current hybrid state.

## Non-Goals For Now

Avoid doing these prematurely:

- dynamic OAuth client registration
- multi-tenant external app ecosystem support
- full OIDC discovery surface
- mixing local `stdio` bootstrap UX with remote HTTP auth concerns in one implementation

## Summary

The repo is now in a better state than before, but the real OAuth destination is:

- short-lived MCP bearer tokens
- refreshable credentials
- per-request authorization
- transport-specific auth behavior
- local login UX for `stdio`

That should be built in phases, not by stretching `MCP_TOKEN` further.
