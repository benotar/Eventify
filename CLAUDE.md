# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Collaboration model

The user writes all code. Claude's role is **advisory only**: present options, reasoning, and trade-offs; show code examples in chat for the user to evaluate. Do **not** use Edit/Write on project source files unless the user explicitly says "edit", "write", "implement", or "apply". Reading files (Read, Grep, Glob) is always fine.

## Commands

```powershell
# Build entire solution
dotnet build

# Build a specific project
dotnet build src/BuildingBlocks/Eventify.SharedKernel/Eventify.SharedKernel.csproj

# Run tests (once test projects exist)
dotnet test
dotnet test tests/Eventify.Catalog.UnitTests/

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Add a new service project to the solution
dotnet sln Eventify.slnx add <path-to.csproj>
```

SDK version is pinned to `10.0.107` via `global.json`. All projects target `net10.0` via `Directory.Build.props`.

## Architecture overview

Full source-of-truth: `docs/ARCHITECTURE.md`.

**6 microservices + YARP gateway + React SPA:**

| Service | Architecture style | Role |
|---|---|---|
| Identity | Clean Arch | Duende IdentityServer 7 + ASP.NET Identity; OAuth2/OIDC |
| Catalog | Clean Arch | Artists, Events, Venues, Sessions, PriceTiers; gRPC for inter-service reads |
| Booking | Clean Arch | Reservations (Redis RedLock + TTL), Bookings, MassTransit Saga, SignalR |
| Payment | Clean Arch | Stripe PaymentIntent + webhook; Outbox |
| Ticket | VSA (single project) | QR-coded tickets; validation endpoint |
| Notification | VSA (single project) | MailHog/SendGrid; Outbox-driven consumers |

**Communication:**
- **REST** (external, via YARP): all SPA → service traffic
- **gRPC** (internal sync): Booking → Catalog, Ticket → Catalog
- **RabbitMQ + MassTransit 8.5** (async): integration events + Saga commands
- **SignalR** (real-time): seat map updates, Redis backplane

**Database:** one logical Postgres 17 database per service; no cross-DB queries; data sharing only via gRPC or events.

## BuildingBlocks

Two projects in `src/BuildingBlocks/`:

- **`Eventify.SharedKernel`** — Domain + Application + Infrastructure base classes consolidated:
  - Domain: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `DomainEvent` (abstract record), interfaces (`IEntity`, `IAggregateRoot`, `IAuditable`, `IClearableAggregate`, `IDomainEvent`), `DomainException`
  - Application: `ICommand`, `IQuery<T>`, `ICommandHandler`, `IQueryHandler`, `LoggingBehavior`, `ValidationBehavior`, `NotFoundException`, `ValidationException`
  - Infrastructure (pending): `BaseDbContext`, `AuditInterceptor`, `DispatchDomainEventsInterceptor`

- **`Eventify.IntegrationEvents`** — cross-service event contracts; no deps; `IntegrationEvent` abstract record (UUIDv7)

## Key design decisions

**IDs:** All entity IDs use `Guid.CreateVersion7()` (UUIDv7 — time-sortable, B-tree friendly). Aggregates use strongly-typed IDs as `readonly record struct ArtistId(Guid Value)`.

**Audit fields:** `CreatedAt`/`UpdatedAt` on `Entity<TId>` are mutated only via the internal `IAuditable` interface. Populated by `AuditInterceptor` (EF Core `ISaveChangesInterceptor`) — never override `SaveChangesAsync` per service.

**Domain events:** `AggregateRoot<TId>.RaiseDomainEvent` is protected. Clearing happens only via internal `IClearableAggregate`. Dispatch by `DispatchDomainEventsInterceptor` (after-save, via MediatR `IPublisher`).

**MediatR 12.2 pin:** `RequestHandlerDelegate<TResponse>` does not accept `CancellationToken` in 12.2. Pipeline behaviors call `next()` not `next(cancellationToken)`. If bumped to 12.5+, update `LoggingBehavior` and `ValidationBehavior`.

**Error handling:** Application handlers return `ErrorOr<TResult>` (uses `ErrorOr` library). Business errors → `Error.NotFound/Conflict/Validation/Unauthorized/Failure`. `DomainException` only for invariant bugs that should never reach Domain. No try/catch in handlers; endpoints end with `result.Match(success, errs => errs.ToProblemDetails())`. Full rationale in `docs/ARCHITECTURE.md` §8.3.

**Endpoints:** Minimal APIs via **Carter** (`ICarterModule` per aggregate under `Endpoints/`). Thin handlers: parse → `request.ToCommand()` → `sender.Send()` → `result.Match()`. **No MVC controllers anywhere.** No FastEndpoints (conflicts with MediatR philosophy).

**Mapping:** Manual static extension methods (`ToDto`/`ToDomain`/`ToIntegrationEvent`) colocated with the DTO/event. **No mapper libraries** (no AutoMapper / Mapster / Mapperly). Aggregates are small enough that boilerplate is minimal; full IDE refactoring + compile-time safety wins.

**Money:** Value Object `record Money(decimal Amount, string Currency)` in `SharedKernel`. Validates `Amount >= 0` and ISO 4217 `Currency` in constructor. EF `OwnsOne(b => b.Money)` flattens to `*_amount` + `*_currency` columns. All monetary fields use `Money`, never raw `decimal` + `string`.

**API conventions:** URL-segment versioning (`/v1/...`) via `Asp.Versioning.Http`. Offset pagination via `PagedResult<T>` envelope (`Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`); defaults `pageSize=20`, max `100`.

**Outbox pattern:** All publishing services use MassTransit Transactional Outbox with EF Core. Domain change + outbox row in one DB transaction.

**Booking Saga:** MassTransit Automatonymous StateMachine in the Booking service (orchestrator, not choreography). Full state diagram in `docs/ARCHITECTURE.md` §7.

**VSA vs Clean Architecture:** Ticket and Notification are VSA single-project services (less complexity). Identity, Catalog, Booking, Payment are 4-project Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`).

## C# code style

- **Classic constructors only** — never primary constructors on classes. Use explicit `private readonly` fields assigned in constructor body. Positional record syntax (e.g., `record struct ArtistId(Guid Value)`) is fine.
- Nullable reference types enabled (`<Nullable>enable`).
- Implicit usings enabled.
- `LangVersion` set to `latest`.
- Central package versioning via `Directory.Packages.props` — do not specify versions in individual `.csproj` files.

## Project naming & layering

- Projects: `Eventify.{ServiceName}.{Layer}` (e.g., `Eventify.Booking.Domain`).
- Namespace = folder path.
- One aggregate per folder in Domain; one handler per file in Application.
- Dependency rule: `Domain` has zero deps; `Application` → Domain only; `Infrastructure` → Domain + Application; `Api` → all three. Enforced by NetArchTest in CI.

## Testing

- xUnit + FluentAssertions + NSubstitute + Testcontainers + NetArchTest.
- Unit tests: Domain invariants + Application handlers (mocked deps).
- Integration tests: real Postgres + RabbitMQ via Testcontainers.
- Architecture tests: `Eventify.ArchitectureTests/` project enforces layering.
- Run integration tests against a real database — do not mock the database in integration tests.
