---
applyTo:
  - "src/SD.Project/**"
  - "!src/SD.Project.Application/**"
  - "!src/SD.Project.Domain/**"
  - "!src/SD.Project.Infrastructure/**"
---

# UI Layer Instructions

This layer hosts the ASP.NET Core Razor Pages application.

## Key Components
- **Pages** (`Pages/`): Razor Pages with code-behind files
- **ViewModels** (`ViewModels/`): Presentation models for views
- **Filters** (`Filters/`): ASP.NET Core filters
- **Program.cs**: Application entry point and DI configuration

## Patterns
- Inject application services (e.g., `ProductService`) into page models
- Map DTOs to ViewModels for display
- Keep pages thin; delegate logic to application services
- Static assets go in `wwwroot/`

## Configuration
- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides
- DI is wired in Program.cs via `AddApplication()` (from `SD.Project.Application/DependencyInjection.cs`) and `AddInfrastructure()` (from `SD.Project.Infrastructure/DependencyInjection.cs`)

## Constraints
- Never reference EF Core or domain repositories directly
- Keep Razor markup presentation-only
- Prefer localization-ready strings
