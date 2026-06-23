# .NET Architecture Guidelines

## Full-Stack Overview

Ang Pokedex ay **full-stack application** — hiwalay ang frontend SPA at .NET backend API. Ang frontend ay presentation layer; ang API ay backend entry point lang. Lahat ng PokeAPI calls ay dadaan sa backend (caching, rate limiting, data transformation).

```
┌──────────────────────────────────────────────────────────────────┐
│  Browser                                                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  web/ (Angular + TypeScript)                               │  │
│  │  Pages → Components → services → HttpClient (API)        │  │
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
   [SQLite / EF Core]    [PokeAPI Client]      [Memory Cache]
   Collection data       External data         Rate-limit shield
```

**Key rule:** Ang frontend ay **hindi** tumatawag directly sa PokeAPI. Tanging ang backend ang may access sa external API.

## Project Structure

```
Kota.Pokedex/
├── src/
│   ├── web/                         # Frontend SPA (Angular)
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── core/            # Services, interceptors, API client
│   │   │   │   ├── shared/          # Reusable UI components
│   │   │   │   └── features/        # Feature modules (e.g. pokedex/)
│   │   │   ├── environments/        # environment.ts (API URL per env)
│   │   │   └── main.ts
│   │   ├── angular.json
│   │   ├── package.json
│   │   └── proxy.conf.json          # Dev proxy → API (avoids CORS locally)
│   │
│   ├── Kota.Pokedex.Api/            # Backend REST API
│   │   ├── Controllers/             # HTTP endpoints
│   │   ├── Middleware/              # Exception handling, CORS
│   │   └── Program.cs               # DI and startup
│   │
│   ├── Kota.Pokedex.Core/           # Domain logic, entities, interfaces
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   ├── Exceptions/
│   │   └── Constants/
│   │
│   ├── Kota.Pokedex.Application/    # Use cases via CQRS
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── DTOs/                    # API contracts (mirrored sa frontend types)
│   │   ├── Mapping/
│   │   └── Behaviors/
│   │
│   └── Kota.Pokedex.Infrastructure/ # EF Core, PokeAPI client, caching
│       ├── Persistence/
│       ├── Repositories/
│       ├── ExternalServices/        # PokeAPI integration
│       └── Configuration/
│
├── tests/
│   ├── Kota.Pokedex.Tests.Unit/
│   ├── Kota.Pokedex.Tests.Integration/
│   └── Kota.Pokedex.Tests.E2E/      # Playwright vs full stack
│
├── infra/
│   ├── skaffold.yaml
│   ├── docker/
│   │   ├── Dockerfile.api           # Backend container
│   │   └── Dockerfile.web           # Frontend container (nginx + static build)
│   └── kubernetes/
│       ├── api-deployment.yaml
│       ├── api-service.yaml
│       ├── web-deployment.yaml
│       └── web-service.yaml
│
├── ARCHITECTURE.md
├── CODING_STANDARDS.md
├── RULES.md
└── Kota.Pokedex.sln                 # .NET projects only; web/ managed by npm
```

### Layer Responsibilities

| Layer | Location | Responsibility |
|-------|----------|----------------|
| **Frontend** | `src/web/` | UI, user interaction, calls backend API only |
| **API** | `src/Kota.Pokedex.Api/` | HTTP endpoints, CORS, auth boundary, dispatches CQRS |
| **Application** | `src/Kota.Pokedex.Application/` | Use cases — Commands & Queries |
| **Core** | `src/Kota.Pokedex.Core/` | Domain entities, interfaces, business rules |
| **Infrastructure** | `src/Kota.Pokedex.Infrastructure/` | EF Core, PokeAPI client, cache, repos |
| **Infra (deploy)** | `infra/` | Docker, Skaffold, Kubernetes manifests |

### Infra Folder

Ang `infra/` ay para sa **deployment scaffolding** — hiwalay sa `src/Kota.Pokedex.Infrastructure/` (application code layer).

| Path | Purpose |
|------|---------|
| `infra/skaffold.yaml` | Skaffold pipeline — build API + web, deploy sa K8s |
| `infra/docker/Dockerfile.api` | Multi-stage Docker build para sa API |
| `infra/docker/Dockerfile.web` | Angular build + nginx para sa frontend |
| `infra/kubernetes/` | K8s manifests para sa API at web services |

**Local dev** (recommended — hot reload para sa both):

```bash
# Terminal 1 — Backend
dotnet run --project src/Kota.Pokedex.Api

# Terminal 2 — Frontend
cd src/web && ng serve
```

