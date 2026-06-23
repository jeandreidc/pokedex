# Kota Pokedex

A full-stack Pokedex application built for a take-home exercise. This repository implements **Feature #2: Search & Filter with Pagination** — a backend API that proxies and normalizes [PokeAPI](https://pokeapi.co/docs/v2) data with caching, rate limiting, and observability.

The Angular frontend (`src/web/`) is planned separately; this README focuses on the backend delivered in this phase.

---

## Feature Implemented

**Search & Filter with Pagination**

- Combinable filters: text search, type, ability, and generation
- Server-side filter intersection and pagination
- Filter metadata endpoints for Angular dropdowns
- All PokeAPI access goes through the backend (no direct browser calls)

---

## How to Run

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for Redis via Aspire or Skaffold)
- Optional: [Skaffold](https://skaffold.dev/) + Kubernetes (Docker Desktop, minikube, etc.)

### Option 1 — Aspire (recommended for local dev + dashboard)

Runs the API, Redis, and opens the **Aspire Dashboard** for traces and metrics.

```bash
dotnet run --project src/Kota.Pokedex.AppHost
```

- **API**: URL shown in the Aspire dashboard (typically `https://localhost:7xxx`)
- **Aspire Dashboard**: `https://localhost:18888` (traces, metrics, logs)
- **Redis**: provisioned automatically; cache provider set to `Redis`

### Option 2 — API only (in-memory cache)

```bash
dotnet run --project src/Kota.Pokedex.Api
```

Uses in-memory cache (`Cache:Provider = Memory` in `appsettings.json`). Good for quick testing without Docker.

### Option 3 — Skaffold (Kubernetes)

Deploys API, Redis, and Aspire Dashboard to a local cluster.

```bash
cd infra
skaffold dev
```

Port forwards:

| Service           | Local Port |
|-------------------|------------|
| API               | 8080       |
| Redis             | 6379       |
| Aspire Dashboard  | 18888      |

---

## API Endpoints

### Search Pokemon (combinable filters)

```http
GET /api/pokemon?search=pika&type=fire&ability=blaze&generation=1&page=1&pageSize=20
```

| Parameter    | Description                                      |
|-------------|--------------------------------------------------|
| `search`    | Case-insensitive name contains (optional)        |
| `type`      | Pokemon type slug, e.g. `fire` (optional)        |
| `ability`   | Ability slug, e.g. `overgrow` (optional)         |
| `generation`| Generation id (`1`) or slug (`generation-i`)     |
| `page`      | Page number (default: 1)                         |
| `pageSize`  | Items per page (default: 20, max: 100)           |

**Response:**

```json
{
  "items": [
    { "id": 25, "name": "pikachu", "spriteUrl": "...", "types": ["electric"] }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

### Get Pokemon by id or name

```http
GET /api/pokemon/25
GET /api/pokemon/pikachu
```

### Filter metadata (for Angular dropdowns)

```http
GET /api/filters/types
GET /api/filters/generations
GET /api/filters/abilities?page=1&pageSize=50
GET /api/filters/abilities?search=bl&page=1&pageSize=50
```

| Endpoint | When to call | Returns |
|----------|--------------|---------|
| `/api/filters/types` | On dropdown open | Full list (~18 types) |
| `/api/filters/generations` | On dropdown open | Full list (~9 generations) |
| `/api/filters/abilities` | On dropdown open | First page of abilities (default 50) |
| `/api/filters/abilities?search=bl` | As user types (optional) | Narrowed ability list |

Selecting a dropdown value sends the slug to the search endpoint, e.g. `GET /api/pokemon?type=fire` or `GET /api/pokemon?ability=blaze`.

### Health checks

```http
GET /health
GET /alive
```

### Swagger UI (API landing page)

Open the root URL in a browser to explore and test all endpoints interactively:

```http
GET /
```

The OpenAPI spec is available at `/swagger/v1/swagger.json`.

---

## Architecture

```
Angular (future)  →  Kota.Pokedex.Api  →  MediatR (CQRS)
                              ↓
                    Application (Queries)
                              ↓
              Infrastructure (PokeAPI client, cache, prefetch)
                              ↓
                    PokeAPI  +  Redis / Memory Cache
```

### Projects

| Project | Role |
|---------|------|
| `Kota.Pokedex.Api` | HTTP endpoints, CORS, rate limiting |
| `Kota.Pokedex.Application` | CQRS queries and handlers |
| `Kota.Pokedex.Core` | Interfaces, models, options |
| `Kota.Pokedex.Infrastructure` | PokeAPI client, cache, prefetch |
| `Kota.Pokedex.ServiceDefaults` | OpenTelemetry, health checks, resilience |
| `Kota.Pokedex.AppHost` | Aspire orchestration (API + Redis) |

See [ARCHITECTURE.md](ARCHITECTURE.md) for full guidelines.

---

## Key Decisions & Justifications

### 0. Feature choice: Search & Filter with Pagination (#2)

We chose Feature #2 from the take-home exercise because it best demonstrates backend skills relevant to the role:

- **CQRS read path** — queries, handlers, DTOs, pagination
- **External API integration** — PokeAPI proxy with caching and rate limiting
- **Data transformation** — normalizing PokeAPI responses into clean frontend contracts
- **Product thinking** — combinable filters, dropdown UX, search

Feature #5 (Favorites) was considered as a complementary second feature but deferred to stay within the 2–3 hour scope. The current architecture supports adding it later via CQRS commands without restructuring.

---

### 1. Layered architecture (Clean Architecture)

The solution is split into four backend layers with strict dependency rules:

```
Api → Application → Core ← Infrastructure
```

| Project | Responsibility |
|---------|----------------|
| `Kota.Pokedex.Core` | Interfaces, domain models, options — no framework dependencies |
| `Kota.Pokedex.Application` | CQRS queries/handlers, DTOs — references Core only |
| `Kota.Pokedex.Infrastructure` | PokeAPI client, cache, prefetch — implements Core interfaces |
| `Kota.Pokedex.Api` | HTTP boundary — controllers, CORS, rate limiting, Swagger |

**Why:** Each layer has a single reason to change. Business logic lives in Application handlers, not controllers. Infrastructure is swappable (e.g. Memory cache → Redis) without touching handlers.

---

### 2. CQRS with MediatR

Each use case is a single query + handler (`SearchPokemonQuery`, `GetFilterTypesQuery`, etc.). Controllers only dispatch messages via `IMediator.Send()` — no business logic at the HTTP layer.

**Why:** Keeps endpoints thin, makes each use case testable in isolation, and makes it straightforward to add write operations (e.g. Favorites commands) without bloating controllers.

---

### 3. Backend-only PokeAPI access

The Angular frontend never calls PokeAPI directly. All external API access goes through our backend.

**Why:**
- Centralized caching and rate limiting
- Consistent error handling and response shape for the frontend
- Hides PokeAPI complexity (multiple endpoints, nested responses, no search)
- Aligns with PokeAPI fair use policy (*"locally cache resources whenever you request them"*)

---

### 4. PokeAPI has no server-side search or compound filters

PokeAPI list endpoints only support `limit` and `offset`. There is no `?name=` or `?type=` on `/pokemon`.

| Filter | PokeAPI support | Our approach |
|--------|-----------------|--------------|
| Type | `GET /type/{name}` returns `pokemon[]` | Cache type → pokemon id set |
| Ability | `GET /ability/{name}` returns `pokemon[]` | Cache ability → pokemon id set |
| Generation | `GET /generation/{name}` returns `pokemon_species[]` | Map species names to pokemon ids via index |
| Text search | **None** | Prefetch full pokemon name index; filter in-memory |

When multiple filters are active, we **intersect** the candidate id sets, then apply text search, then paginate server-side. This gives combinable filters without relying on unsupported PokeAPI query params.

---

### 5. Project structure: `src/`, `tests/`, `infra/`

```
Kota.Pokedex/
├── src/          # Application source (Api, Core, Application, Infrastructure, web/)
├── tests/        # Unit, integration, E2E test projects
├── infra/        # Deployment scaffolding (Docker, Skaffold, K8s)
└── *.sln         # .NET solution (frontend managed separately via npm)
```

**Why:** Separates deploy concerns from application code. `infra/` (Docker, Skaffold, K8s manifests) is distinct from `src/Kota.Pokedex.Infrastructure/` (application-layer implementations like PokeAPI client and cache).

---

### 6. Angular frontend (planned) + CORS

The frontend will be an Angular SPA at `src/web/`, calling the backend API only. CORS is configured for Angular's default dev port (`4200`).

**Why Angular:** User's preferred frontend stack for the take-home. Backend is frontend-agnostic — any SPA consuming the REST API works.

---

### 7. Aggressive caching

| Data | TTL | Reason |
|------|-----|--------|
| Full pokemon index | 24h | Powers text search and base pagination |
| Filter dropdown lists (types, abilities, generations) | 7d | Stable reference data for dropdowns |
| Type / ability / generation → pokemon id sets | 24h | Filter result sets rarely change |
| Individual pokemon types | 24h | Lazy-loaded for result hydration |

Implemented via `ICacheService` with two providers switchable by config:

```json
"Cache": { "Provider": "Memory" }   // local dev, single instance
"Cache": { "Provider": "Redis" }    // Skaffold/K8s, shared across replicas
```

**Why Memory:** Zero-dependency for quick local runs.
**Why Redis:** Required when running multiple API pods — shared cache across replicas under Aspire or Skaffold.

---

### 8. IHttpClientFactory for PokeAPI calls

All outbound HTTP uses a typed `HttpClient` registered via `IHttpClientFactory`:

- **Connection pooling** — avoids socket exhaustion under load
- **DNS refresh** — handled by the factory
- **Standard resilience handler** — retry + circuit breaker for transient failures
- **Semaphore throttle** — max 5 concurrent outbound requests (configurable)

Service Discovery is **not** applied to HttpClient defaults — it was removed because Aspire's service discovery incorrectly intercepted external PokeAPI URLs (`https://pokeapi.co`) as internal service names.

---

### 9. Two-layer rate limiting

| Layer | Mechanism | Purpose |
|-------|-----------|---------|
| **Inbound** | ASP.NET Core `RateLimiter` (100 req/min/IP) | Protect our API from abuse |
| **Outbound** | SemaphoreSlim on `PokeApiClient` | Respect PokeAPI fair use; limit concurrent calls |

PokeAPI removed hard rate limits in 2018, but fair use policy still applies. Caching minimizes outbound calls; throttling is a safety net.

---

### 10. Startup prefetch after `ApplicationStarted`

`PokemonPrefetchHostedService` warms data **after** the host is fully started (via `IHostApplicationLifetime.ApplicationStarted`), not during boot:

| Prefetched at startup | PokeAPI calls | Why |
|-----------------------|---------------|-----|
| Pokemon name index | ~13 (`limit=100`) | PokeAPI has no text search — required for `?search=` |
| Types list | ~1 | Dropdown populated instantly on first open |
| Generations list | ~1 | Dropdown populated instantly on first open |
| Abilities list | ~4 | Dropdown shows first page immediately without typing |

Managed by `PokemonIndexService` (pokemon data) and `FilterMetadataService` (dropdown lists).

**Other prefetch strategies for scale:**

| Method | When to use |
|--------|-------------|
| Startup warmup | Default — load index + filter lists before traffic |
| Cache-aside | Type/ability/generation pokemon-id sets on first filter use |
| Background refresh | Periodic re-fetch of cached lists (24h timer) |
| Batch hydration | Parallel fetch with throttle for page detail enrichment |

---

### 11. Filter dropdown UX — show options on open, typeahead optional

Dropdown filters and pokemon text search serve different purposes and use different endpoints:

| User action | Endpoint | Behaviour |
|-------------|----------|-----------|
| Types dropdown open | `GET /api/filters/types` | Full list returned immediately (~18 items) |
| Generations dropdown open | `GET /api/filters/generations` | Full list returned immediately (~9 items) |
| Abilities dropdown open | `GET /api/filters/abilities?page=1&pageSize=50` | First 50 abilities immediately — **no typing required** |
| User types in ability dropdown | `GET /api/filters/abilities?search=bl` | Optional typeahead to narrow ~367 abilities |
| User types in pokemon search bar | `GET /api/pokemon?search=pika` | Filters pokemon results by name |
| User selects a filter value | `GET /api/pokemon?type=fire` | Applies selected filter to pokemon results |

**Why prefetch filter lists?** Users should not need to know ability names upfront. Opening a dropdown should populate it instantly from cache.

**Why paginate abilities only?** Types (~18) and generations (~9) fit in one response. Abilities (~367) use pagination so the dropdown loads the first page on open and fetches more on scroll.

**Why separate ability `search` from pokemon `search`?**
- `/api/filters/abilities?search=bl` — helps the user **find a dropdown option** (typeahead)
- `/api/pokemon?search=pika` — filters **pokemon results** by name

---

### 12. Separate filter metadata endpoints

`/api/filters/types`, `/api/filters/abilities`, and `/api/filters/generations` are separate from `/api/pokemon` so the Angular frontend can populate dropdowns independently of search results. Filter selection sends slugs to `GET /api/pokemon` — the two concerns do not overlap.

---

### 13. Swagger UI as API landing page

Swashbuckle serves interactive API docs at the root URL (`/`). The OpenAPI spec is at `/swagger/v1/swagger.json`.

**Why:** Gives reviewers and developers an immediate, testable entry point when visiting the API — no need to remember endpoint paths. XML comments on controllers document parameters and response types.

---

### 14. OpenTelemetry + Aspire Dashboard

`ServiceDefaults` configures OTEL traces (ASP.NET Core + HttpClient) and metrics (runtime, requests).

| Environment | How telemetry is collected |
|-------------|---------------------------|
| **Aspire AppHost** (local) | Auto-wired to Aspire Dashboard at `https://localhost:18888` |
| **Skaffold/K8s** | `OTEL_EXPORTER_OTLP_ENDPOINT` → aspire-dashboard service |

**Why Aspire:** Zero-config local observability — reviewers can see request traces and PokeAPI call latency without setting up Jaeger manually.

---

### 15. AppHost vs Skaffold — two run modes, one API

| Mode | Command | Use case |
|------|---------|----------|
| **API only** | `dotnet run --project src/Kota.Pokedex.Api` | Fastest — in-memory cache, no Docker |
| **Aspire AppHost** | `dotnet run --project src/Kota.Pokedex.AppHost` | Local dev with Redis + Aspire Dashboard |
| **Skaffold** | `cd infra && skaffold dev` | Production-like K8s deploy with Redis + Dashboard |

- **`Kota.Pokedex.Api`** — the actual application deployed to production
- **`Kota.Pokedex.AppHost`** — local dev orchestrator only; starts Api + Redis + Dashboard. Not deployed to production.
- **`infra/`** — Skaffold deploys Api + Redis + Aspire Dashboard to Kubernetes

**Why both AppHost and Skaffold?** AppHost gives fast inner-loop dev with hot reload and no Docker build. Skaffold validates the full containerized deployment path.

---

### 16. Global exception handling

`ExceptionHandlingMiddleware` catches unhandled exceptions and returns consistent JSON error responses. `PokeApiException` maps to `502 Bad Gateway`; validation errors to `400 Bad Request`.

**Why:** Frontend receives predictable error shapes. PokeAPI failures are surfaced clearly without exposing internal stack traces in production.

---

### 17. Pagination owned by the backend

All list endpoints return `PagedResult<T>` with `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Default page size is 20 (max 100) for pokemon; 50 (max 100) for ability dropdown.

**Why:** PokeAPI pagination (`limit`/`offset`) only applies to raw list fetches. Our filtered/intersected results are paginated in-memory after filtering — the frontend never needs to understand PokeAPI pagination semantics.

---

## Configuration

`src/Kota.Pokedex.Api/appsettings.json`:

```json
{
  "Cache": { "Provider": "Memory", "DefaultTtlMinutes": 1440 },
  "ConnectionStrings": { "redis": "localhost:6379" },
  "RateLimiting": { "PermitLimit": 100, "WindowMinutes": 1 },
  "PokeApi": { "BaseUrl": "https://pokeapi.co/api/v2/", "MaxConcurrentRequests": 5 },
  "Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }
}
```

CORS is configured for Angular's default port (`4200`).

---

## What I'd Do With More Time

- Integration tests with `WireMock` for PokeAPI responses
- Response compression (`Brotli`) for large filter result sets
- Background cache refresh job instead of startup-only warmup
- Feature #5 (Favorites / Collection) on top of this search infrastructure
- Angular frontend with paginated grid and filter dropdowns
- Precomputed filter intersection indexes for hot combinations (type + generation)

---

## License

MIT (or as specified by project owner)
