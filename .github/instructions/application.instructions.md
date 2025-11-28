---
applyTo:
  - "src/SD.Project.Application/**"
---

# Application Layer Instructions

This layer orchestrates use cases and coordinates between domain and infrastructure.

## Key Components
- **Commands** (`Commands/`): Write operations that modify state
- **Queries** (`Queries/`): Read operations that return data
- **DTOs** (`DTOs/`): Data transfer objects for external communication
- **Services** (`Services/`): Application services like `ProductService`
- **Interfaces** (`Interfaces/`): Contracts for infrastructure services

## Patterns
- Commands and queries are simple record types
- `ProductService` handles both commands and queries via `HandleAsync` overloads
- DTOs are the contract between Application and UI layers
- Register all services in `DependencyInjection.cs`

## Constraints
- Depend only on Domain layer; define interfaces here that Infrastructure implements
- Never reference Infrastructure implementations directly
- Keep services thin; delegate business logic to domain entities
- Use interfaces for infrastructure concerns (e.g., `INotificationService`)
