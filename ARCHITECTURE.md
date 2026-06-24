# .NET Architecture Guidelines

## Full-Stack Overview

The Pokedex is a **full-stack application** with a separate Angular SPA frontend and a .NET backend API. The frontend is the presentation layer; the API is the backend entry point only. All PokeAPI calls go through the backend (caching, rate limiting, data transformation).

```
┌──────────────────────────────────────────────────────────────────┐
│  Browser                                                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  web/ (Angular + TypeScript)                               │  │
│  │  Pages → Components → services → HttpClient (API)          │  │
│  └────────────────────────────┬───────────────────────────────┘  │
└───────────────────────────────┼──────────────────────────────────┘
                                │ HTTP (REST / JSON)
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│  Kota.Pokedex.Api                                                │
│  Controllers → IMediator → Commands / Queries                    │
└───────────────────────────────┬──────────────────────────────────┘
                                │
          ┌─────────────────────┼─────────────────────┐
          ▼                     ▼                     ▼
   [SQLite / EF Core]    [PokeAPI Client]      [Redis / Memory Cache]
   Collection data       External data         Shared / local cache
```

**Key rule:** The frontend **must not** call PokeAPI directly. Only the backend accesses the external API.

## Project Structure

```
Kota.Pokedex/
├── src/
│   ├── web/                         # Frontend SPA (Angular)
│   │   ├── src/app/
│   │   │   ├── core/                # Services, models, constants, utils
│   │   │   ├── shared/              # Reusable UI components
│   │   │   └── features/            # Feature areas (pokedex, collection, auth)
│   │   ├── environments/
│   │   ├── angular.json
│   │   ├── package.json
│   │   └── proxy.conf.json          # Dev proxy → API (avoids CORS locally)
│   │
│   ├── Kota.Pokedex.Api/            # Backend REST API
│   │   ├── Controllers/             # HTTP endpoints (Pokemon, Bootstrap, Ready, …)
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   ├── Kota.Pokedex.Core/           # Interfaces, models, constants, options
│   ├── Kota.Pokedex.Application/    # CQRS commands, queries, DTOs
│   ├── Kota.Pokedex.Infrastructure/ # PokeAPI client, cache, prefetch, EF Core
│   ├── Kota.Pokedex.ServiceDefaults/ # OpenTelemetry, health checks, JSON logging
│   └── Kota.Pokedex.AppHost/        # Aspire local orchestration (not deployed)
│
├── tests/
│   ├── Kota.Pokedex.Tests.Unit/
│   └── Kota.Pokedex.Tests.Integration/
│
├── infra/
│   ├── skaffold.yaml
│   ├── docker/
│   │   ├── Dockerfile               # Unified multi-target (api + web)
│   │   └── docker-bake.hcl
│   └── kubernetes/
│
├── README.md
├── ARCHITECTURE.md
├── CODING_STANDARDS.md
├── RULES.md
└── Kota.Pokedex.sln                 # .NET projects; web/ managed by npm
```

### Layer Responsibilities

| Layer | Location | Responsibility |
|-------|----------|----------------|
| **Frontend** | `src/web/` | UI, user interaction, calls backend API only |
| **API** | `src/Kota.Pokedex.Api/` | HTTP endpoints, CORS, auth boundary, dispatches CQRS |
| **Application** | `src/Kota.Pokedex.Application/` | Use cases — Commands & Queries |
| **Core** | `src/Kota.Pokedex.Core/` | Interfaces, domain models, constants, options |
| **Infrastructure** | `src/Kota.Pokedex.Infrastructure/` | EF Core, PokeAPI client, cache, prefetch hosted service |
| **Infra (deploy)** | `infra/` | Docker, Skaffold, Kubernetes manifests |

### Infra Folder

The `infra/` folder is **deployment scaffolding** — separate from `src/Kota.Pokedex.Infrastructure/` (application code layer).

| Path | Purpose |
|------|---------|
| `infra/skaffold.yaml` | Skaffold pipeline — build API + web, deploy to K8s |
| `infra/docker/Dockerfile` | Multi-stage build (`api` and `web` targets) |
| `infra/kubernetes/` | K8s manifests for API, web, Redis, Aspire Dashboard |