**Local dev with Skaffold** (from `infra/` directory):

```bash
cd infra
skaffold dev
```

## Backend Architecture with CQRS

**CQRS (Command Query Responsibility Segregation)** — hiwalay ang write path (Commands) at read path (Queries). Ang bawat use case ay isang maliit, focused handler na dispatched via **MediatR**.

```
┌─────────────────────────────────────────────────────────┐
│  Frontend (web/)                                        │
│  HttpClient → /api/pokemon                              │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────┐
│  API Layer (Kota.Pokedex.Api)                           │
│  Controller → IMediator.Send(command | query)           │
└──────────────────────────┬──────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
┌─────────────────────┐         ┌─────────────────────┐
│  Commands (Write)   │         │  Queries (Read)     │
│  MarkFavorite       │         │  SearchPokemon      │
│  MarkCaught         │         │  GetCollectionStats │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           ▼                               ▼
┌─────────────────────┐         ┌─────────────────────┐
│  IRepository        │         │  IPokeApiClient /   │
│  (SQLite)           │         │  IReadStore + Cache │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           └───────────────┬───────────────┘
                           ▼
              [SQLite]          [PokeAPI]
```

### 0. Frontend Layer
**Responsibility**: Presentation, user interaction, consuming backend API

- Angular + TypeScript SPA sa `src/web/`
- API calls via `HttpClient` services sa `core/services/` — hindi scattered calls sa components
- TypeScript interfaces sa `core/models/` — mirror ng backend DTOs
- `proxy.conf.json` sa dev para iwas CORS issues locally (`ng serve` → proxies `/api` to backend)

```typescript
// src/web/src/app/core/services/pokemon.service.ts
@Injectable({ providedIn: 'root' })
export class PokemonService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  searchPokemon(params: SearchParams): Observable<PagedResult<PokemonSummary>> {
    return this.http.get<PagedResult<PokemonSummary>>(`${this.baseUrl}/pokemon`, {
      params: { ...params } as Record<string, string>
    });
  }
}
```

```json
// src/web/proxy.conf.json — dev proxy
{
  "/api": {
    "target": "http://localhost:5164",
    "secure": false
  }
}
```

```bash
# Run with proxy
ng serve --proxy-config proxy.conf.json
```

**Frontend rules:**
- ❌ Walang direct PokeAPI calls mula sa browser
- ✅ Loading at error states sa bawat page
- ✅ Models/interfaces aligned sa Application DTOs

### 1. Core (Domain) Layer
**Responsibility**: Business rules and domain logic

- Entity models without framework dependencies
- Interfaces defining contracts
- Custom exceptions
- Domain value objects

```csharp
// Kota.Pokedex.Core/Entities/Pokemon.cs
namespace Kota.Pokedex.Core.Entities;

public class Pokemon {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int BaseHp { get; set; }
}

// Kota.Pokedex.Core/Interfaces/IPokemonRepository.cs
public interface IPokemonRepository {
    Task<Pokemon?> GetByIdAsync(int id);
    Task<List<Pokemon>> GetAllAsync();
    Task AddAsync(Pokemon pokemon);
    Task UpdateAsync(Pokemon pokemon);
    Task DeleteAsync(int id);
}
```

### 2. Application Layer (CQRS)
**Responsibility**: Use cases as Commands and Queries

- **Commands** — nagbabago ng state (Create, Update, Delete)
- **Queries** — read-only, walang side effects
- **Handlers** — naglalaman ng business logic para sa isang use case
- **Validators** — FluentValidation rules para sa commands
- **Behaviors** — cross-cutting concerns sa MediatR pipeline

#### Query Example

```csharp
// Kota.Pokedex.Application/Queries/Pokemon/GetPokemonByIdQuery.cs
public record GetPokemonByIdQuery(int Id) : IRequest<PokemonDto?>;

// Kota.Pokedex.Application/Queries/Pokemon/GetPokemonByIdQueryHandler.cs
public class GetPokemonByIdQueryHandler : IRequestHandler<GetPokemonByIdQuery, PokemonDto?> {
    private readonly IPokemonRepository _repository;
    private readonly IMapper _mapper;

    public GetPokemonByIdQueryHandler(IPokemonRepository repository, IMapper mapper) {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PokemonDto?> Handle(GetPokemonByIdQuery request, CancellationToken cancellationToken) {
        var pokemon = await _repository.GetByIdAsync(request.Id);
        return pokemon is null ? null : _mapper.Map<PokemonDto>(pokemon);
    }
}
```

