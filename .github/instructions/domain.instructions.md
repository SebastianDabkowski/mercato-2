---
applyTo:
  - "src/SD.Project.Domain/**"
---

# Domain Layer Instructions

This layer contains the core business logic and is persistence-agnostic.

## Key Principles
- Entities in `Entities/` encapsulate business rules and state
- Value objects in `ValueObjects/` are immutable and equality-compared by value
- Repository interfaces in `Repositories/` define data access contracts
- Domain services in `Services/` contain logic that doesn't belong to a single entity

## Constraints
- **No infrastructure dependencies**: Do not reference EF Core, ASP.NET, or any external frameworks
- **No DTOs**: Domain types should not be designed for serialization
- **Rich domain model**: Prefer encapsulating behavior within entities over anemic models

## Patterns
- Use factory methods for complex entity creation
- Validate invariants in constructors and property setters
- Define repository interfaces here; implementations go in Infrastructure
