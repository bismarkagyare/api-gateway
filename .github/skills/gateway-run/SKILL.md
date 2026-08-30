---
name: gateway-run
description: 'Run and verify the API gateway stack. Use when starting the gateway and mock services, connecting to or troubleshooting Redis, loading API keys, or running the end-to-end smoke test.'
---

# Gateway Run & Verify

Run the full API gateway stack and verify it end-to-end.

## When to Use
- Starting the gateway (`Gateway.Api`) or the mock services
- Loading API keys into Redis
- Verifying auth, rate limiting, and path routing with the smoke test
- Troubleshooting "401 on every route" or "rate limit not applying"

## Prerequisites
- .NET 10 SDK
- A running Redis server (see [Troubleshooting](#troubleshooting) if unavailable)
- Gateway and mocks built: `dotnet build RateLimitedApiGateway.sln`

## Procedure

1. **Start Redis** — confirm it's reachable on `localhost:6379`.
2. **Load API keys** — run [load-keys.ps1](./scripts/load-keys.ps1) to create the sample keys (`test-key-1`, `test-key-2`) as Redis hashes.
3. **Start the mocks** (each in its own terminal):
   - `dotnet run --project Downstream.MockApi` → http://localhost:5099 (products)
   - `dotnet run --project Downstream.OrdersApi` → http://localhost:5101 (orders)
4. **Start the gateway**:
   - `dotnet run --project Gateway.Api` → http://localhost:5136
5. **Verify** — run [smoke-test.ps1](./scripts/smoke-test.ps1). It checks the mocks directly, then the gateway with `X-API-Key: test-key-1`, printing the rate-limit headers.

## Endpoints
| Route | Behavior |
|---|---|
| `GET /health` (with key) | Probes Redis + all downstream routes |
| `GET /proxy/products` (with key) | Forwards to `Downstream.MockApi` |
| `GET /proxy/orders` (with key) | Forwards to `Downstream.OrdersApi` |
| `GET /proxy/<anything-else>` | 404 `RouteNotFound` |

## Troubleshooting
- **401 on every route** — Redis is down or the key isn't loaded; the gateway boots without Redis but auth needs it. Run `load-keys.ps1` and confirm `redis-cli ping`.
- **Rate limit never trips** — `maxRequests` is per-key per-window; lower `maxRequests` for the key in Redis to force a 429, or fire more than `maxRequests` requests within `WindowSeconds`.
- **Health says unhealthy** — check Redis and that both mocks are running.
