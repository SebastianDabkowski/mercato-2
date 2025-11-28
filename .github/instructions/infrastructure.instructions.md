---
applyTo:
  - "src/SD.Project.Infrastructure/**"
---

# Infrastructure Layer Instructions

This layer provides implementations for interfaces defined in Domain and Application layers.

## Key Components
- **Persistence** (`Persistence/`): EF Core `AppDbContext` and configurations
- **Repositories** (`Repositories/`): Repository implementations using EF Core
- **Services** (`Services/`): Infrastructure service implementations

## Patterns
- `AppDbContext` auto-loads `IEntityTypeConfiguration` types from the assembly
- Place entity configurations in `Persistence/Configurations/`
- Repository methods return domain entities; mapping to DTOs happens in Application layer
- Use `AsNoTracking()` for read-only queries

## Configuration
- Currently uses `UseInMemoryDatabase("AppDb")` for development
- Replace with SQL provider when `ConnectionStrings:DefaultConnection` is configured
- Register all implementations in `DependencyInjection.cs`

## Constraints
- Implement interfaces from Domain/Application layers
- Keep EF Core concerns isolated from other layers
- Use `SaveChangesAsync` for all write operations