#### Command Example

```csharp
// Kota.Pokedex.Application/Commands/Pokemon/CreatePokemonCommand.cs
public record CreatePokemonCommand(string Name, string Type, int BaseHp) : IRequest<PokemonDto>;

// Kota.Pokedex.Application/Commands/Pokemon/CreatePokemonCommandHandler.cs
public class CreatePokemonCommandHandler : IRequestHandler<CreatePokemonCommand, PokemonDto> {
    private readonly IPokemonRepository _repository;
    private readonly IMapper _mapper;

    public CreatePokemonCommandHandler(IPokemonRepository repository, IMapper mapper) {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PokemonDto> Handle(CreatePokemonCommand request, CancellationToken cancellationToken) {
        var pokemon = new Pokemon {
            Name = request.Name,
            Type = request.Type,
            BaseHp = request.BaseHp
        };

        await _repository.AddAsync(pokemon);
        return _mapper.Map<PokemonDto>(pokemon);
    }
}

// Kota.Pokedex.Application/Commands/Pokemon/CreatePokemonCommandValidator.cs
public class CreatePokemonCommandValidator : AbstractValidator<CreatePokemonCommand> {
    public CreatePokemonCommandValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.BaseHp).GreaterThan(0);
    }
}
```

#### Validation Behavior (Pipeline)

```csharp
// Kota.Pokedex.Application/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull {
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        if (!_validators.Any()) {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0) {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### 3. Infrastructure Layer
**Responsibility**: External concerns (databases, APIs, file systems)

- Entity Framework DbContext
- Repository implementations (write side)
- Read store implementations (read side, kung kailangan ng optimized queries)
- External service integrations
- Dependency injection configuration

```csharp
// Kota.Pokedex.Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext : DbContext {
    public DbSet<Pokemon> Pokemon { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Pokemon>()
            .HasKey(p => p.Id);
    }
}

// Kota.Pokedex.Infrastructure/Repositories/PokemonRepository.cs
public class PokemonRepository : IPokemonRepository {
    private readonly AppDbContext _context;

    public PokemonRepository(AppDbContext context) => _context = context;

    public async Task<Pokemon?> GetByIdAsync(int id) {
        return await _context.Pokemon.FirstOrDefaultAsync(p => p.Id == id);
    }
}
```

### 4. API Layer (Backend)
**Responsibility**: HTTP boundary between frontend at backend — **hindi** presentation layer

- Controllers dispatch Commands/Queries via `IMediator`
- CORS policy para sa frontend origin
- Global exception handling → consistent JSON error responses
- Walang business logic sa controllers — dispatch lang

```csharp
// Kota.Pokedex.Api/Controllers/PokemonController.cs
[ApiController]
[Route("api/[controller]")]
public class PokemonController : ControllerBase {
    private readonly IMediator _mediator;

