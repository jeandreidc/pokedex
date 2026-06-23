# Kota Pokedex

A full-stack Pokedex application built for a take-home exercise. This repository implements **Feature #2: Search & Filter with Pagination** and **Feature #5: Favorites / Collection Tracker** — a backend API that proxies and normalizes [PokeAPI](https://pokeapi.co/docs/v2) data, persists per-user collections in SQLite, and an **Angular** frontend where you browse, filter, and “shop” for Pokémon to add to your collection.

---

## Features Implemented

### #2 — Search & Filter with Pagination

- Combinable filters: text search, type, ability, and generation
- Server-side filter intersection and pagination
- Filter metadata endpoints for Angular dropdowns
- All PokeAPI access goes through the backend (no direct browser calls)

<img width="1222" height="1219" alt="image" src="https://github.com/user-attachments/assets/1c02fc77-1200-4472-aaac-e18d48ad092a" />


### #5 — Favorites / Collection Tracker

- Per-user favorites and caught status (JWT auth, SQLite persistence)
- Collection sidebar (favorites, caught, generation stats)
- Optimistic UI on Pokémon cards (heart + pokeball)
- Secured CRUD API for collection entries

<img width="476" height="342" alt="image" src="https://github.com/user-attachments/assets/b0283a68-58e2-4201-b83d-001a91bb653a" />

<img width="477" height="727" alt="image" src="https://github.com/user-attachments/assets/a6f7b1d0-f503-4bcb-8d2b-92cea935de95" />



---

## Design Philosophy & Personal Choices

The features, UI, and infrastructure choices below were picked to make this sample **more usable**, **more “Pokédex”**, and closer to something I’d actually want to demo — not just a text-only API exercise.

### Product & UX

- **Usability first** — filters, sprites on cards, and a collection sidebar so it feels like you are building a personal Pokédex, not scrolling a plain list.
- **Visual, familiar workflow** — the browse → pick → review flow mimics **online shopping** (e.g. Amazon): search the catalog, add Pokémon with heart / caught actions on the card, then open the **slide-out collection** to see what you gathered.
- **Pagination vs infinite scroll** — pagination could be polished further (e.g. infinite scroll). For this scope, **explicit pages are enough**: predictable API load, clear “page X of Y”, and simpler to test. With more time, infinite scroll would be a nice UX upgrade.

### Tech stack

- **C# / .NET + Angular** — chosen because these are where I am **most familiar**, which let me focus on product and architecture instead of fighting the stack.
- **Observability (traces, structured logs, metrics)** — added as nice-to-haves, mostly **force of habit** from day-to-day work, and to confirm I could wire **OpenTelemetry → Aspire Dashboard** end-to-end in this repo (including custom counters for favorites, caught, register, and login).
- **Skaffold** — one command (`skaffold dev`) runs **API, web, Redis, and Aspire Dashboard** together in Kubernetes, so reviewers and I don’t have to start each piece manually during demos.
- **GitHub Actions CI** — basic workflow on push/PR: `dotnet restore`, `build` (Release), unit tests, and integration tests (see [.github/workflows/ci.yml](.github/workflows/ci.yml)).

### Backend architecture

- **RESTful API (not GraphQL)** — I settled on REST because it is what I am **most familiar with**; it has been a long time since I used GraphQL in production. GraphQL has real advantages (e.g. **flattening nested data** in one request), but for this project I tried to get similar benefits on the REST side: **normalized DTOs** (`PokemonSummary` with types, abilities, generation on one card payload), **combinable filter query params**, and **CQRS handlers** that assemble the right shape per endpoint without over-fetching on the frontend.
- **Clean Architecture + CQRS (MediatR)** — structured the codebase so reads (queries) and writes (commands) stay separated, controllers stay thin, and infrastructure (PokeAPI, cache, SQLite) can be swapped without touching handlers.
- **Event sourcing** — considered it, but for a take-home Pokédex with simple favorites/caught state, **traditional CRUD + EF Core** is enough; event sourcing would add complexity without a clear payoff here.
- **Public vs secured API** — I could still polish a **harder split** between public browse endpoints (`/api/pokemon`, `/api/filters`) and secured collection/auth (`/api/collection`, `/api/auth`) — e.g. separate policy modules, API versioning, or gateway routes. **Time ran out**; for this project, **JWT on collection + `[Authorize]`** works and demonstrates the security model, so I settled here.

