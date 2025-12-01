# Mercato 2 – Copilot Instructions

## Technology Stack
- .NET SDK 9.0
- ASP.NET Core Razor Pages
- Entity Framework Core (InMemory provider for local dev)
- C# 13

## Architecture Snapshot
- Clean layers: `src/SD.Project` (UI Razor Pages host) ➜ `SD.Project.Application` ➜ `SD.Project.Domain`; infrastructure types live in `SD.Project.Infrastructure` but are only referenced by the UI host (see `architecture.md`).
- Always route new use cases through `ProductService` or a sibling application service registered in `SD.Project.Application/DependencyInjection.cs`; Razor Pages should keep only orchestration/mapping logic.
- Domain types (`Domain/Entities`, `ValueObjects`) must stay persistence-agnostic; interact with storage exclusively through interfaces in `Domain/Repositories`.
- Infrastructure supplies implementations (`Persistence/AppDbContext`, `Repositories/ProductRepository`, `Services/NotificationService`) and is wired via `SD.Project.Infrastructure/DependencyInjection.cs`.

## Build & Run
- From the repo root run `cd src; dotnet restore SD.Project.sln` before building.
- Launch the site with `dotnet run --project SD.Project/SD.Project.csproj`; it hosts Razor Pages on https://localhost:5001 with EF Core InMemory storage that resets on restart.
- No automated tests exist yet; add new test projects beside the layer they cover to keep dependency flow consistent.

## Application Layer Patterns
- Commands/queries are simple records in `Application/Commands` and `Application/Queries`; `ProductService.HandleAsync` is overloaded to accept each one, so prefer unique method names or distinct service classes if overloads become ambiguous.
- DTOs (`Application/DTOs/ProductDto.cs`) are the contract between Application ↔ UI; Razor Pages materialize `ProductViewModel` instances from these DTOs (`src/SD.Project/ViewModels`).
- `INotificationService` is the seam for side effects; implementations should remain async and cancellation-friendly because services pass through the page token.

## Persistence & Infrastructure
- `AppDbContext` auto-loads `IEntityTypeConfiguration` types from the assembly, so place new mappings under `Infrastructure/Persistence/Configurations` to keep the context clean.
- `AddInfrastructure` currently forces `UseInMemoryDatabase("AppDb")`; when adding SQL providers, replace that call and honor `ConnectionStrings:DefaultConnection` from `src/SD.Project/appsettings.json`.
- `ProductRepository.GetAllAsync` returns a read-only list built from `AsNoTracking()`; keep repository methods returning domain entities and push projection/mapping higher up.
- `NotificationService` just logs via `ILogger`; swap in real channels without touching Application code by registering a different `INotificationService` implementation.

## UI Layer Notes
- `Pages/Index.cshtml.cs` resolves `ProductService` directly and maps DTOs to `ProductViewModel`; follow this pattern for new pages—inject services, call `HandleAsync`, map for display.
- Razor markup lives under `src/SD.Project/Pages`; keep them presentation-only (no EF/constants) and prefer localization-ready strings.
- Static assets live in `wwwroot`; the pipeline uses `app.MapStaticAssets()` so reference files relative to that root.

## Extending the System
- New features should add domain behavior first, then expose it via repositories/services, finally surface it through Razor Pages.
- Register every new service in the corresponding `DependencyInjection` helper; missing registrations are the most common runtime issue because the host only calls these extension methods.
- When persisting data, wrap writes in repository calls and finish with `SaveChangesAsync` so the InMemory provider behaves the same as relational providers later.

## Boundaries
- Never commit secrets, connection strings, or credentials to the repository.
- Do not modify files in `.github/agents/` as they contain private agent instructions.
- Keep EF Core references confined to the Infrastructure layer.
- Avoid adding new NuGet packages without explicit justification.

## Acceptance Criteria
- All new code must build without errors or warnings.
- New services must be registered in the appropriate `DependencyInjection.cs`.
- Follow existing code style and naming conventions.
- Razor Pages should remain thin with logic delegated to application services.
