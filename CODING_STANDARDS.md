# .NET Coding Standards

## Naming Conventions

### Classes and Interfaces
- **PascalCase**: `public class UserService`, `public interface IRepository`
- **Abstract classes**: prefix with `Abstract`: `public abstract class AbstractEntity`
- **Interfaces**: prefix with `I`: `public interface IUserRepository`

### Methods and Properties
- **PascalCase**: `public void ProcessData()`, `public string UserName { get; set; }`
- **Private methods**: `private void ValidateInput()`

### Variables and Parameters
- **camelCase**: `string userName`, `int recordCount`
- **Constants**: `UPPER_SNAKE_CASE`: `private const string DATABASE_CONNECTION_STRING = "..."`
- **Private fields**: `_camelCase`: `private string _username`

### Async Methods
- **Suffix with Async**: `public async Task<User> GetUserAsync()`, `public async Task SaveAsync()`

## Code Style

### Indentation and Formatting
- Use **4 spaces** for indentation (no tabs)
- **One statement per line**
- **Opening braces on same line** (Allman style):
  ```csharp
  if (condition) {
      // code
  } else {
      // code
  }
  ```

### Access Modifiers
- Always explicitly declare access modifiers
- Order: `public`, `protected`, `private`, `internal`
- Prefer **access restriction** (private by default, public only when needed)

### Method Organization
Order class members:
1. Constants and static fields
2. Public properties
3. Private fields
4. Constructors
5. Public methods
6. Private methods

### Nullable Reference Types
- Enable nullable reference types in project file
- Use `string?` for nullable strings, `User?` for nullable objects
- Use `!` operator only when certain value is not null

```csharp
public string? GetOptionalValue() => null;
public string GetRequiredValue() => "value";
```

## Comments and Documentation

### XML Documentation
- Document public APIs with `///` comments
- Include `<summary>`, `<param>`, `<returns>`, `<exception>` tags

```csharp
/// <summary>
/// Retrieves a user by ID from the database.
/// </summary>
/// <param name="userId">The unique identifier of the user.</param>
/// <returns>The user if found; null otherwise.</returns>
/// <exception cref="ArgumentException">Thrown when userId is invalid.</exception>
public async Task<User?> GetUserByIdAsync(int userId) { }
```

### Inline Comments
- Use `//` for complex logic only
- Avoid obvious comments: ❌ `// increment counter` for `counter++`

## SOLID Principles

### Single Responsibility
- One class = one reason to change
- Service classes handle business logic, repositories handle data access

### Open/Closed
- Open for extension, closed for modification
- Use inheritance and interfaces

### Liskov Substitution
- Derived classes must be substitutable for base classes

### Interface Segregation
- Many specific interfaces > one general-purpose interface

### Dependency Inversion
- Depend on abstractions (interfaces), not concrete implementations

## Exception Handling

```csharp
// Good
try {
    await ProcessDataAsync();
} catch (ArgumentException ex) {
    logger.LogError(ex, "Invalid argument: {Message}", ex.Message);
    throw;
} catch (Exception ex) {
    logger.LogError(ex, "Unexpected error occurred");
    throw;
}

// Bad
try { } catch { } // never swallow exceptions
```

- Use specific exception types
- Always log exceptions
- Don't catch generic `Exception` unless re-throwing

## Async/Await

```csharp
// Good
public async Task<User> GetUserAsync(int id) {
    return await _repository.GetAsync(id);
}

// Avoid
public Task<User> GetUserAsync(int id) {
    return _repository.GetAsync(id); // missing async keyword
}
```

- Always add `Async` suffix to async methods
- Use `ConfigureAwait(false)` in library code: `await task.ConfigureAwait(false)`

## Collections

```csharp
// Use LINQ
var adults = users.Where(u => u.Age >= 18).ToList();

// Initialize with collection initializers
var items = new List<string> { "one", "two", "three" };

// Use appropriate types
IEnumerable<T> // for queries
IList<T> // for collections
List<T> // concrete implementation
```

## LINQ Best Practices

```csharp
// Good: fluent, readable
var result = users
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Select(u => u.Email)
    .ToList();

// Query syntax when it improves readability
var result = from user in users
             where user.IsActive
             orderby user.Name
             select user.Email;
```

## Entity Framework

### DbContext Usage
```csharp
using (var context = new AppDbContext()) {
    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
}

// Or with dependency injection
public class UserRepository {
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context) => _context = context;
}
```

### Queries
- Use `AsNoTracking()` for read-only queries
- Use `Include()` to avoid N+1 queries
- Filter at database level, not in memory

```csharp
var users = await _context.Users
    .Include(u => u.Orders)
    .Where(u => u.IsActive)
    .AsNoTracking()
    .ToListAsync();
```

## Testing Standards

### Naming
- `[MethodName]_[Scenario]_[ExpectedResult]`
- `GetUserById_WithValidId_ReturnsUser`

### Structure (AAA Pattern)
```csharp
[Fact]
public async Task GetUserById_WithValidId_ReturnsUser() {
    // Arrange
    var userId = 1;
    var expected = new User { Id = 1, Name = "John" };

    // Act
    var result = await _service.GetUserByIdAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected.Id, result.Id);
}
```
