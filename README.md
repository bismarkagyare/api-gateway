# API Gateway

A lightweight API gateway that authenticates requests using API keys, applies per-API-key rate limiting, and proxies traffic to downstream services. Built to explore how real API gateways enforce traffic and protection policies.

## Features

- **API Key Authentication** via `X-API-Key` header
- **Per-API-Key Rate Limiting** with Redis-backed distributed storage
- **Fixed-Window Rate Limiting** using atomic counters
- **Rate-Limit Response Headers** (`X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`)
- **Reverse Proxy** to downstream APIs
- **Stateless & Horizontally Scalable**

## Architecture

The gateway consists of two ASP.NET Core applications:

- **Gateway.Api** – The main API gateway with authentication, rate limiting, and request proxying
- **Downstream.MockApi** – Mock downstream service that serves product data

### Request Pipeline

Incoming requests flow through middleware in this order:

1. **API Key Authentication** – Extract and validate API key from `X-API-Key` header; load metadata from Redis
2. **Rate Limiting** – Evaluate per-API-key rate limits; add rate-limit headers; block if exceeded
3. **Request Routing** – Forward allowed requests to downstream services

Failed authentication or rate-limit violations are blocked before reaching downstream services.

## Rate Limiting Details

- **Strategy:** Fixed-window counter
- **Storage:** Redis with automatic key expiration
- **Scope:** Per API key
- **Reset:** Automatic when Redis key expires

### Rate-Limit Headers

All responses include:

- `X-RateLimit-Limit` – Max requests per window
- `X-RateLimit-Remaining` – Requests remaining in current window
- `X-RateLimit-Reset` – Unix timestamp when limit resets

## Prerequisites

- .NET 8+ SDK
- Redis server

## Getting Started

### 1. Start Redis

```bash
redis-server
```

### 2. Configure Redis Connection

Edit `Gateway.Api/appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RateLimit": {
    "WindowSeconds": 60,
    "MaxRequests": 100
  },
  "Downstream": {
    "BaseUrl": "http://localhost:5099"
  }
}
```

### 3. Load API Keys into Redis

Store API key metadata as Redis hashes:

```bash
redis-cli
HSET apikey:meta:test-key-1 plan premium maxRequests 100 windowSeconds 60 isActive true
HSET apikey:meta:test-key-2 plan free maxRequests 10 windowSeconds 60 isActive true
```

### 4. Run Gateway.Api

```bash
dotnet run --project Gateway.Api
```

The gateway starts on `http://localhost:5136`.

### 5. (Optional) Run Downstream.MockApi

```bash
dotnet run --project Downstream.MockApi
```

The mock API runs on `http://localhost:5099`.

## Testing

### Health Check

```bash
curl http://localhost:5136/health
```

**Response:**
```json
{"status":"healthy"}
```

### Valid Request (with API key)

```bash
curl -H "X-API-Key: test-key-1" http://localhost:5136/proxy/products
```

**Response:**
```json
[
  { "id": 1, "name": "Keyboard", "price": 100 },
  { "id": 2, "name": "Mouse", "price": 50 }
]
```

### Missing API Key

```bash
curl http://localhost:5136/proxy/products
```

**Response:** `401 Unauthorized` – "API key is missing"

### Rate Limit Exceeded

After exhausting your API key's limit:

```
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1735775400

Rate limit exceeded. Try again later.
```

## Running Tests

```bash
# Build all projects
dotnet build

# Run the solution
dotnet run --project Gateway.Api
```

## Future Enhancements

- Sliding-window rate limiting
- Per-endpoint rate limits
- Metrics & observability (Prometheus, Application Insights)
- Circuit breaker for downstream services
- Request/response logging
- Admin API for managing API keys and limits