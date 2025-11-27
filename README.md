# Mercato 2

Product catalog sample that demonstrates a clean separation between web, application, domain, and infrastructure layers using ASP.NET Core Razor Pages and Entity Framework Core.

## Prerequisites
- [.NET SDK 9.0](https://dotnet.microsoft.com/) or later
- PowerShell 5.1+ (default on Windows) or any shell compatible with the .NET CLI

## Quick Start
```powershell
# Restore solution dependencies
cd src
 dotnet restore SD.Project.sln

# Run the Razor Pages host (listens on https://localhost:5001 by default)
dotnet run --project SD.Project/SD.Project.csproj
```
The default configuration hosts the web UI with an in-memory database, so restarting the app clears previously added data.

## Solution Layout
| Project | Path | Purpose |
| --- | --- | --- |
| SD.Project | `src/SD.Project` | ASP.NET Core Razor Pages front-end (entry point, pages, ViewModels). |
| SD.Project.Application | `src/SD.Project.Application` | Application layer: commands, queries, DTOs, `ProductService` orchestration, abstractions. |
| SD.Project.Domain | `src/SD.Project.Domain` | Core domain model (entities, value objects, repository contracts). |
| SD.Project.Infrastructure | `src/SD.Project.Infrastructure` | EF Core persistence (`AppDbContext`, `ProductRepository`) and notification adapters. |

Dependency flow is strictly top-down: UI ➜ Application ➜ Domain, while Infrastructure implements Application/Domain abstractions and is referenced by the UI host only.

## Key Features
- Razor Pages dashboard (`Pages/Index.cshtml`) that lists products resolved through the application layer.
- Application service (`ProductService`) capable of handling `CreateProductCommand` and `GetAllProductsQuery` requests.
- EF Core InMemory persistence via `AppDbContext` with repository abstraction (`IProductRepository`).
- Notification stub (`NotificationService`) that logs product creation events until a real channel is plugged in.

## Configuration
`src/SD.Project/appsettings.json` contains base logging settings and a placeholder `ConnectionStrings:DefaultConnection`. The infrastructure layer currently forces `UseInMemoryDatabase`. Replace the provider logic inside `SD.Project.Infrastructure/DependencyInjection.cs` when connecting to a real database.

## Architecture Reference
See [`architecture.md`](architecture.md) for a deeper look at the layering decisions, dependency registration, and data flow.

## Next Steps
- Replace the InMemory provider with SQL Server, PostgreSQL, or another relational database.
- Flesh out entity configurations in `SD.Project.Infrastructure/Persistence/Configurations`.
- Add integration tests that cover `ProductService` end-to-end.
