---
name: csharp-conventions
description: General C# and .NET coding conventions, patterns, and best practices. Use when implementing, reviewing, or planning C# code. Covers naming, architecture, patterns, and common practices.
---

# C# / .NET Conventions

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase | `MyApp.Services` |
| Class/Struct | PascalCase | `OrderService` |
| Interface | I + PascalCase | `IOrderRepository` |
| Method | PascalCase | `CalculateTotal()` |
| Property | PascalCase | `FirstName` |
| Public Field | PascalCase | `MaxRetries` |
| Private Field | _camelCase | `_orderRepository` |
| Parameter | camelCase | `orderId` |
| Local Variable | camelCase | `totalAmount` |
| Constant | PascalCase | `DefaultTimeout` |
| Enum | PascalCase (singular) | `OrderStatus` |
| Async Method | Suffix Async | `GetOrderAsync()` |

## File Organization

- One type per file (class, interface, enum)
- File name matches type name
- Organize by feature/domain, not by type (prefer `Features/Orders/` over `Services/`)

## Common Patterns

### Dependency Injection

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

### Async/Await

```csharp
// DO: async all the way
public async Task<Order> GetOrderAsync(int id, CancellationToken ct = default)
{
    return await _repository.GetByIdAsync(id, ct);
}

// DON'T: block on async
public Order GetOrder(int id)
{
    return _repository.GetByIdAsync(id).Result; // DEADLOCK RISK
}
```

### Null Handling

```csharp
// Prefer nullable reference types
public string? GetDisplayName(User? user)
{
    return user?.DisplayName ?? "Unknown";
}

// Guard clauses at boundaries
public void Process(Order order)
{
    ArgumentNullException.ThrowIfNull(order);
    // ...
}
```

### Result Pattern (for operations that can fail)

```csharp
public record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

## Error Handling

- Throw on programmer errors (argument validation)
- Use Result pattern for expected failures (business logic)
- Catch specific exceptions, never bare `catch`
- Log with structured logging: `_logger.LogError(ex, "Failed to process order {OrderId}", orderId)`

## Performance Guidelines

- Prioritize speed over memory usage — latency is the #1 priority
- Use `ValueTask<T>` for hot paths that often complete synchronously
- Use `Span<T>` and `Memory<T>` for buffer operations
- Pool objects in high-throughput scenarios (`ObjectPool<T>`)
- Prefer `StringBuilder` for string concatenation in loops
- Use `ConfigureAwait(false)` in library code

## Project Structure

```
src/
├── <Project>/            # Main project
├── <Project>.Tests/      # Test project
└── <Solution>.sln
```
