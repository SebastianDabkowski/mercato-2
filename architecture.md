# Architecture

This solution illustrates a layered approach that keeps the UI (Razor Pages), application orchestration, domain logic, and infrastructure concerns isolated while still being easy to run locally.

```
Browser ➜ SD.Project (UI) ➜ SD.Project.Application ➜ SD.Project.Domain
                                  ▲
                                  │
                            SD.Project.Infrastructure
```

## Solution Pieces
| Layer | Assembly | Responsibilities | Depends On |
| --- | --- | --- | --- |
| Presentation | `src/SD.Project` | Hosts ASP.NET Core Razor Pages, wiring of middleware, Razor `Pages/*`, and ViewModels. | Application, Infrastructure |
| Application | `src/SD.Project.Application` | Use-case orchestration via `ProductService`, command/query contracts, DTOs, cross-layer interfaces such as `INotificationService`. | Domain |
| Domain | `src/SD.Project.Domain` | Enterprise logic, aggregates (`Product`), value objects (`Money`), repository contracts. | — |
| Infrastructure | `src/SD.Project.Infrastructure` | EF Core `AppDbContext`, repository implementation, notification adapter, DI helpers. | Application, Domain |

## Dependency Injection Flow
- `SD.Project/Program.cs` registers `AddApplication()` and `AddInfrastructure(Configuration)` before enabling Razor Pages.
- `SD.Project.Application/DependencyInjection.cs` currently wires `ProductService` (scoped) and is the place to expose additional use cases.
- `SD.Project.Infrastructure/DependencyInjection.cs` configures `AppDbContext`, `ProductRepository`, and `NotificationService`. Replace the `UseInMemoryDatabase` call with the provider required by your environment when `ConnectionStrings:DefaultConnection` becomes meaningful.

## Request Lifecycle
1. `Pages/Index.cshtml.cs` receives a GET request and resolves `ProductService` from DI.
2. The page issues `GetAllProductsQuery`, which is handled inside `ProductService`.
3. `ProductService` delegates reads to `IProductRepository`, implemented by `ProductRepository` (EF Core) against `AppDbContext`.
4. Results are mapped to DTOs and then to `ProductViewModel` for display.
5. Future POST flows (e.g., create product) would send `CreateProductCommand` through the same service, which persists aggregates and triggers `INotificationService`.

## Persistence
- `AppDbContext` exposes `DbSet<Product>` and automatically loads future `IEntityTypeConfiguration` classes in `Persistence/Configurations`.
- `ProductRepository` encapsulates EF Core operations (`GetAllAsync`, `AddAsync`, `SaveChangesAsync`), keeping the application layer unaware of EF-specific APIs.
- Default provider is `UseInMemoryDatabase("AppDb")`. Swap for SQL Server, PostgreSQL, etc., once `appsettings.json` contains a real connection string.

## Notifications
- `NotificationService` currently logs creation events via `ILogger`. Because it implements `INotificationService`, you can replace it with email, queue, or webhook integrations without touching the application layer.

## Extensibility Notes
- **Database Provider**: Update `DependencyInjection.cs` in Infrastructure with the desired provider and ensure migrations live alongside `AppDbContext`.
- **Configurations Folder**: `Persistence/Configurations` is empty to encourage adding explicit EF mappings once the domain grows.
- **Additional Use Cases**: Add new commands/queries/services under `SD.Project.Application`, register them in `AddApplication`, and keep Razor Pages thin by consuming those services.
- **Testing**: Introduce unit tests against Domain/Application, and integration tests that spin up `AppDbContext` using the provider of choice.