**Local dev (hot reload):**

```bash
# Terminal 1 — Backend
dotnet run --project src/Kota.Pokedex.Api

# Terminal 2 — Frontend
cd src/web && npm start
```

**Local dev with Skaffold (full stack):**

```bash
cd infra && skaffold dev
```

## Bootstrap, Prefetch & Pagination

Two separate mechanisms: **(A) startup prefetch** on the API, and **(B) on-demand page loading** in the browser. There is **no background prefetch of pages 2+**.

### A. Startup prefetch (API warmup)

| Component | File |
|-----------|------|
| Orchestrator | `Infrastructure/Services/PokemonPrefetchHostedService.cs` |
| Pokémon index + generation map | `PokemonIndexService.WarmupAsync()` |
| Filter metadata | `FilterMetadataService.WarmupAsync()` |
| First 24 card details | `PokemonIndexService.PrefetchFirstPageCardDetailsAsync()` |
| Readiness state | `Core/Interfaces/IWarmupState.cs`, `Infrastructure/Health/WarmupHealthCheck.cs` |
| HTTP ready probe | `Api/Controllers/ReadyController.cs` → `GET /api/ready` |
| K8s readiness | `infra/kubernetes/api-deployment.yaml` → `/health/ready` |

```
ApplicationStarted
  → warmup index + filters + prefetch 24 cards into cache
  → IWarmupState.MarkComplete()
  → /health/ready and /api/ready return 200
```

### B. Browser initial load & pagination

| Step | Endpoint | Purpose |
|------|----------|---------|
| 1 | `GET /api/ready` | Poll until warmup complete |
| 2 | `GET /api/bootstrap` | Filter metadata + `pokemonTotalCount` (no card items) |
| 3 | `GET /api/pokemon?page=N` | All pages — fixed **24 items** per page (`PokemonPagination.CatalogPageSize`) |

| Frontend | File |
|----------|------|
| Ready + bootstrap | `web/.../bootstrap-api.service.ts` |
| Initial load | `PokedexPageComponent.loadInitial()` |
| Next / filter | `PokedexPageComponent.setupPageLoader()` → `pokemon-api.service.ts` |
| Fixed page size constant | `Core/Constants/PokemonPagination.cs`, `web/.../pokemon-pagination.constants.ts` |
| Catalog count (bootstrap) | `GetPokemonCatalogCountQueryHandler.cs` |
| Paginated search | `SearchPokemonQueryHandler.cs` (ignores client `pageSize`) |

**Why keep `BootstrapDto`?** One request for types, generations, abilities page 1, and total catalog count instead of four separate filter calls plus a count query. Pokémon cards always come from `/api/pokemon`.

## Backend Architecture with CQRS

**CQRS (Command Query Responsibility Segregation)** separates write paths (Commands) from read paths (Queries). Each use case is a small, focused handler dispatched via **MediatR**.

```
┌─────────────────────────────────────────────────────────┐
│  Frontend (web/)                                        │
│  HttpClient → /api/pokemon, /api/bootstrap, …         │
└──────────────────────────┬────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────┐
│  API Layer (Kota.Pokedex.Api)                           │
│  Controller → IMediator.Send(command | query)           │
└──────────────────────────┬────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
┌─────────────────────┐         ┌─────────────────────┐
│  Commands (Write)   │         │  Queries (Read)     │
│  MarkFavorite       │         │  SearchPokemon      │
│  MarkCaught         │         │  GetBootstrapQuery  │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           ▼                               ▼
┌─────────────────────┐         ┌─────────────────────┐
│  IRepository        │         │  IPokeApiClient +   │
│  (SQLite)           │         │  ICacheService      │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           └───────────────┬───────────────┘
                           ▼
              [SQLite]          [PokeAPI + Cache]
```

### 0. Frontend Layer

**Responsibility:** Presentation, user interaction, consuming backend API

- Angular + TypeScript SPA in `src/web/`
- API calls via `HttpClient` services in `core/services/` — not scattered in components
- TypeScript interfaces in `core/models/` — mirror backend DTOs
- `proxy.conf.json` in dev proxies `/api` to the backend (avoids CORS during `npm start`)