    public PokemonController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PokemonSummaryDto>>> Search(
        [FromQuery] SearchPokemonQuery query) {
        return Ok(await _mediator.Send(query));
    }
}
```

```csharp
// Kota.Pokedex.Api/Program.cs — CORS for frontend
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration["Frontend:Url"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();
app.UseCors("Frontend");
app.MapControllers();
```

## Dependency Injection

Register services in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// MediatR — auto-registers all handlers from Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePokemonCommandHandler).Assembly));

// FluentValidation — auto-registers all validators
builder.Services.AddValidatorsFromAssembly(typeof(CreatePokemonCommandValidator).Assembly);

// MediatR pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Infrastructure
builder.Services.AddScoped<IPokemonRepository, PokemonRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();
app.Run();
```

**Service Lifetimes**:
- `AddSingleton`: Single instance for application lifetime
- `AddScoped`: New instance per HTTP request
- `AddTransient`: New instance every time

**NuGet Packages**:
- `MediatR` — command/query dispatch
- `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` — validation
- `AutoMapper.Extensions.Microsoft.DependencyInjection` — mapping

## CQRS Conventions

### Naming

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Action}{Entity}Command` | `CreatePokemonCommand` |
| Command Handler | `{Action}{Entity}CommandHandler` | `CreatePokemonCommandHandler` |
| Command Validator | `{Action}{Entity}CommandValidator` | `CreatePokemonCommandValidator` |
| Query | `{Action}{Entity}Query` | `GetPokemonByIdQuery` |
| Query Handler | `{Action}{Entity}QueryHandler` | `GetPokemonByIdQueryHandler` |

### Rules

- **Commands** — nagbabalik ng result o `Unit`; pwedeng mag-throw ng domain exceptions
- **Queries** — read-only, hindi dapat mag-mutate ng data
- **One handler per command/query** — isang class, isang responsibility
- **Use `record`** para sa command/query definitions — immutable by default
- **Folder per feature** — `Commands/Pokemon/`, `Queries/Pokemon/`

### When to Split Read/Write Stores

Para sa simpleng CRUD (tulad ng Pokedex), shared repository ay sapat na. Mag-introduce ng hiwalay na `IReadStore` kapag:

- Kailangan ng denormalized read models
- May complex reporting queries na iba sa write model
- May performance bottleneck sa read side

## Design Patterns

### Repository Pattern
Abstracts data access logic (write side):

```csharp
public interface IRepository<T> where T : class {
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

### Mediator Pattern (via MediatR)
Decouples controllers from handlers:

```csharp
// Controller knows nothing about handlers — only sends messages
await _mediator.Send(new GetPokemonByIdQuery(id));
await _mediator.Send(new CreatePokemonCommand(name, type, baseHp));
```

### Specification Pattern
Complex queries sa read side:

```csharp
public abstract class Specification<T> {
    public IQueryable<T> Apply(IQueryable<T> query) {
        query = Criteria(query);
        return Includes.Aggregate(query, (current, include) => include(current));
    }

    protected abstract IQueryable<T> Criteria(IQueryable<T> query);
    protected List<Func<IQueryable<T>, IQueryable<T>>> Includes { get; } = [];
}
```

### Unit of Work Pattern
Manages multiple repositories sa command handlers:

```csharp
public interface IUnitOfWork {
    IPokemonRepository Pokemons { get; }
    ITrainerRepository Trainers { get; }
    Task<int> SaveChangesAsync();
}
```

## Error Handling

### Custom Exceptions
```csharp
// Kota.Pokedex.Core/Exceptions/
public class EntityNotFoundException : Exception {
    public EntityNotFoundException(string message) : base(message) { }
}

public class InvalidOperationException : Exception {
    public InvalidOperationException(string message) : base(message) { }
}
```

### Global Exception Handler Middleware
```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();

public class ExceptionHandlingMiddleware {
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        } catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception) {
        context.Response.ContentType = "application/json";
        var response = new { error = exception.Message };
        return context.Response.WriteAsJsonAsync(response);
    }
}
```

## Data Flow

### Search (Read — full stack)
```
User types sa search bar (web/)
   ↓
[SearchPage] → searchPokemon({ search, type, page })
   ↓
GET /api/pokemon?search=pika&type=fire&page=1
   ↓
[PokemonController] → _mediator.Send(SearchPokemonQuery)
   ↓
[SearchPokemonQueryHandler] → IPokeApiClient + cache
   ↓
[PokeAPI] → raw data → normalized PagedResult<PokemonSummaryDto>
   ↓
[Controller] → 200 OK JSON
   ↓
[SearchPage] → renders paginated grid
```

### Mark Favorite (Write — full stack)
```
User clicks ♥ (web/)
   ↓
[CollectionPage] → markFavorite({ pokemonId })
   ↓
POST /api/collection/favorites
   ↓
[CollectionController] → _mediator.Send(MarkFavoriteCommand)
   ↓
[ValidationBehavior] → FluentValidation
   ↓
[MarkFavoriteCommandHandler] → repository.AddAsync
   ↓
[SQLite] → INSERT
   ↓
[Controller] → 201 Created
   ↓
[CollectionPage] → updates UI state
```

### Backend-only: Query (Read)
```
GET /api/pokemon/{id}
   ↓
[Controller] → _mediator.Send(new GetPokemonByIdQuery(id))
   ↓
[ValidationBehavior] → skip (queries typically have no validators)
   ↓
[GetPokemonByIdQueryHandler] → calls repository / PokeAPI client
   ↓
[Mapper] → entity → PokemonDto
   ↓
[Controller] → 200 OK
```

### Backend-only: Command (Write)
```
POST /api/collection/favorites
   ↓
[Controller] → _mediator.Send(new MarkFavoriteCommand(...))
   ↓
[ValidationBehavior] → FluentValidation rules
   ↓
[MarkFavoriteCommandHandler] → business logic + repository.AddAsync
   ↓
[Repository] → INSERT into SQLite
   ↓
[Controller] → 201 Created
```
