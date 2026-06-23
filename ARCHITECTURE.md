# .NET Architecture Guidelines

## Project Structure

```
Kota.Pokedex/
├── src/                             # Application source code
│   ├── Kota.Pokedex.Api/            # Web API entry point
│   ├── Kota.Pokedex.Core/           # Domain logic, entities, interfaces
│   │   ├── Entities/                # Domain models
│   │   ├── Interfaces/              # Abstractions (IRepository, IReadStore)
│   │   ├── Exceptions/              # Custom exceptions
│   │   └── Constants/               # Domain constants
│   │
│   ├── Kota.Pokedex.Application/    # Use cases via CQRS
│   │   ├── Commands/                # Write operations (Create, Update, Delete)
│   │   │   └── Pokemon/
│   │   │       ├── CreatePokemonCommand.cs
│   │   │       ├── CreatePokemonCommandHandler.cs
│   │   │       └── CreatePokemonCommandValidator.cs
│   │   ├── Queries/                 # Read operations (Get, List, Search)
│   │   │   └── Pokemon/
│   │   │       ├── GetPokemonByIdQuery.cs
│   │   │       └── GetPokemonByIdQueryHandler.cs
│   │   ├── DTOs/                    # Data transfer objects
│   │   ├── Mapping/                 # AutoMapper profiles
│   │   └── Behaviors/               # MediatR pipeline (validation, logging)
│   │
│   └── Kota.Pokedex.Infrastructure/ # EF Core, external APIs, implementations
│       ├── Persistence/             # DbContext, migrations
│       ├── Repositories/            # Write-side repository implementations
│       ├── ReadStores/              # Read-optimized query implementations (optional)
│       ├── ExternalServices/        # Third-party integrations
│       └── Configuration/           # Infrastructure setup
│
├── tests/                           # Test projects
│   ├── Kota.Pokedex.Tests.Unit/     # Unit tests
│   ├── Kota.Pokedex.Tests.Integration/ # Integration tests
│   └── Kota.Pokedex.Tests.E2E/      # End-to-end tests
│
├── infra/                           # Deployment & platform scaffolding
│   ├── skaffold.yaml                # Skaffold config (build + deploy loop)
│   ├── docker/
│   │   └── Dockerfile               # Container image definition
│   └── kubernetes/
│       ├── deployment.yaml          # K8s deployment manifest
│       └── service.yaml             # K8s service manifest
│
├── ARCHITECTURE.md
├── CODING_STANDARDS.md
├── RULES.md
└── Kota.Pokedex.sln
```

### Infra Folder

Ang `infra/` ay para sa **deployment scaffolding** — hiwalay sa `src/Kota.Pokedex.Infrastructure/` (application code layer).

| Path | Purpose |
|------|---------|
| `infra/skaffold.yaml` | Skaffold pipeline — build image, deploy sa K8s, port-forward |
| `infra/docker/Dockerfile` | Multi-stage Docker build para sa API |
| `infra/kubernetes/` | Kubernetes manifests (Deployment, Service, etc.) |

**Local dev with Skaffold** (from `infra/` directory):

```bash
cd infra
skaffold dev
```

## Layered Architecture with CQRS

**CQRS (Command Query Responsibility Segregation)** — hiwalay ang write path (Commands) at read path (Queries). Ang bawat use case ay isang maliit, focused handler na dispatched via **MediatR**.

```
┌─────────────────────────────────────────────────────────┐
│  API Layer                                              │
│  Controller → IMediator.Send(command | query)           │
└──────────────────────────┬──────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
┌─────────────────────┐         ┌─────────────────────┐
│  Commands (Write)   │         │  Queries (Read)     │
│  CreatePokemon      │         │  GetPokemonById     │
│  UpdatePokemon      │         │  GetAllPokemon      │
│  DeletePokemon      │         │  SearchPokemon      │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           ▼                               ▼
┌─────────────────────┐         ┌─────────────────────┐
│  IRepository        │         │  IRepository /      │
│  (write model)      │         │  IReadStore         │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           └───────────────┬───────────────┘
                           ▼
                    [Database / External API]
```

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

### 4. API (Presentation) Layer
**Responsibility**: HTTP endpoints, request/response handling

- Controllers dispatch Commands/Queries via `IMediator`
- Request/response models
- Middleware
- Filters

```csharp
// Kota.Pokedex.Api/Controllers/PokemonController.cs
[ApiController]
[Route("api/[controller]")]
public class PokemonController : ControllerBase {
    private readonly IMediator _mediator;

    public PokemonController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<ActionResult<PokemonDto>> GetPokemon(int id) {
        var pokemon = await _mediator.Send(new GetPokemonByIdQuery(id));
        return pokemon is null ? NotFound() : Ok(pokemon);
    }

    [HttpPost]
    public async Task<ActionResult<PokemonDto>> CreatePokemon(CreatePokemonCommand command) {
        var pokemon = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPokemon), new { id = pokemon.Id }, pokemon);
    }
}
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

### Query (Read)
```
GET /api/pokemon/{id}
   ↓
[Controller] → _mediator.Send(new GetPokemonByIdQuery(id))
   ↓
[ValidationBehavior] → skip (queries typically have no validators)
   ↓
[GetPokemonByIdQueryHandler] → calls repository
   ↓
[Repository] → SELECT from database
   ↓
[Mapper] → entity → PokemonDto
   ↓
[Controller] → 200 OK
```

### Command (Write)
```
POST /api/pokemon
   ↓
[Controller] → _mediator.Send(new CreatePokemonCommand(...))
   ↓
[ValidationBehavior] → FluentValidation rules
   ↓
[CreatePokemonCommandHandler] → business logic + repository.AddAsync
   ↓
[Repository] → INSERT into database
   ↓
[Mapper] → entity → PokemonDto
   ↓
[Controller] → 201 Created
```