**Frontend rules:**
- ❌ No direct PokeAPI calls from the browser
- ✅ Loading and error states on data-fetching pages
- ✅ Models/interfaces aligned with Application DTOs
- ✅ Fixed catalog page size (24) — server is source of truth for pagination

### 1. Core Layer

**Responsibility:** Business rules, interfaces, constants — no framework dependencies

- Interfaces defining contracts (`IPokemonIndexService`, `ICacheService`, …)
- Domain models and options
- Constants (`PokemonPagination.CatalogPageSize = 24`)

### 2. Application Layer (CQRS)

**Responsibility:** Use cases as Commands and Queries

- **Commands** — mutate state (favorites, caught, auth)
- **Queries** — read-only, no side effects
- **Handlers** — business logic for one use case
- **DTOs** — API contracts consumed by the frontend

### 3. Infrastructure Layer

**Responsibility:** External concerns (PokeAPI, cache, EF Core, hosted services)

- `PokeApiClient` — typed HTTP client with throttling
- `ICacheService` — Memory or Redis
- `PokemonPrefetchHostedService` — startup warmup
- `AppDbContext` — SQLite collection persistence

### 4. API Layer (Backend)

**Responsibility:** HTTP boundary between frontend and backend — **not** the presentation layer

- Controllers dispatch Commands/Queries via `IMediator`
- CORS for frontend origin (`http://localhost:4200`)
- Global exception handling → consistent JSON errors
- No business logic in controllers — dispatch only

## CQRS Conventions

### Naming

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Action}{Entity}Command` | `MarkFavoriteCommand` |
| Command Handler | `{Action}{Entity}CommandHandler` | `MarkFavoriteCommandHandler` |
| Query | `{Action}{Entity}Query` | `SearchPokemonQuery` |
| Query Handler | `{Action}{Entity}QueryHandler` | `SearchPokemonQueryHandler` |

### Rules

- **Commands** — return a result or `Unit`; may throw domain exceptions
- **Queries** — read-only; must not mutate data
- **One handler per command/query**
- **Use `record`** for command/query definitions
- **Folder per feature** — `Commands/Collection/`, `Queries/Pokemon/`

## Data Flow

### Initial Pokedex load (full stack)

```
Browser polls GET /api/ready (200 when warmup done)
   ↓
GET /api/bootstrap → types, generations, abilities, pokemonTotalCount
   ↓
GET /api/pokemon?page=1 → 24 hydrated cards
   ↓
[PokedexPageComponent] → renders grid; totalPages = ceil(totalCount / 24)
```

### Next page (on demand — not prefetched)

```
User clicks Next
   ↓
GET /api/pokemon?page=2
   ↓
[SearchPokemonQueryHandler] → skip/take + hydrate card details (cache-aside)
   ↓
[PokedexPageComponent] → replaces grid items; catalogTotalCount unchanged
```

### Mark Favorite (Write)

```
User clicks ♥ (web/)
   ↓
POST /api/collection/... (JWT)
   ↓
[CollectionController] → _mediator.Send(MarkFavoriteCommand)
   ↓
[Handler] → EF Core → SQLite
   ↓
[Controller] → 201 Created
```

## Error Handling

- Custom exceptions in `Core` (`PokeApiException`, etc.)
- `ExceptionHandlingMiddleware` maps errors to consistent JSON
- `PokeApiException` → `502 Bad Gateway`
- Validation failures → `400 Bad Request`
- No stack traces in production responses

## Dependency Injection

- Handlers registered via MediatR assembly scan
- Infrastructure services registered in `Infrastructure/DependencyInjection.cs`
- `PokemonPrefetchHostedService` registered as `IHostedService`
- Service lifetimes: Singleton (cache, warmup state), Scoped (handlers, DbContext)

## Related Documentation

- [README.md](README.md) — run instructions, API reference, design decisions
- [CODING_STANDARDS.md](CODING_STANDARDS.md) — naming, logging, testing style
- [RULES.md](RULES.md) — layer rules, security, pagination constraints
