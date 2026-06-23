# .NET Project Rules

## Code Organization Rules

### 1. Layer Separation
- ✅ Core layer must NOT reference Application, Infrastructure, or API layers
- ✅ Application layer can reference Core layer only
- ✅ Infrastructure layer can reference Core and Application layers
- ✅ API layer can reference Application and Infrastructure (via DI)
- ✅ Frontend (`src/web/`) is independent — communicates with API via HTTP only
- ❌ Frontend must NOT call PokeAPI directly
- ❌ NO circular dependencies

### 2. No Framework Dependencies in Core
- ✅ Use interfaces for abstraction
- ✅ Keep entities as POCOs (Plain Old CLR Objects)
- ❌ NO Entity Framework imports in Core
- ❌ NO ASP.NET Core imports in Core

### 3. Namespace Rules
Must follow convention: `[CompanyName].[ProjectName].[LayerName].[SubModule]`

```
Kota.Pokedex.Core.Entities
Kota.Pokedex.Core.Interfaces
Kota.Pokedex.Application.Services
Kota.Pokedex.Infrastructure.Persistence
Kota.Pokedex.Api.Controllers
```

### 4. Async/Await Rules
- ✅ All I/O operations (database, HTTP, file) MUST be async
- ✅ Method suffix must be `Async` for async methods
- ✅ Always `await` async calls
- ❌ NO `.Result` or `.Wait()` (deadlock risk)
- ❌ NO `async void` except for event handlers

```csharp
// Good
public async Task<User> GetUserAsync(int id) {
    return await _repository.GetByIdAsync(id);
}

// Bad
public Task<User> GetUserAsync(int id) {
    return _repository.GetByIdAsync(id); // missing await
}

// Bad
public User GetUser(int id) {
    return _repository.GetByIdAsync(id).Result; // blocks thread
}
```

## Data Access Rules

### 5. Repository Pattern Mandatory
- ✅ All database access MUST go through repositories
- ✅ Services use repositories, never DbContext directly
- ❌ NO direct DbContext usage outside repositories

### 6. Entity Framework Rules
- ✅ Use `AsNoTracking()` for read-only queries
- ✅ Use `Include()` to avoid N+1 queries
- ✅ Use `ConfigureAwait(false)` in async calls
- ✅ Use parameterized queries (LINQ)
- ❌ NO raw SQL queries without approval
- ❌ NO lazy loading

```csharp
// Good
var users = await _context.Users
    .Include(u => u.Orders)
    .Where(u => u.IsActive)
    .AsNoTracking()
    .ToListAsync();

// Bad
var users = await _context.Users.ToListAsync(); // loads all users
foreach (var user in users) {
    var orders = user.Orders; // N+1 query
}
```

### 7. Validation Rules
- ✅ Validate input at API boundary (Controllers)
- ✅ Validate business rules in Services
- ✅ Use FluentValidation for complex rules
- ✅ Return descriptive validation errors

```csharp
// Validator
public class CreatePokemonValidator : AbstractValidator<CreatePokemonRequest> {
    public CreatePokemonValidator() {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters");
    }
}

// Controller
[HttpPost]
public async Task<ActionResult> CreatePokemon(CreatePokemonRequest request) {
    var validator = new CreatePokemonValidator();
    var result = await validator.ValidateAsync(request);
    if (!result.IsValid) {
        return BadRequest(result.Errors);
    }
    // proceed
}
```

## Error Handling Rules

### 8. Exception Handling
- ✅ Catch specific exceptions
- ✅ Log all exceptions with context
- ✅ Re-throw or transform to appropriate types
- ✅ Use custom exceptions for domain errors
- ❌ NO empty catch blocks `catch { }`
- ❌ NO `catch (Exception ex)` without logging

```csharp
try {
    await _repository.SaveAsync();
} catch (DbUpdateException ex) {
    _logger.LogError(ex, "Database error while saving");
    throw new ApplicationException("Failed to save data", ex);
} catch (Exception ex) {
    _logger.LogError(ex, "Unexpected error");
    throw;
}
```

### 9. Global Error Handling
- ✅ Use middleware for unhandled exceptions
- ✅ Log all exceptions
- ✅ Return consistent error responses
- ❌ NO stack traces in production responses