---

## How to Run

### Prerequisites

- [Docker](https://www.docker.com/) with Kubernetes enabled (e.g. Docker Desktop)
- [Skaffold](https://skaffold.dev/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running components individually (see below)
- [Node.js](https://nodejs.org/) — for local frontend dev only

### Skaffold (recommended — full stack)

Builds and deploys **API**, **Angular web**, **Redis**, and **Aspire Dashboard** to a local Kubernetes cluster from a single unified Dockerfile (`infra/docker/Dockerfile`). This is the primary way to run the app — Redis and all supporting services are provisioned automatically.

```bash
cd infra
skaffold dev
```

Skaffold builds both container images (`api` and `web` targets) with BuildKit, applies the manifests in `infra/kubernetes/`, and sets up port forwards automatically.

Open **http://localhost:4200** — the web container serves the Angular app and proxies `/api/` to the API service inside the cluster.

Port forwards:

| Service           | Local Port | Notes                                      |
|-------------------|------------|--------------------------------------------|
| Web (nginx)       | 4200       | Angular UI; `/api` proxied to API service  |
| API               | 8080       | Direct API access (optional)               |
| Redis             | 6379       | Shared cache (`Cache:Provider = Redis`)    |
| Aspire Dashboard  | 18888      | Traces, metrics, logs                      |

**Manual Docker build** (without Skaffold), from the repo root:

```bash
docker buildx bake -f infra/docker/docker-bake.hcl
```

This produces `kota-pokedex-api:latest` and `kota-pokedex-web:latest` in one command.

### Run individually

Use these when you want hot reload or to run a single component without the full K8s stack. **Note:** API-only modes use in-memory cache unless you run Redis separately (Aspire AppHost does this for you).

#### Aspire AppHost (API + Redis + dashboard, no K8s)

```bash
dotnet run --project src/Kota.Pokedex.AppHost
```

- **API**: URL shown in the Aspire dashboard (typically `https://localhost:7xxx`)
- **Aspire Dashboard**: `https://localhost:18888` (traces, metrics, logs)
- **Redis**: provisioned automatically; cache provider set to `Redis`

#### API only (in-memory cache)

```bash
dotnet run --project src/Kota.Pokedex.Api
```

Uses in-memory cache (`Cache:Provider = Memory` in `appsettings.json`). Good for quick API testing without Docker.

#### Angular frontend (local dev with API)

Terminal 1 — API:

```bash
dotnet run --project src/Kota.Pokedex.Api
```

Terminal 2 — Frontend (proxies `/api` → `http://localhost:5164`):

```bash
cd src/web
npm start
```

Open **http://localhost:4200** — search/filter UI with Pokémon sprite cards.

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
    { "id": 25, "name": "pikachu", "spriteUrl": "...", "types": ["electric"], "abilities": ["Static"], "generation": "I" }
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

### 0. Feature choices: #2 + #5 (and why not #1)

The take-home exercise listed several optional features. I implemented **#2** first, then **#5**, because they complement each other in the product UX:

| Feature | Role in the app |
|---------|-----------------|
| **#2 — Search & Filter** | Find Pokémon quickly (browse / discovery) |
| **#5 — Favorites / Collection** | Save what you care about (persisted per user) |

**Why they work better together:** #2 alone is read-only browsing; #5 alone has little value without a way to discover Pokémon. Combined, the flow feels like **shopping on Amazon** — you search and filter the catalog, then add items to your “cart” (favorites / caught) with one tap on the card. The collection sidebar slides in from the right so you can review what you picked without leaving the browse experience.

**Feature #1 (team comparison):** I would have liked to include this — it’s a strong demo of analytics and domain modeling — but I did not have time to design a fair way to compare teams (stats, typings, coverage, etc.) in a useful UI. **#5 was a better fit for the remaining scope** because it showcases full **CRUD**, auth, persistence, and CQRS commands alongside the existing read-heavy #2 stack.

**#5 over #1 for this pass:** secured write endpoints, EF Core + migrations, JWT, and collection stats by generation are concrete backend skills to demonstrate; team comparison remains a good follow-up once the comparison model is defined.

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
| **Skaffold** (primary) | `cd infra && skaffold dev` | Full stack on K8s — API + web + Redis + Dashboard |
| **Aspire AppHost** | `dotnet run --project src/Kota.Pokedex.AppHost` | Local dev with Redis + Aspire Dashboard, no container build |
| **API only** | `dotnet run --project src/Kota.Pokedex.Api` | Fastest — in-memory cache, no Docker |

- **`Kota.Pokedex.Api`** — the actual application deployed to production
- **`Kota.Pokedex.AppHost`** — local dev orchestrator only; starts Api + Redis + Dashboard. Not deployed to production.
- **`infra/`** — Skaffold deploys API, web, Redis, and Aspire Dashboard to Kubernetes

**Why Skaffold first?** It mirrors production — containers, Redis, and the full stack in one command. AppHost and API-only modes are for faster inner-loop dev when you don't need the K8s path.

---

### 16. Global exception handling

`ExceptionHandlingMiddleware` catches unhandled exceptions and returns consistent JSON error responses. `PokeApiException` maps to `502 Bad Gateway`; validation errors to `400 Bad Request`.

**Why:** Frontend receives predictable error shapes. PokeAPI failures are surfaced clearly without exposing internal stack traces in production.

---

### 17. Pagination owned by the backend

All list endpoints return `PagedResult<T>` with `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Default page size is 20 (max 100) for pokemon; 50 (max 100) for ability dropdown.

**Why:** PokeAPI pagination (`limit`/`offset`) only applies to raw list fetches. Our filtered/intersected results are paginated in-memory after filtering — the frontend never needs to understand PokeAPI pagination semantics.

---

### 18. Structured JSON logging (`ILogger<T>`)

All backend logging uses **`ILogger<T>`** injected via DI with **message templates** — never string interpolation in log calls. Properties in `{CurlyBraces}` become queryable fields in the log output.

Console output is **JSON** via `AddJsonConsole()` in `Kota.Pokedex.ServiceDefaults` (one JSON object per line). When `OTEL_EXPORTER_OTLP_ENDPOINT` is set, the same logs are also exported to the Aspire Dashboard alongside traces and metrics.

Example log call:

```csharp
_logger.LogInformation("Pokemon index warmup complete with {EntryCount} entries", index.Count);
```

Example JSON line written to stdout:

```json
{
  "Timestamp": "2026-06-23T12:00:00.0000000Z",
  "LogLevel": "Information",
  "Category": "Kota.Pokedex.Infrastructure.Services.PokemonIndexService",
  "Message": "Pokemon index warmup complete with 1025 entries",
  "EntryCount": 1025
}
```

**Why JSON:** Machine-parseable logs for local dev, containers, and K8s (`kubectl logs`) without adding Serilog or another logging framework. **Why message templates:** Preserves structured fields instead of embedding values in the message string, so log aggregators and Aspire can filter on `{Path}`, `{EntryCount}`, `{Method}`, etc.

**Rules:**
- ✅ `_logger.LogDebug("PokeAPI GET {Path}", path)`
- ❌ `_logger.LogDebug($"PokeAPI GET {path}")`
- ✅ Pass exceptions as the first argument: `_logger.LogError(ex, "...")`

---

### 19. Angular card UI with `spriteUrl` images

The frontend (`src/web/`) is an Angular standalone SPA that calls the backend API only. Each result is a **Pokémon card** — not a text-only list:

| Element | Source |
|---------|--------|
| Circular sprite | `spriteUrl` from the API (prefetched index sprites) |
| Name + `#id` | `name`, `id` |
| Type badges | `types[]` with type-colored pills |
| Ability + Generation | **Abilities** label + capsules; generation as *GEN I* (italic grey) under name/id |

**Layout:** Horizontal card — round image on the left, attributes on the right. The grid uses `repeat(auto-fill, minmax(260px, 1fr))` and a `ResizeObserver` computes `pageSize` from the visible grid area so each page fills the viewport with as many cards as fit (6–100).

**Filters:** Types and generations load once from `/api/filters/*`. Abilities are paginated on the API (~367 total, max 100 per page) but the frontend **fetches all pages** on load and on search so the dropdown is never limited to A–C only.

**Why `spriteUrl` not official artwork:** Sprites are already on every index entry — no extra PokeAPI detail call per card. Faster first paint and simpler backend; artwork can be added later via detail hydration if needed.

**Why show ability + generation on cards:** Each Pokémon’s own attributes — not the active search filters — so users see full context at a glance without applying filters first.

**Card hydration:** `GetPokemonCardDetailsAsync` fetches `pokemon/{id}` for types + abilities and resolves generation via a cached id→generation map built from all generation species lists.

---

## Configuration

`src/Kota.Pokedex.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": { "TimestampFormat": "O", "UseUtcTimestamp": true, "IncludeScopes": true }
    }
  },
  "Cache": { "Provider": "Memory", "DefaultTtlMinutes": 1440 },
  "ConnectionStrings": { "redis": "localhost:6379" },
  "RateLimiting": { "PermitLimit": 100, "WindowMinutes": 1 },
  "PokeApi": { "BaseUrl": "https://pokeapi.co/api/v2/", "MaxConcurrentRequests": 5 },
  "Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }
}
```

CORS is configured for Angular's default port (`4200`).

---

## Scalability

### Is the current solution scalable?

**Partially.** The app is designed so the **read-heavy browse path** (#2) can scale horizontally today; the **write path** (#5 collection + auth) is intentionally pinned to a **single API replica** because of SQLite.

| Component | Scales today? | Notes |
|-----------|---------------|--------|
| **Angular web (nginx)** | Yes | Static assets; scale `kota-pokedex-web` replicas independently |
| **API — browse / filters / PokeAPI proxy** | Yes* | Stateless handlers; **Redis** (`Cache:Provider = Redis` in K8s) shares PokeAPI index and filter metadata across pods |
| **API — JWT auth** | Yes | No server-side session; any pod can validate tokens |
| **API — collection CRUD** | No (multi-pod) | **SQLite** on a **ReadWriteOnce** PVC — one writer, one mount; `api-deployment` runs **1 replica** |
| **Redis** | Single instance | Enough for dev/demo; production would use Redis Sentinel/Cluster for HA |

\*Only after replacing SQLite for collection data (see below). With the current SQLite setup, **do not** scale API pods beyond 1.

### What to do when scaling out (more pods)

Typical production path:

1. **Replace SQLite with a shared database** (SQL Server, PostgreSQL, etc.) — required before running multiple API replicas. All pods then share one collection/auth store with proper concurrent writes.
2. **Increase API `replicas`** in `infra/kubernetes/api-deployment.yaml` (and add a **HorizontalPodAutoscaler** on CPU/memory or request rate).
3. **Keep Redis** as the shared cache for PokeAPI responses and warmed indexes so new pods do not each cold-call PokeAPI.
4. **Scale the web tier** separately — nginx replicas behind a Service/Ingress; `/api` still proxies to the API Service.
5. **Optional hardening** — distributed rate limiting (Redis-backed) if per-pod `RateLimiter` partitions are too coarse at high replica counts; Redis HA; managed DB with backups.

For this take-home, **SQLite + 1 API pod + Redis** is a deliberate trade-off: simple local Skaffold setup, full CRUD demo, and a clear upgrade path when traffic or availability requirements grow.

---

## Future Improvements

- **SQL Server (or PostgreSQL) instead of SQLite** — prerequisite for **multiple API pods**; also better ops tooling and concurrent collection writes at scale. For this take-home (single replica, local Skaffold), SQLite remains sufficient.
- Feature **#1 — team comparison** once a clear comparison model exists (e.g. type coverage, stat totals, role tags)
- **Clearer public vs secured API boundary** — e.g. dedicated auth policies, route groups, or gateway split; current JWT + `[Authorize]` on collection is sufficient for the demo
- Expand integration tests with `WireMock` for richer PokeAPI scenarios beyond current fakes
- Response compression (`Brotli`) for large filter result sets
- Background cache refresh job instead of startup-only warmup
- Official artwork hydration (`ArtworkUrl`) as optional upgrade over sprites
- Precomputed filter intersection indexes for hot combinations (type + generation)

---

## License

MIT (or as specified by project owner)
