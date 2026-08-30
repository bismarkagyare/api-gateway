# Project Guidelines

Lightweight .NET 10 API gateway (API-key auth → Redis rate limiting → path-based reverse proxy). User-facing usage lives in `README.md`; this file captures agent-critical facts not documented there.

## Build and Run
- Build: `dotnet build RateLimitedApiGateway.sln` (three projects, all `net10.0`)
- Gateway: `dotnet run --project Gateway.Api` → http://localhost:5136
- Mocks: `Downstream.MockApi` → :5099 (products), `Downstream.OrdersApi` → :5101 (orders)
- Requires a Redis server (default `localhost:6379`). The gateway boots with Redis down (`AbortOnConnectFail=false`), but auth and rate limiting need it.

## Architecture
- `Gateway.Api/` — the gateway. Pipeline: `RequestLoggingMiddleware` → `ApiKeyAuthenticationMiddleware` → `RateLimitingMiddleware` → `ProxyController` (catch-all `/proxy/{**path}`) → `ProxyService`.
- `Downstream.MockApi/`, `Downstream.OrdersApi/` — mock downstream services.
- API keys are Redis hashes `apikey:meta:<key>` with fields `plan`, `maxRequests`, `windowSeconds`, `isActive`.

## Conventions
- **Rate limiting** is fixed-window, per API key, and MUST stay atomic: a single Lua script in `RedisRateLimitService` does INCR + PEXPIRE (on first request) + PTTL in one round trip. Don't revert to separate INCR/EXPIRE calls — that was a race plus a 60× window bug.
- **Routing** is config-driven via `Downstream:Routes` (path prefix → target base URL); `ProxyService.ResolveRoute` matches the longest prefix at segment boundaries. Never hardcode downstream URLs.
- **Middleware** is constructed from the root provider, so constructor-injected services must be registered as **singletons** (scoped → startup crash).
- **No test project exists** — add one before relying on refactors.

## Gotchas
- `net9.0` is EOL; keep everything on `net10.0`.
- `/health` requires a valid API key (it sits behind the auth middleware).
- `Microsoft.AspNetCore.OpenApi` is pinned to a version that pulls patched `Microsoft.OpenApi` 2.7.5 — don't force OpenApi 3.x (breaks the source generator).