## Dependency Injection Rules

### 10. DI Configuration
- ✅ Register all dependencies in `Program.cs`
- ✅ Use constructor injection, never service locator
- ✅ Use appropriate lifetimes (Scoped for DbContext)
- ❌ NO new keyword for service instantiation

```csharp
// Good - Constructor injection
public class PokemonService {
    public PokemonService(IPokemonRepository repo) {
        _repository = repo; // stored as readonly field
    }
}

// Bad - Service locator
public class PokemonService {
    public void GetPokemon() {
        var service = ServiceLocator.Get<IPokemonRepository>(); // anti-pattern
    }
}
```

## Testing Rules

### 11. Test Coverage
- ✅ Unit tests for all Services
- ✅ Integration tests for Repositories
- ✅ End-to-end tests for critical flows
- ✅ Minimum 70% code coverage
- ❌ NO tests in production code
- ❌ NO hardcoded test data in production

### 12. Test Naming
- Naming: `[MethodName]_[Scenario]_[ExpectedResult]`
- Use AAA pattern (Arrange, Act, Assert)
- One assertion per test where possible

```csharp
[Fact]
public async Task GetPokemonById_WithValidId_ReturnsPokemon() {
    // Arrange
    var pokemonId = 1;

    // Act
    var result = await _service.GetPokemonAsync(pokemonId);

    // Assert
    Assert.NotNull(result);
}
```

## Security Rules

### 13. Input Validation
- ✅ Validate all user inputs
- ✅ Use [Required], [StringLength] attributes
- ✅ Sanitize data before storage
- ❌ NO trust user input
- ❌ NO SQL injection risks (use LINQ)

### 14. Authentication & Authorization
- ✅ Use claims-based identity
- ✅ Verify authorization in controllers or policies
- ✅ Use HTTPS for all endpoints
- ❌ NO passwords in logs
- ❌ NO sensitive data in query strings

### 15. Secrets Management
- ✅ Use User Secrets for development
- ✅ Use environment variables for deployment
- ✅ Use Azure Key Vault in production
- ❌ NO secrets in source code
- ❌ NO hardcoded connection strings

```csharp
// Good - Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Bad
var connectionString = "Server=localhost;Database=pokedex;..."; // hardcoded
```

## Code Quality Rules

### 16. Code Review Requirements
- ✅ All changes require code review before merge
- ✅ Address all review comments
- ✅ Tests must pass before merge
- ✅ No merge conflicts
- ✅ Linting must pass

### 17. Documentation Requirements
- ✅ Document public APIs with XML comments
- ✅ Update README for major changes
- ✅ Add comments for complex algorithms
- ❌ NO obvious comments (`// increment x`)
- ❌ NO commented-out code blocks

### 18. Performance Rules
- ✅ Use `AsNoTracking()` for read-only queries
- ✅ Batch operations when processing multiple records
- ✅ Use caching for frequently accessed data
- ✅ Profile before optimization
- ❌ NO N+1 queries
- ❌ NO loading entire tables into memory

## Build & Deployment Rules

### 19. Build Requirements
- ✅ Solution must build without warnings
- ✅ All tests must pass
- ✅ Code analysis must pass
- ❌ NO build warnings ignored

### 20. Version Control
- ✅ Use meaningful commit messages
- ✅ Commit frequently with logical groupings
- ✅ Push to feature branches, merge via pull requests
- ✅ Keep main branch always deployable
- ❌ NO force pushes to shared branches
- ❌ NO merge without approval

```
Good commit message:
"feat: Add Pokemon filtering by type"

Bad commit message:
"fix stuff" or "updates"
```

## File Organization Rules

### 21. File Naming
- One class per file (exceptions: small related classes)
- File name matches class name
- Folder matches namespace

```
File: Kota.Pokedex.Core/Entities/Pokemon.cs
Content: namespace Kota.Pokedex.Core.Entities;
         public class Pokemon { }
```

### 22. Class Size Limits
- Services: max 300 lines
- Controllers: max 200 lines
- If larger, break into smaller classes
- ❌ NO "God Classes"
