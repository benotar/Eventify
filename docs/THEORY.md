# Eventify — Theory & Learning Guide

> Companion to `ARCHITECTURE.md`. Where ARCHITECTURE.md says **what** we are building and **which** technologies we picked, this document explains **why** those patterns exist, **how** they work under the hood, and **what** you need to internalize to write the code yourself.
>
> Structure: by topic/pattern. Each section has: (1) the concept and the problem it solves, (2) the trade-offs, (3) how it is applied in Eventify, (4) recommended reading. Code samples are in C# 10 / .NET 10 unless noted.
>
> Read in order — later sections assume earlier ones. Skip nothing on first pass; come back as reference later.

---

## Table of Contents

**Part I — Foundations**
1. C# 12+ language features used across the project
2. .NET 10 runtime and tooling
3. EF Core 9 fundamentals
4. ASP.NET Core dependency injection
5. Minimal APIs

**Part II — Domain-Driven Design**
6. Strategic DDD: bounded contexts, ubiquitous language
7. Tactical DDD: entity, value object, aggregate, repository
8. Domain events vs integration events
9. Strongly-typed IDs and UUIDv7

**Part III — Architectural styles**
10. Clean Architecture (Onion / Hexagonal lineage)
11. Vertical Slice Architecture
12. Choosing between Clean and VSA per service

**Part IV — CQRS and mediation**
13. CQRS principles and pitfalls
14. MediatR — request/handler/notification
15. Pipeline behaviors
16. FluentValidation

**Part V — Error handling**
17. Two-tier error model: results vs exceptions
18. The ErrorOr library
19. RFC 7807 ProblemDetails
20. Global exception middleware

**Part VI — Microservices patterns**
21. Database-per-service
22. Saga (orchestration vs choreography)
23. Transactional Outbox
24. Inbox / idempotent consumers
25. Distributed locking with RedLock
26. Eventual consistency

**Part VII — Inter-service communication**
27. REST conventions
28. gRPC and Protocol Buffers
29. RabbitMQ and AMQP 0-9-1
30. MassTransit
31. SignalR with Redis backplane
32. YARP API Gateway

**Part VIII — Authentication & authorization**
33. OAuth 2.0 flows
34. OpenID Connect
35. JWT structure and validation
36. Duende IdentityServer 7
37. ASP.NET Core Identity
38. Policy-based authorization

**Part IX — Payment integration**
39. Stripe domain model
40. PaymentIntent flow
41. Webhook signing and idempotency

**Part X — EF Core advanced**
42. ISaveChangesInterceptor
43. Value converters and strongly-typed IDs
44. Owned entities (Money)
45. Migrations strategy
46. Concurrency control

**Part XI — API design**
47. URL-segment versioning
48. Pagination strategies
49. The Idempotency-Key header

**Part XII — Endpoint composition & mapping**
50. Carter modules
51. Manual mapping vs mapper libraries

**Part XIII — Observability**
52. Structured logging with Serilog
53. OpenTelemetry — traces, metrics, logs
54. Health checks

**Part XIV — Resilience**
55. Polly v8 and Microsoft.Extensions.Http.Resilience

**Part XV — Testing**
56. xUnit, FluentAssertions, Moq
57. Testcontainers
58. NetArchTest

**Part XVI — DevOps**
59. Docker fundamentals
60. Docker Compose
61. Kubernetes essentials
62. GitHub Actions

**Part XVII — Frontend**
63. React + Vite + TypeScript
64. TanStack Router and Query
65. Zustand
66. shadcn/ui + Tailwind
67. React Hook Form + Zod
68. oidc-client-ts
69. Stripe Elements
70. @microsoft/signalr

**Appendix A** — Recommended books and resources, ordered by topic

---

# Part I — Foundations

## 1. C# 12+ language features used across the project

Eventify pins `LangVersion=latest` (effectively C# 13 on .NET 10). You don't need every feature, but the following appear constantly and must be second nature.

### 1.1 Records

```csharp
public sealed record Address(string Street, string City, string Country);
```

A `record` is a reference type with value-based equality auto-generated from the declared positional parameters or properties. Two records with the same field values are `Equals`-equal regardless of reference identity. This is exactly what a Value Object needs.

`record struct` is the same idea but as a value type. Eventify uses `readonly record struct` for strongly-typed IDs:

```csharp
public readonly record struct ArtistId(Guid Value);
```

Because it is a `struct`, no heap allocation; because it is `readonly`, the `Value` field is immutable; because it is `record`, equality is structural.

Pitfalls:
- Records still have reference identity if used as `record class` — but `Equals` ignores it. Don't use records when identity matters (entities). Use them for value objects, DTOs, commands, events, query results.
- Inheritance between records works, but is rarely needed. Prefer composition.
- `with` expression creates a shallow copy: `var newArtist = artist with { Name = "X" };` — collection references are shared.

### 1.2 Init-only setters and required members

```csharp
public sealed class ArtistDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Bio { get; init; }
}
```

- `init` setters can only be assigned during object initialization (constructor or object initializer). After that the property is read-only. This lets you use object-initializer syntax while keeping immutability.
- `required` forces the compiler to check that the member is initialized in any constructor call or object initializer. Use it on DTOs to avoid silent default values.

### 1.3 Nullable reference types

```xml
<Nullable>enable</Nullable>
```

This is on globally. The compiler treats reference types as non-nullable unless you mark them `?`. You will see two annotations everywhere:

```csharp
public string Name { get; private set; }        // non-null; if uninitialized, compiler warns
public string? Bio { get; private set; }        // can be null
public string Title { get; private set; } = ""; // initialize to avoid warning

string? Find(int id);                            // returns null if not found
[NotNullWhen(true)] out User? user;              // post-conditions
user!.Name                                       // null-forgiving operator: "trust me"
```

Pitfalls:
- The annotation is a *contract for callers*, not a runtime guarantee. JSON deserialization can produce `null` in a non-nullable property.
- Avoid `!` (null-forgiving) — it bypasses the warning without fixing the cause. Use it only at well-defined trust boundaries.

### 1.4 Pattern matching

```csharp
return result switch
{
    { IsSuccess: true } ok => Results.Ok(ok.Value),
    { IsError: true, Errors: var errs } => errs.ToProblemDetails(),
    _ => Results.StatusCode(500)
};

if (e is DomainException { Code: var code }) { /* ... */ }
```

You will see this in endpoint handlers, in MediatR pipeline behaviors, and in saga state transitions. Property patterns, type patterns, and `switch` expressions are core.

### 1.5 Primary constructors (rejected in this project)

C# 12 allows `class Foo(Dep d)`. **Eventify rejects this style** (see CLAUDE.md and `feedback_classic_constructors`). Use classic constructors with `private readonly` fields:

```csharp
public sealed class ReservationService
{
    private readonly IReservationRepository _repo;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(IReservationRepository repo, ILogger<ReservationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }
}
```

Why: primary-constructor parameters are mutable in scope across the class, which leaks responsibility (any method can reassign them), and they don't show up in the field list — which obscures DI dependencies in a code review. Positional `record` syntax is fine because records are immutable by design.

### 1.6 Async / await

The whole project is async end-to-end. A few invariants you must internalize:

- Every async call should be awaited or explicitly fire-and-forget (rare). A forgotten `await` returns a `Task` that is silently dropped — exceptions vanish.
- Always pass `CancellationToken` from the outermost caller (HTTP request → handler → repository → EF Core). Cancellation is how ASP.NET Core stops work when the client disconnects.
- Don't call `.Result` or `.Wait()` — they deadlock under specific synchronization contexts and waste a thread.
- `ConfigureAwait(false)` is unnecessary in ASP.NET Core (no sync context). Leave it out.
- `ValueTask<T>` is only worth it on hot paths that often complete synchronously. Default to `Task<T>`.

### 1.7 File-scoped namespaces and global usings

```csharp
namespace Eventify.Catalog.Domain.Artists;
// no curly brace; whole file is in this namespace

public sealed class Artist { /* ... */ }
```

Combined with implicit usings (enabled in `.csproj`), this removes most ceremony. Implicit usings auto-import `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `Microsoft.AspNetCore.*`, etc. — your files start clean.

### Recommended reading
- **C# in Depth, 4th ed.** — Jon Skeet. Read chapters on records, pattern matching, nullable references.
- Official docs: <https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/>
- "Effective C#" — Bill Wagner.
- Stephen Cleary's blog on async (<https://blog.stephencleary.com/>) — read at least "Async/Await — Best Practices in Asynchronous Programming".

---

## 2. .NET 10 runtime and tooling

### 2.1 SDK pinning

`global.json` at repo root pins the SDK:

```json
{
  "sdk": {
    "version": "10.0.107",
    "rollForward": "latestPatch"
  }
}
```

`rollForward: latestPatch` means any patch ≥ 107 of the 10.0.x line is allowed but not a different minor/major. This guarantees that every developer and CI runner builds with the same compiler — critical for reproducible builds.

### 2.2 Directory.Build.props and Directory.Packages.props

MSBuild walks up from each `.csproj` until it finds these files and imports them automatically.

`Directory.Build.props` (repo-wide settings):

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props` (central package versioning):

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="12.2.0" />
    <PackageVersion Include="FluentValidation" Version="11.10.0" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

In `.csproj` you reference packages without versions: `<PackageReference Include="MediatR" />`. This eliminates version drift between projects.

### 2.3 Solution file (`.slnx`)

`.slnx` is the new XML solution format introduced in .NET 9 / VS 17.10. Simpler than `.sln`, hand-editable, supports nested folders cleanly. Used in Eventify as `Eventify.slnx`. Add projects with `dotnet sln Eventify.slnx add <path>`.

### 2.4 The `dotnet` CLI essentials

```bash
dotnet new sln -n Eventify
dotnet new classlib -n Eventify.Catalog.Domain
dotnet sln add src/Services/Catalog/Eventify.Catalog.Domain
dotnet add reference ../Eventify.Catalog.Domain
dotnet add package FluentValidation
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Services/Catalog/Eventify.Catalog.Api
dotnet ef migrations add Init --project Infrastructure --startup-project Api
dotnet ef database update --project Infrastructure --startup-project Api
```

You will run these constantly. Memorize the `--project` / `--startup-project` distinction: `--project` is where migrations are stored (Infrastructure), `--startup-project` is where the host wires up DbContext (Api).

### 2.5 The build pipeline (simplified)

```
.cs files → Roslyn (csc) → IL (.dll) → JIT (tiered compilation) → native code
                                     → AOT (optional, not used in Eventify)
```

You don't need to know IL, but you should know:
- The compiler emits warnings as errors (per `Directory.Build.props`). Warnings are not noise; treat them as bugs.
- Build output goes to `bin/Debug/net10.0/` and `bin/Release/net10.0/`. CI builds Release.
- Each project produces one assembly named after the project (`Eventify.Catalog.Domain.dll`).

### Recommended reading
- "Pro .NET 5 Custom Templates and Tools" for dotnet CLI deep dive.
- Microsoft docs: <https://learn.microsoft.com/en-us/dotnet/core/tools/>
- "Pro .NET Memory Management" — Konrad Kokosa (for understanding GC and value types when you need to optimize).

---

## 3. EF Core 9 fundamentals

EF Core is the ORM in every Eventify service. You need to understand four things deeply: `DbContext`, the change tracker, the LINQ-to-SQL translation, and the unit of work pattern it implements.

### 3.1 DbContext is your unit of work

`DbContext` represents a single business transaction. It tracks loaded entities, accumulates changes, and writes them in one `SaveChangesAsync` call inside a database transaction.

```csharp
public sealed class CatalogDbContext : DbContext
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Event> Events => Set<Event>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
```

Lifetime: register as **scoped** (default for `AddDbContext<T>`). One DbContext per HTTP request. **Never** share across threads or requests.

### 3.2 The change tracker

When you load an entity through EF Core (e.g., `await db.Artists.FirstAsync(x => x.Id == id)`), EF Core stores a snapshot of its property values. When you call `SaveChangesAsync`, it compares current values to the snapshot, detects changes, and generates the minimal `UPDATE` SQL.

States:
- **Added** — entity was just `Add`ed, will be `INSERT`ed.
- **Modified** — entity is tracked and at least one property changed; will be `UPDATE`d.
- **Unchanged** — tracked but no changes.
- **Deleted** — `Remove`d; will be `DELETE`d.
- **Detached** — not tracked.

Crucial fact for Eventify: when an entity is `Deleted`, EF Core *detaches* it from the ChangeTracker after `SaveChangesAsync`. This is why our `PublishDomainEventsInterceptor` runs **pre-save** — otherwise `*DeletedDomainEvent`s would be silently dropped because the aggregate is no longer in `ChangeTracker.Entries()`.

### 3.3 Configuration: fluent API per aggregate

We prefer `IEntityTypeConfiguration<T>` over data annotations:

```csharp
public sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("artists");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new ArtistId(value));

        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Bio).HasMaxLength(2000);

        builder.HasIndex(a => a.Name);
    }
}
```

Why: configuration code stays out of Domain (which has zero EF Core dependency), is colocated by aggregate, and is easy to grep.

### 3.4 Querying

```csharp
// IQueryable: deferred, composable, translated to SQL
IQueryable<Artist> query = db.Artists.Where(a => a.Name.StartsWith("Cold"));

// Execution: ToListAsync, FirstAsync, SingleAsync, CountAsync, AnyAsync, etc.
var artists = await query.OrderBy(a => a.Name).Skip(0).Take(20).ToListAsync(ct);

// Projection (avoid loading whole entity if you only need fields)
var dtos = await db.Artists
    .Where(a => a.IsActive)
    .Select(a => new ArtistListItemDto(a.Id.Value, a.Name))
    .ToListAsync(ct);

// Eager loading
var artistWithEvents = await db.Artists
    .Include(a => a.Events)
        .ThenInclude(e => e.Sessions)
    .FirstOrDefaultAsync(a => a.Id == id, ct);

// Read-only queries — disable change tracking for performance
await db.Artists.AsNoTracking().ToListAsync(ct);
```

Pitfalls:
- N+1 queries: looping over entities and lazily loading per-iteration. EF Core 9 doesn't enable lazy loading by default — keep it that way.
- Client-side evaluation: if an expression can't be translated, EF Core throws (in EF Core 3+). Good — you want to know.
- Always `await` and always pass `CancellationToken`.

### 3.5 SaveChangesAsync and transactions

`SaveChangesAsync` wraps all changes in a single transaction by default (the SaveChanges Transaction). If you need to span multiple `SaveChangesAsync` calls in one transaction:

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);
try
{
    db.Artists.Add(artist);
    await db.SaveChangesAsync(ct);

    db.AuditLogs.Add(log);
    await db.SaveChangesAsync(ct);

    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

In Eventify this is rare because we colocate domain change + outbox row in one aggregate operation, and MassTransit's transactional outbox uses an EF Core `IDbContextTransaction` under the hood.

### 3.6 Migrations

```bash
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Api
dotnet ef database update --project Infrastructure --startup-project Api
dotnet ef migrations script --project Infrastructure --startup-project Api > migrate.sql
```

Each migration is a C# class that EF Core generates by diffing the current model against the previous snapshot. **Always review the generated SQL before applying** — EF Core sometimes generates destructive operations (drop + recreate) when a subtler `ALTER` would do.

In dev: `db.Database.MigrateAsync()` at startup. In production / K8s: run migrations as a separate `Job` to keep startup safe (no migrations on hot-path container start).

### Recommended reading
- **"Entity Framework Core in Action, 2nd ed."** — Jon P Smith. Definitive book.
- Official docs: <https://learn.microsoft.com/en-us/ef/core/>
- Julie Lerman's Pluralsight courses on EF Core.
- "Mastering EF Core 9" (when released) — Sander van Vugt.

---

## 4. ASP.NET Core dependency injection

### 4.1 The container

ASP.NET Core ships with a built-in DI container (`Microsoft.Extensions.DependencyInjection`). It is intentionally minimal — no auto-registration, no property injection, no AOP. Eventify uses it as-is.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IArtistRepository, ArtistRepository>();
builder.Services.AddTransient<INotificationFactory, NotificationFactory>();

var app = builder.Build();
```

### 4.2 Lifetimes — pick wrong and you break the app

| Lifetime | One instance per | Use for |
|---|---|---|
| **Singleton** | Application | Stateless services, configuration, factories, caches |
| **Scoped** | HTTP request (or DI scope) | DbContext, repositories, MediatR handlers, anything that holds request state |
| **Transient** | Every injection | Lightweight stateless objects; cheap to create |

Pitfalls:
- Injecting a **scoped** service into a **singleton** captures the scoped service forever — leak + thread-safety bug. The container will throw at build time in dev (`ValidateScopes = true`).
- DbContext is scoped. Anything that depends on it (repositories, handlers) must be scoped or transient.
- For background services (singleton) that need a DbContext, inject `IServiceScopeFactory` and create a scope per work unit:

```csharp
public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxPublisher(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            // ... work
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

### 4.3 Configuration binding

```csharp
public sealed class StripeOptions
{
    public required string SecretKey { get; init; }
    public required string WebhookSecret { get; init; }
}

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));

// Then inject IOptions<StripeOptions> (singleton), IOptionsSnapshot<StripeOptions> (scoped, re-reads),
// or IOptionsMonitor<StripeOptions> (singleton + change notifications).
```

Use `IOptions` for config that doesn't change at runtime. Use `IOptionsMonitor` if you actually consume change notifications.

### 4.4 Hosted services

`IHostedService` and its convenience base class `BackgroundService` run for the lifetime of the host. Use for: outbox publishers, reservation TTL sweepers, message bus startup. They are singletons by definition.

### 4.5 Service provider scopes

Inside MVC/Minimal API request handling, ASP.NET Core opens a scope per request. Inside MassTransit consumers, MassTransit opens a scope per message. You generally don't manage scopes yourself outside of background services.

### Recommended reading
- "Dependency Injection Principles, Practices, and Patterns" — Mark Seemann, Steven van Deursen. Language-agnostic, deeply principled.
- Andrew Lock's blog (<https://andrewlock.net/>) — best ASP.NET Core internals writing in the ecosystem.
- "ASP.NET Core in Action, 3rd ed." — Andrew Lock.

---

## 5. Minimal APIs

ASP.NET Core Minimal APIs replace MVC Controllers for most use cases. Eventify uses them exclusively, organized via Carter (Section 50).

### 5.1 The basics

```csharp
var app = builder.Build();

app.MapGet("/health", () => Results.Ok("ok"));

app.MapGet("/artists/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetArtistQuery(id), ct);
    return result.Match(
        dto => Results.Ok(dto),
        errors => errors.ToProblemDetails());
});

app.Run();
```

The framework binds parameters from route, query, body, headers, or services automatically. Sources:
- Route values match by name.
- Service types resolved from DI (no attribute needed if registered).
- Complex types default to JSON body.
- Use `[FromQuery]`, `[FromHeader]`, `[AsParameters]` for explicit control.

### 5.2 IResult and Results.X

`Results.Ok(value)`, `Results.NotFound()`, `Results.Created(uri, value)`, `Results.Problem(...)`, `Results.Stream(...)`. These produce `IResult` implementations that ASP.NET serializes correctly.

For OpenAPI metadata:

```csharp
app.MapPost("/artists", CreateArtist)
   .WithName("CreateArtist")
   .WithTags("Catalog")
   .Produces<ArtistDto>(StatusCodes.Status201Created)
   .ProducesProblem(StatusCodes.Status400BadRequest)
   .RequireAuthorization("Admin");
```

### 5.3 Route groups

```csharp
var v1 = app.MapGroup("/v1").WithTags("v1");
var artists = v1.MapGroup("/artists").RequireAuthorization();
artists.MapGet("/", ListArtists);
artists.MapGet("/{id:guid}", GetArtist);
artists.MapPost("/", CreateArtist).RequireAuthorization("Admin");
```

Carter wraps this so each aggregate becomes an `ICarterModule` with its own `AddRoutes(IEndpointRouteBuilder)`.

### 5.4 Why not Controllers?

- Less ceremony — no class, no `[ApiController]`, no `[HttpGet]`.
- Better performance — no MVC pipeline.
- First-class support for OpenAPI in .NET 10 via `Microsoft.AspNetCore.OpenApi`.
- Forces *thin* handlers — there is nowhere to hide business logic.

Why not FastEndpoints? Conflicts with MediatR's "endpoint = thin shell" idiom; FastEndpoints encourages putting the handler in the endpoint class, which duplicates the MediatR handler indirection. See ARCHITECTURE.md §8.10.

### Recommended reading
- "ASP.NET Core in Action, 3rd ed." — Andrew Lock, chapter on Minimal APIs.
- Official docs: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis>
- Stephen Cleary's "Minimal APIs vs Controllers" series.

---

# Part II — Domain-Driven Design

DDD is the philosophical backbone of Eventify's Clean Architecture services. Without DDD, you would have anemic data models, "service" classes full of procedural logic, and no way to keep complexity from leaking everywhere as the system grows. DDD splits into **strategic** (how you carve the system into bounded contexts) and **tactical** (the building blocks inside each context).

## 6. Strategic DDD: bounded contexts and ubiquitous language

### 6.1 The core insight

A "User" in Identity is not the same thing as a "User" in Booking. Identity's User has email, password hash, roles. Booking's User is just an ID and maybe a display name pulled from a Userinfo lookup. Trying to define one universal User across the whole system forces you to invent god-objects with 100 properties no individual context needs.

A **Bounded Context** is the boundary inside which a particular model is consistent and a particular vocabulary applies. The same word ("Ticket", "Reservation", "Session") can mean different things in different contexts, and that's healthy.

### 6.2 Bounded Context = microservice (mostly)

In Eventify, each microservice owns one bounded context:

| Bounded Context | Service | "Session" means... |
|---|---|---|
| Catalog | Catalog | A scheduled instance of an event (with PriceTiers, SeatLayout) |
| Booking | Booking | A reference (SessionId + cached pricing) for reservation/saga state |
| Ticket | Ticket | A reference (SessionId + label cached on the ticket) |

The Catalog model is rich; the Booking model only needs what it cares about. Each service stores what it needs in its own database. No service queries another service's database directly — Section 21.

### 6.3 Ubiquitous Language

Inside each bounded context, the team (developers, domain experts, docs) uses one vocabulary, and the *code* uses the same vocabulary. If business calls it a "Session", the class is `Session`, not `Show`, `Event`, `Concert`, or `Performance`. The Glossary in ARCHITECTURE.md §2 is your ubiquitous language for Eventify.

This sounds trivial. It is not. Anemic codebases are full of `EventManager.ProcessBooking()` style names that reflect *technical thinking*, not the domain. When you name a method, ask: "would a non-developer say this sentence?" `reservation.Confirm()` — yes. `bookingService.ProcessAndPersist()` — no.

### 6.4 Context Mapping

When two contexts must integrate (Booking needs Session details from Catalog), pick a relationship pattern:

- **Customer / Supplier** — downstream depends on upstream's schema; upstream agrees to support it. (Booking → Catalog gRPC contract.)
- **Conformist** — downstream just accepts whatever upstream provides. (Notification consumes whatever IntegrationEvents Booking publishes.)
- **Anti-Corruption Layer (ACL)** — downstream wraps the upstream model in its own translation layer so its domain stays clean. (Payment wraps Stripe's `PaymentIntent` in an internal `Payment` aggregate.)
- **Shared Kernel** — a small shared model both contexts depend on. (Eventify.IntegrationEvents and SharedKernel `Money`.)
- **Published Language** — a stable, versioned format for inter-context communication. (Our IntegrationEvents.)

### 6.5 The Three Amigos and Event Storming

Before coding, do **Event Storming** with domain experts: paste sticky notes on a wall, each note an event in past tense ("Reservation Created", "Payment Succeeded"). Group by time. Discover commands, aggregates, policies, and bounded contexts emergently. This is how you arrive at the catalog/booking/payment split rather than guessing.

### Recommended reading
- **"Domain-Driven Design" — Eric Evans** (the Blue Book). Required.
- **"Implementing Domain-Driven Design" — Vaughn Vernon** (the Red Book). More practical, more code.
- **"Learning Domain-Driven Design" — Vlad Khononov** (2021). The best modern intro — read first.
- "Domain-Driven Design Distilled" — Vaughn Vernon. Short summary.
- Event Storming: "Introducing EventStorming" — Alberto Brandolini.

---

## 7. Tactical DDD building blocks

These are the actual classes you write in the Domain layer.

### 7.1 Entity

An object with identity. Two entities with the same ID are the same entity, even if other fields differ.

```csharp
public abstract class Entity<TId> : IEntity, IAuditable
    where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    protected Entity(TId id) { Id = id; }
    protected Entity() { Id = default!; }   // EF Core

    DateTimeOffset IAuditable.CreatedAt { set => CreatedAt = value; }
    DateTimeOffset? IAuditable.UpdatedAt { set => UpdatedAt = value; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}
```

Key points:
- Identity-based equality, *not* property equality.
- Audit fields exposed only via internal `IAuditable` interface → outside code can't mutate `CreatedAt`.
- Parameterless protected ctor for EF Core materialization.

### 7.2 Value Object

An object defined entirely by its properties. No identity. Immutable. Two value objects with the same property values are equal.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new DomainException("Money.Amount must be non-negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Money.Currency must be ISO 4217 (3 letters).");
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add Money in different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);
}
```

Note the constructor validates invariants. `Money` can never be in an invalid state — period. Operations return new `Money` instances; the original is unchanged.

`record` gives you structural equality for free. If you write your own `ValueObject` base class (as some DDD books do), it's because records didn't exist yet — in C# 10+ just use `record`.

### 7.3 Aggregate and Aggregate Root

An **Aggregate** is a cluster of entities and value objects that must change together to maintain invariants. The **Aggregate Root** is the only entity in the cluster that the outside world is allowed to reference. All modifications go through the root.

```csharp
public sealed class Reservation : AggregateRoot<ReservationId>
{
    private readonly List<ReservedSeat> _seats = [];

    public UserId UserId { get; private set; }
    public SessionId SessionId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public IReadOnlyList<ReservedSeat> Seats => _seats.AsReadOnly();

    private Reservation() { }     // EF Core

    private Reservation(ReservationId id, UserId userId, SessionId sessionId, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        SessionId = sessionId;
        Status = ReservationStatus.Pending;
        ExpiresAt = expiresAt;
    }

    public static Reservation Create(UserId userId, SessionId sessionId, TimeSpan ttl, IClock clock)
    {
        var reservation = new Reservation(
            new ReservationId(Guid.CreateVersion7()),
            userId,
            sessionId,
            clock.UtcNow.Add(ttl));
        reservation.RaiseDomainEvent(new ReservationCreatedDomainEvent(reservation.Id, sessionId, userId));
        return reservation;
    }

    public void AddSeat(SeatId seatId, Money price, SectionCategory category)
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException("Cannot add seats to a non-pending reservation.");
        if (_seats.Any(s => s.SeatId == seatId))
            throw new DomainException("Seat already in this reservation.");
        _seats.Add(new ReservedSeat(seatId, price, category));
    }

    public void Confirm(IClock clock)
    {
        if (Status != ReservationStatus.Pending)
            throw new DomainException("Only pending reservations can be confirmed.");
        if (clock.UtcNow >= ExpiresAt)
            throw new DomainException("Reservation has expired.");
        Status = ReservationStatus.Confirmed;
        RaiseDomainEvent(new ReservationConfirmedDomainEvent(Id));
    }
}
```

Notes:
- All mutation goes through methods that enforce invariants. No public setters.
- `_seats` is a private list; `Seats` exposes a read-only view. Outside code cannot add a seat directly.
- Factory method `Create` is the only construction path. Constructor is private.
- Domain events are raised whenever something noteworthy happens.
- `DomainException` is for invariant violations — they should *never* occur if the caller validated first. They indicate a bug, not a business error (use `ErrorOr` for those).

#### Aggregate design rules
1. **Small aggregates.** Don't pull every related entity into one root. The smaller the aggregate, the lower the contention and the easier the transaction.
2. **Reference other aggregates by ID, not by reference.** `Reservation` holds `SessionId`, not `Session`. This forces explicit lookups and keeps transactional boundaries clean.
3. **One transaction = one aggregate.** Modifying multiple aggregates in one transaction usually means your aggregate boundaries are wrong.
4. **Eventual consistency between aggregates.** If `Booking` confirms and `Ticket` must be issued, that's a domain/integration event flow, not a single transaction.

### 7.4 Domain Service

When a piece of domain logic doesn't naturally belong to one aggregate (because it needs two), put it in a domain service. **Stateless.** Operates on aggregates. Lives in the Domain project.

```csharp
public interface IUniqueEmailChecker
{
    Task<bool> IsUniqueAsync(Email email, CancellationToken ct);
}
```

The implementation lives in Infrastructure (it queries the DB), but the interface is in Domain because the *contract* belongs to the domain.

Don't overuse domain services. If logic fits inside one aggregate, put it there.

### 7.5 Repository

A repository abstracts persistence for an aggregate root. One repository per aggregate root.

```csharp
public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken ct);
    Task AddAsync(Reservation reservation, CancellationToken ct);
    void Remove(Reservation reservation);
}
```

Implementation in Infrastructure wraps `DbContext`:

```csharp
public sealed class ReservationRepository : IReservationRepository
{
    private readonly BookingDbContext _db;

    public ReservationRepository(BookingDbContext db) => _db = db;

    public Task<Reservation?> GetByIdAsync(ReservationId id, CancellationToken ct) =>
        _db.Reservations.Include(r => r.Seats).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(Reservation r, CancellationToken ct) => await _db.Reservations.AddAsync(r, ct);

    public void Remove(Reservation r) => _db.Reservations.Remove(r);
}
```

Note: no `SaveChanges` in the repository. Saving is the responsibility of the **Unit of Work** (the DbContext itself, called from the handler). The repository handles tracking; the handler commits.

#### Generic repository — usually a mistake
You will see `IRepository<T>` with `GetAll`, `Find(predicate)`, `Add`, `Remove`. This leaks query concerns into every aggregate equally and tempts you to bypass aggregate boundaries. Prefer specific repositories per aggregate.

#### Repository vs EF Core DbSet
Some teams skip the repository layer and inject `DbContext` directly into handlers. That works, but couples handlers to EF Core and makes testing harder (you have to mock `IQueryable`). Eventify uses repositories for the aggregate-loading path; ad-hoc read queries in Application can use `DbContext` directly when they project to DTOs.

### 7.6 Specification pattern (optional, not used in MVP)

Encapsulates a query predicate as an object. Useful when you have many query variations on the same aggregate and want to compose them. Out of scope for MVP — mention only because you'll see it in books.

### Recommended reading
- "Implementing Domain-Driven Design" — Vaughn Vernon, chapters 5–10 (aggregates).
- "Patterns, Principles, and Practices of Domain-Driven Design" — Scott Millett, Nick Tune. Practical .NET focus.
- Article: "Effective Aggregate Design" (3-part) — Vaughn Vernon. <https://www.dddcommunity.org/library/vernon_2011/>
- Jimmy Bogard's blog on DDD: <https://www.jimmybogard.com/>

---

## 8. Domain events vs Integration events

A constant source of confusion. They are not the same, and mixing them causes coupling and data leaks.

### 8.1 Domain Event

- Raised by an aggregate to signal something meaningful happened inside the domain.
- **In-process.** Handled by the same service that raised it.
- Schema is *internal* — can freely change with code.
- Examples: `ReservationCreatedDomainEvent`, `SeatAddedToReservationDomainEvent`.

```csharp
public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ReservationCreatedDomainEvent(ReservationId ReservationId, SessionId SessionId, UserId UserId)
    : DomainEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
```

Dispatch in Eventify: `PublishDomainEventsInterceptor` runs in `SavingChangesAsync` (pre-save), collects domain events from all tracked aggregates, clears them, and publishes via MediatR `IPublisher` (notifications, not requests).

Why pre-save:
1. EF Core detaches `Deleted` entities after `SaveChangesAsync`, so post-save dispatch would silently drop `*DeletedDomainEvent`.
2. A domain event handler that writes to the Outbox table must participate in the *same* DB transaction as the aggregate change — pre-save guarantees atomicity. If the outbox write happened post-save and crashed, you'd lose the integration event.

### 8.2 Integration Event

- Published by a service to RabbitMQ for *other services* to consume.
- **Cross-process.** Schema is a **contract** — once published, you can't break it without versioning.
- Past tense, plain data (no logic, no behavior).
- Examples: `BookingConfirmedIntegrationEvent`, `PaymentSucceededIntegrationEvent`.

```csharp
public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredAt);

public sealed record BookingConfirmedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid SessionId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<Guid> SeatIds)
    : IntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
```

These live in `Eventify.IntegrationEvents` — a separate assembly with **zero dependencies on internal domain types**. The contract must be re-creatable by any consumer using only the integration-events package. Never put a `ReservationId` or `Money` from Domain into an integration event — use `Guid` and `decimal + string`.

### 8.3 The bridge: domain event handler writes integration event to outbox

```csharp
public sealed class BookingConfirmedDomainEventHandler : INotificationHandler<BookingConfirmedDomainEvent>
{
    private readonly BookingDbContext _db;     // shares the same transaction as the aggregate change

    public BookingConfirmedDomainEventHandler(BookingDbContext db) => _db = db;

    public async Task Handle(BookingConfirmedDomainEvent e, CancellationToken ct)
    {
        var integration = new BookingConfirmedIntegrationEvent(
            e.BookingId.Value, e.UserId.Value, e.SessionId.Value,
            e.TotalAmount.Amount, e.TotalAmount.Currency, e.SeatIds.Select(s => s.Value).ToList());

        // MassTransit Outbox-aware Bus: the message ends up in the OutboxMessage table
        // in the same transaction as the aggregate.
        await _publishEndpoint.Publish(integration, ct);
    }
}
```

The integration event sits in the Outbox table until MassTransit's delivery service picks it up and publishes to RabbitMQ. Section 23.

### Recommended reading
- "Domain Events: Salvation" — Udi Dahan. <https://udidahan.com/2009/06/14/domain-events-salvation/>
- Jimmy Bogard's MediatR notifications pattern: <https://www.jimmybogard.com/a-better-domain-events-pattern/>

---

## 9. Strongly-typed IDs and UUIDv7

### 9.1 Why strongly-typed IDs

```csharp
void AssignArtistToEvent(Guid eventId, Guid artistId) { }

// Bug: arguments are swapped, compiler is silent because both are Guid.
AssignArtistToEvent(artistGuid, eventGuid);
```

With strongly-typed IDs:

```csharp
public readonly record struct EventId(Guid Value);
public readonly record struct ArtistId(Guid Value);

void AssignArtistToEvent(EventId eventId, ArtistId artistId) { }

AssignArtistToEvent(artistId, eventId);    // compile error
```

You eliminate a whole class of bugs at compile time.

### 9.2 EF Core mapping

```csharp
builder.Property(e => e.Id)
    .HasConversion(id => id.Value, value => new EventId(value));
```

Or globally per type:

```csharp
configurationBuilder.Properties<EventId>().HaveConversion<EventIdConverter>();
```

### 9.3 JSON serialization

System.Text.Json needs a converter:

```csharp
public sealed class EventIdJsonConverter : JsonConverter<EventId>
{
    public override EventId Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
        => new(r.GetGuid());
    public override void Write(Utf8JsonWriter w, EventId v, JsonSerializerOptions o)
        => w.WriteStringValue(v.Value);
}
```

Register: `options.Converters.Add(new EventIdJsonConverter());`

This is boilerplate. Some teams use source generators (e.g., `StronglyTypedId` NuGet) to generate everything. Eventify keeps it manual to fit the "no magic" preference, but the source generator is a reasonable alternative.

### 9.4 UUIDv7

UUIDv4 is random — 122 bits of entropy. As a primary key, this destroys B-tree locality on insert: each new row goes to a random page, causing page splits and cache misses.

UUIDv7 (RFC 9562, 2024) puts a 48-bit Unix millisecond timestamp in the high bits, then 74 bits of entropy. Result:
- Time-sortable. Newer rows compare greater.
- B-tree friendly. Sequential inserts stay in hot pages.
- Still globally unique (no central coordination).

```csharp
var id = Guid.CreateVersion7();   // .NET 9+
```

Use this for every entity primary key, every domain event ID, every integration event ID. Postgres stores it in the `uuid` type — no schema change needed.

### Recommended reading
- "Strongly Typed Ids in C#" — Andrew Lock series.
- RFC 9562 (UUID Revision): <https://datatracker.ietf.org/doc/rfc9562/>
- Postgres B-tree internals: <https://www.postgresql.org/docs/current/btree.html>

---

# Part III — Architectural styles

Eventify deliberately uses **two** architectural styles. Identity, Catalog, Booking, Payment use **Clean Architecture** (4 projects each). Ticket and Notification use **Vertical Slice Architecture** (single project each). Knowing both, and knowing *when* to switch, is half the value of this project.

## 10. Clean Architecture

### 10.1 The intellectual lineage

Clean Architecture (Robert C. Martin, 2012) is the modern name for a family of architectures with the same dependency rule:

- **Hexagonal Architecture** / **Ports & Adapters** — Alistair Cockburn (2005). Domain in the center, "ports" are interfaces, "adapters" are implementations.
- **Onion Architecture** — Jeffrey Palermo (2008). Concentric rings; dependencies point inward.
- **Clean Architecture** — Uncle Bob (2012). Synthesis; introduced the famous concentric-circle diagram.

They are all the same idea: **business rules must not depend on frameworks, UI, or databases**. The reverse arrow: frameworks depend on business rules through abstractions defined by the business rules.

### 10.2 The Dependency Rule

> Source code dependencies must point only inward, toward higher-level policies.

In Eventify terms:

```
+-------------------------------------------------+
|  Api  (Carter modules, hosting, config)         |   <-- outermost
|    depends on Application + Infrastructure       |
|  +-----------------------------------------+    |
|  | Infrastructure (EF Core, Redis, gRPC,   |    |
|  |                MassTransit, Stripe SDK) |    |
|  |   depends on Application + Domain       |    |
|  |  +--------------------------------+     |    |
|  |  | Application (handlers,         |     |    |
|  |  |   command/query, validators,   |     |    |
|  |  |   port interfaces)             |     |    |
|  |  |   depends on Domain ONLY       |     |    |
|  |  |  +----------------------+      |     |    |
|  |  |  | Domain (aggregates,  |      |     |    |
|  |  |  |   value objects,     |      |     |    |
|  |  |  |   domain events,     |      |     |    |
|  |  |  |   port interfaces)   |      |     |    |
|  |  |  |   ZERO dependencies  |      |     |    |
|  |  |  +----------------------+      |     |    |
|  |  +--------------------------------+     |    |
|  +-----------------------------------------+    |
+-------------------------------------------------+
```

NetArchTest enforces these rules in CI (Section 58).

### 10.3 Project layout for Catalog (canonical)

```
src/Services/Catalog/
├── Eventify.Catalog.Domain/
│   ├── Artists/
│   │   ├── Artist.cs                  (aggregate root)
│   │   ├── ArtistId.cs                (record struct)
│   │   ├── ArtistName.cs              (value object)
│   │   ├── Events/                    (domain events for this aggregate)
│   │   │   ├── ArtistCreatedDomainEvent.cs
│   │   │   └── ArtistDeletedDomainEvent.cs
│   │   ├── Exceptions/
│   │   │   └── ArtistNotFoundException.cs
│   │   └── IArtistRepository.cs       (port — interface defined by Domain)
│   ├── Events/
│   ├── Venues/
│   ├── Sessions/
│   └── DomainErrors.cs                (named Error constants)
│
├── Eventify.Catalog.Application/
│   ├── Artists/
│   │   ├── Commands/
│   │   │   ├── CreateArtist/
│   │   │   │   ├── CreateArtistCommand.cs       (record, implements ICommand<ArtistDto>)
│   │   │   │   ├── CreateArtistCommandHandler.cs
│   │   │   │   └── CreateArtistCommandValidator.cs
│   │   │   ├── UpdateArtist/
│   │   │   └── DeleteArtist/
│   │   ├── Queries/
│   │   │   ├── GetArtist/
│   │   │   └── ListArtists/
│   │   └── Dto/
│   │       └── ArtistDto.cs
│   └── DependencyInjection.cs         (extension: AddApplication())
│
├── Eventify.Catalog.Infrastructure/
│   ├── Persistence/
│   │   ├── CatalogDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── ArtistConfiguration.cs
│   │   │   └── ...
│   │   ├── Migrations/
│   │   └── Repositories/
│   │       └── ArtistRepository.cs    (implements Domain port)
│   ├── Grpc/                          (Catalog.GrpcService — implements proto)
│   ├── Messaging/                     (MassTransit configuration)
│   └── DependencyInjection.cs         (extension: AddInfrastructure())
│
└── Eventify.Catalog.Api/
    ├── Endpoints/
    │   ├── ArtistsModule.cs           (ICarterModule)
    │   ├── EventsModule.cs
    │   └── ...
    ├── appsettings.json
    ├── Program.cs
    └── DependencyInjection.cs         (extension: AddApi())
```

### 10.4 What each layer owns

**Domain (`Eventify.Catalog.Domain`)**
- Aggregates, entities, value objects, enums.
- Domain events.
- Domain exceptions (`DomainException` subclasses).
- *Interfaces* that the domain needs from outside (e.g., `IArtistRepository`, `IClock`).
- Zero NuGet dependencies except `Eventify.SharedKernel` (which itself has zero external deps in its Domain part).
- **No EF Core attributes, no JSON attributes, no MediatR.**

**Application (`Eventify.Catalog.Application`)**
- Commands and queries (request DTOs).
- Handlers (orchestrate domain operations).
- Validators (FluentValidation).
- DTOs returned to the API layer.
- Pipeline behaviors (logging, validation).
- *Interfaces* for ports it needs (e.g., `IFileStorage`, `IEmailGateway`) — implementations in Infrastructure.
- Returns `ErrorOr<TResult>` from every handler.
- Depends on: Domain, MediatR, FluentValidation, ErrorOr, SharedKernel.Application.
- **No EF Core, no Stripe, no MassTransit.**

**Infrastructure (`Eventify.Catalog.Infrastructure`)**
- EF Core DbContext, entity configurations, migrations.
- Repository implementations.
- Third-party SDK adapters (Stripe, Redis, gRPC client wrappers).
- MassTransit consumer registrations and saga state-machine persistence.
- Provides extension methods that register all of the above.
- Depends on: Application, Domain, all third-party libraries.

**Api (`Eventify.Catalog.Api`)**
- `Program.cs` — composition root.
- Carter modules (endpoint definitions).
- `appsettings.json`, environment-specific overrides.
- Filters, middleware, authentication setup.
- Depends on: everything.

### 10.5 The composition root

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();
app.Run();
```

Each layer exposes a single `AddXxx(IServiceCollection, IConfiguration?)` extension. `Program.cs` is the only place where layers compose. This makes dependency wiring discoverable and prevents the "magic" of auto-scanning.

### 10.6 Cost vs benefit

Costs:
- 4 projects per service, plus tests = 6+ projects per service. For 4 Clean-Arch services, that's 24+ projects.
- More ceremony to add a feature: command + handler + validator + endpoint + repository method + DTO + integration event.
- More files to navigate.

Benefits:
- The business rules are testable without any framework loaded.
- You can swap EF Core for Dapper, Postgres for MSSQL, MediatR for direct dispatch, with zero changes to Domain.
- Reviewers can spot a layering violation instantly.
- New developers can find anything by name: command in Application, configuration in Infrastructure, aggregate in Domain.

For a learning project this trade is excellent. For a tiny CRUD microservice (Notification, Ticket) it is overkill — hence VSA.

### Recommended reading
- **"Clean Architecture"** — Robert C. Martin. The book.
- **"Get Your Hands Dirty on Clean Architecture"** — Tom Hombergs. Most practical, with Java but principles apply 1:1.
- "Architecture Patterns with Python" — Percival & Gregory (free online). DDD + Clean Arch in Python, but the patterns translate.
- Jason Taylor's Clean Architecture template for .NET: <https://github.com/jasontaylordev/CleanArchitecture>
- "Pragmatic Clean Architecture" course — Milan Jovanović. Best modern .NET walkthrough.

---

## 11. Vertical Slice Architecture

### 11.1 The contrarian view

VSA (Jimmy Bogard, 2014) starts from the observation that Clean Architecture optimizes for *layer reuse*, but most "reuse" inside a single service is fake — a repository method that has one caller, a validator used by one command. By forcing the same shape on every feature, Clean Architecture spreads each feature across 5+ files.

VSA inverts the priority: **a feature should be contained in one place**. Each "slice" (= one use case = one endpoint) gets its own folder containing everything: request, handler, validator, response, mapping.

### 11.2 Project layout for Ticket (canonical VSA)

```
src/Services/Ticket/Eventify.Ticket.Api/
├── Common/
│   ├── Persistence/
│   │   ├── TicketDbContext.cs
│   │   └── Configurations/
│   ├── Behaviors/
│   ├── Errors/
│   └── Extensions/
├── Features/
│   ├── IssueTickets/
│   │   ├── IssueTicketsConsumer.cs   (MassTransit consumer)
│   │   ├── IssueTicketsCommand.cs
│   │   ├── IssueTicketsHandler.cs
│   │   ├── IssueTicketsValidator.cs
│   │   └── Ticket.cs                  (domain class colocated)
│   ├── GetMyTickets/
│   │   ├── GetMyTicketsEndpoint.cs    (ICarterModule)
│   │   ├── GetMyTicketsQuery.cs
│   │   ├── GetMyTicketsHandler.cs
│   │   └── TicketDto.cs
│   ├── ValidateTicket/
│   │   └── ...
│   └── RevokeTicketsOnBookingCancelled/
│       └── ...
├── Program.cs
└── appsettings.json
```

Notice:
- No `Domain` / `Application` / `Infrastructure` projects.
- Domain classes live next to the features that use them. If `Ticket` only mutates inside `IssueTicketsHandler` and `ValidateTicketHandler`, both can sit in the same folder tree.
- Cross-cutting concerns (DbContext, validators, behaviors) live in `Common/`.
- Each slice is independently understandable.

### 11.3 When VSA shines

- The service has few cross-cutting concepts.
- Most features are CRUD-shaped (one command/query → one DB write/read → done).
- Few aggregate invariants to enforce. (Tickets: state machine with 3 states; no rich domain.)
- A small team (or solo) maintains it.

### 11.4 When VSA hurts

- Many features share the same aggregate with complex invariants → duplication of aggregate-loading logic across slices.
- Cross-aggregate operations become fuzzy (which slice owns the transaction?).
- A rich domain begs for a Domain layer.

Booking and Catalog are the right call for Clean Architecture. Ticket and Notification are the right call for VSA. ARCHITECTURE.md §4 summarizes the per-service choice.

### 11.5 You can mix in one project

Even inside VSA, nothing forbids extracting a `Domain/` folder for shared aggregates. The point is *defaulting* to feature-folders rather than layers, and only extracting when duplication actually appears.

### Recommended reading
- Jimmy Bogard, "Vertical Slice Architecture": <https://www.jimmybogard.com/vertical-slice-architecture/>
- Milan Jovanović's VSA tutorials: <https://www.milanjovanovic.tech/>
- "MediatR & Vertical Slice Architecture" — Jimmy Bogard, NDC talks on YouTube.

---

## 12. Choosing between Clean and VSA per service

A 30-second rubric:

| Question | If yes → |
|---|---|
| Does the service have ≥3 aggregates with non-trivial invariants? | Clean |
| Will multiple services consume this service's domain library? | Clean (well-named layers help) |
| Does the team rotate? Will many devs touch it? | Clean (predictable structure) |
| Is the service mostly "consume event → write a row → publish event"? | VSA |
| Will the service have <30 endpoints total? | VSA |
| Is the domain primarily CRUD with minor validation? | VSA |

The two are not a moral choice. Pick per service. The mistake is not picking — defaulting to whatever you used last time.

### Recommended reading
- "Choosing Between Clean Architecture and Vertical Slice" — Derek Comartin (CodeOpinion YouTube).

---

# Part IV — CQRS and mediation

## 13. CQRS — Command Query Responsibility Segregation

### 13.1 The core idea

Greg Young, ~2010: in most systems, reads and writes have wildly different requirements (read shape, denormalization, indexing, caching, concurrency), and one model that tries to serve both ends up serving neither well. CQRS says: **use different models for reading and writing**.

In its full form, CQRS implies separate databases (read store + write store) with projections, and is paired with Event Sourcing. That's not Eventify. We use **CQRS-lite**:

- Same database.
- Same EF Core model for reads and writes.
- *But* separate request types and handlers: `CreateArtistCommand` vs `GetArtistQuery`, each with its own handler.

### 13.2 Why even CQRS-lite is worth it

- Each handler has a single responsibility. No "ArtistService" with 12 methods.
- Read handlers can bypass the aggregate (project directly to DTO with `Select`) without breaking the write model's invariants.
- The shape of a command makes the *intent* explicit — `ConfirmReservationCommand` is clearer than `UpdateReservationStatus("confirmed")`.
- Pipeline behaviors (logging, validation, transactions) apply uniformly to all commands or queries.

### 13.3 Command vs Query

| Aspect | Command | Query |
|---|---|---|
| Intent | Change state | Read state |
| Side effects | Yes | No |
| Returns | `ErrorOr<Result>` (often the new resource's ID/DTO) | `ErrorOr<TDto>` or `ErrorOr<PagedResult<TDto>>` |
| Validation | Strict (FluentValidation) | Light (just parameter sanity) |
| Loaded entity | Aggregate root (tracked) | Projection (no tracking) |
| Concurrency | Optimistic via row version | Read-only, no concern |

### 13.4 The CQS principle (don't confuse with CQRS)

Bertrand Meyer's **Command-Query Separation**: every method either *does* something or *returns* something, never both. CQRS is the architectural application of this rule to the whole request layer.

### Recommended reading
- "CQRS Documents" — Greg Young (free PDF). <https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf>
- Martin Fowler: "CQRS". <https://martinfowler.com/bliki/CQRS.html>
- "What is CQRS?" — CodeOpinion (YouTube, Derek Comartin).

---

## 14. MediatR — request/handler/notification

MediatR (Jimmy Bogard) is an in-process mediator. It decouples the *caller* (an endpoint) from the *handler* (the implementation) via a registry. You send a request; MediatR finds the handler; the handler runs; the result flows back.

### 14.1 The two contracts

```csharp
// Commands and queries are "requests" — exactly one handler per request type.
public interface IRequest<TResponse> { }
public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

// Notifications are pub/sub — many handlers per notification.
public interface INotification { }
public interface INotificationHandler<TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken ct);
}
```

Eventify wraps these in our own marker interfaces:

```csharp
public interface ICommand<TResponse> : IRequest<ErrorOr<TResponse>> { }
public interface IQuery<TResponse> : IRequest<ErrorOr<TResponse>> { }
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, ErrorOr<TResponse>>
    where TCommand : ICommand<TResponse> { }
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, ErrorOr<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

This forces every handler to return `ErrorOr<T>` and makes the *intent* of a request type self-documenting.

### 14.2 A complete command flow

```csharp
// 1. The command (Application/Artists/Commands/CreateArtist/)
public sealed record CreateArtistCommand(string Name, string? Bio, string? ImageUrl)
    : ICommand<Guid>;

// 2. The validator
public sealed class CreateArtistCommandValidator : AbstractValidator<CreateArtistCommand>
{
    public CreateArtistCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Bio).MaximumLength(2000);
    }
}

// 3. The handler
public sealed class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IArtistRepository _repo;
    private readonly CatalogDbContext _db;

    public CreateArtistCommandHandler(IArtistRepository repo, CatalogDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<ErrorOr<Guid>> Handle(CreateArtistCommand cmd, CancellationToken ct)
    {
        var artist = Artist.Create(cmd.Name, cmd.Bio, cmd.ImageUrl);
        await _repo.AddAsync(artist, ct);
        await _db.SaveChangesAsync(ct);
        return artist.Id.Value;
    }
}

// 4. The endpoint (Carter module in Api)
public sealed class ArtistsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/artists").WithTags("Artists");

        group.MapPost("/", async (CreateArtistRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(req.ToCommand(), ct);
            return result.Match(
                id => Results.Created($"/v1/artists/{id}", new { id }),
                errors => errors.ToProblemDetails());
        }).RequireAuthorization("Admin");
    }
}
```

The endpoint is genuinely thin: parse, dispatch, return. All business logic is in the handler. All validation in the validator. All persistence in the repository.

### 14.3 Registration

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<CreateArtistCommandHandler>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```

MediatR scans the assembly, registers every handler, every notification handler, and every pipeline behavior in DI.

### 14.4 The licensing footnote

MediatR went commercial at v13 (2024). Eventify pins v12.2, which is the last fully-free version. For a portfolio project this is fine; for a commercial product you'd budget the licence or move to MassTransit Mediator (which is free).

### Recommended reading
- MediatR repo: <https://github.com/jbogard/MediatR>
- Jimmy Bogard's blog on MediatR patterns.
- "You Probably Don't Need MediatR" — Tim Deschryver (counter-argument worth reading).

---

## 15. Pipeline behaviors

A pipeline behavior wraps a request handler — exactly like ASP.NET middleware wraps an HTTP request. The pipeline runs in order, calling `next()` to invoke the next stage; the final stage is the handler itself.

### 15.1 The signature (MediatR 12.2)

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

Note: in MediatR 12.2, `RequestHandlerDelegate<TResponse>` is `delegate Task<TResponse>()` — **no `CancellationToken` parameter**. So you call `await next()`, not `await next(ct)`. If you bump to 12.5+, the signature gains a CT parameter and you call `await next(ct)`.

### 15.2 LoggingBehavior

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling {Request}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("Handled {Request} in {Elapsed}ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler {Request} threw", name);
            throw;
        }
    }
}
```

### 15.3 ValidationBehavior (returns Error.Validation, not throw)

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IErrorOr
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length == 0) return await next();

        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        // Cast required because TResponse is a generic IErrorOr.
        // Real implementation uses reflection or a per-arity dispatch — covered in §17.
        return (TResponse)(object)ErrorOr<object>.From(errors);
    }
}
```

The `TResponse : IErrorOr` constraint ensures this behavior only runs for commands/queries that return `ErrorOr<T>`. If a request returns plain `Unit`, the behavior won't be selected.

### 15.4 Registration order matters

```csharp
cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));    // outermost — sees everything
cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // innermost — runs just before handler
```

The first registered behavior is the *outermost* wrapper. So logs include validation results.

### 15.5 Other useful behaviors (not in MVP)

- **TransactionBehavior** — wraps the handler in a DB transaction. Often unnecessary if `SaveChangesAsync` is the single write point.
- **CachingBehavior** — caches query results by request hash.
- **AuthorizationBehavior** — checks claims against `[Authorize]`-style attributes on the request.
- **IdempotencyBehavior** — looks up `Idempotency-Key` and returns cached response.

### Recommended reading
- Jimmy Bogard, "MediatR Pipeline Examples": <https://www.jimmybogard.com/mediatr-pipeline-examples/>
- Steve Smith's articles on MediatR behaviors.

---

## 16. FluentValidation

### 16.1 Why a library

Manual validation grows ugly fast. FluentValidation gives you:
- Declarative rules read top-to-bottom.
- Localized messages, custom rules, conditional rules.
- Pluggable into MediatR via `ValidationBehavior` (auto runs on every command).

### 16.2 The basics

```csharp
public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("At least one seat must be selected.")
            .Must(s => s.Count <= 10).WithMessage("Cannot reserve more than 10 seats.");
        RuleForEach(x => x.SeatIds).NotEmpty();
    }
}
```

### 16.3 Custom rules

```csharp
RuleFor(x => x.Currency)
    .Must(BeIso4217).WithMessage("Currency must be an ISO 4217 code.");

private static bool BeIso4217(string code) =>
    code.Length == 3 && code.All(char.IsLetter);
```

### 16.4 Async rules (use sparingly)

```csharp
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) => await _users.IsUniqueAsync(email, ct))
    .WithMessage("Email already in use.");
```

Avoid hitting the database from a validator if the handler will check anyway. The handler is the source of truth for race-condition-safe checks; validators are for shape/format.

### 16.5 Registration

```csharp
services.AddValidatorsFromAssemblyContaining<CreateArtistCommandValidator>();
```

`ValidationBehavior` (Section 15.3) will pick them up automatically.

### Recommended reading
- FluentValidation docs: <https://docs.fluentvalidation.net/>
- "Validation in ASP.NET Core" — Andrew Lock blog series.

---

# Part V — Error handling

The most subtle architectural choice you make in any service is how to model failure. Eventify uses a **two-tier** model. Internalize it before writing a single handler.

## 17. Two-tier error model: results vs exceptions

### 17.1 The thesis

Two categories of failure require two different mechanisms:

| Category | Examples | Mechanism |
|---|---|---|
| **Expected business outcomes** | "Seat already booked", "Session not found", "Reservation expired", "Email already in use" | **Result type** (`ErrorOr<T>`) — return a value, no exception thrown |
| **Bugs and infrastructure failures** | NullReferenceException, DB connection lost, RabbitMQ timeout, domain invariant violated | **Exception** — bubble up, caught by global middleware |

A "seat is taken" is not exceptional — it's a normal outcome of a contended system. Modeling it as `throw new SeatTakenException()` has three problems:

1. **Performance**: exceptions on hot paths capture a stack trace, which is expensive (microseconds — adds up under load).
2. **Lying signature**: a handler with signature `Task<BookingDto> Handle(...)` claims it returns a booking, when it actually returns *either* a booking *or* nothing plus a side effect (the exception). Callers must know about the lie.
3. **Coupling endpoints to error semantics**: every endpoint needs a `try/catch (SeatTakenException e) => Conflict()` block. Move one such exception and you must update every catch site.

### 17.2 The result pattern in one sentence

A handler's signature **honestly declares** that it returns *either* a success value *or* a categorized error, and the caller chooses how to react.

### 17.3 Where exceptions still live

- `DomainException` — invariant violation inside an aggregate. *Should never happen* if callers validate first. Indicates a bug.
- Standard CLR exceptions — `ArgumentNullException`, `InvalidOperationException`, etc. — for programmer errors.
- Infrastructure exceptions — EF Core `DbUpdateException`, Polly `BrokenCircuitException`, etc. — bubble up to be observed and converted to a 500 ProblemDetails.

A handler **never throws** for an expected business outcome. It returns `Error.X`.

---

## 18. The ErrorOr library

`ErrorOr` (Amichai Mantinband) is a discriminated union: an `ErrorOr<T>` is either a `T` or a `List<Error>`.

### 18.1 The basics

```csharp
public async Task<ErrorOr<Guid>> Handle(CreateReservationCommand cmd, CancellationToken ct)
{
    if (await _sessions.GetAsync(cmd.SessionId, ct) is not { } session)
        return Error.NotFound("Session.NotFound", "The session does not exist.");

    if (session.StartsAt <= _clock.UtcNow)
        return Error.Conflict("Session.AlreadyStarted", "Cannot reserve seats for a started session.");

    var availability = await _seats.CheckAvailabilityAsync(cmd.SessionId, cmd.SeatIds, ct);
    if (availability.Any(a => !a.IsAvailable))
        return Error.Conflict("Seat.Taken", "One or more seats are already reserved.");

    var reservation = Reservation.Create(cmd.UserId, cmd.SessionId, _ttl, _clock);
    foreach (var seat in availability)
        reservation.AddSeat(seat.SeatId, seat.Price, seat.Category);

    await _repo.AddAsync(reservation, ct);
    await _db.SaveChangesAsync(ct);
    return reservation.Id.Value;
}
```

Implicit conversions from `Error` and `T` make the return statements clean. No `new ErrorOr<Guid>(error)` boilerplate.

### 18.2 Error categories

```csharp
Error.NotFound(code, description);
Error.Conflict(code, description);
Error.Validation(code, description);
Error.Unauthorized(code, description);
Error.Forbidden(code, description);
Error.Failure(code, description);
Error.Unexpected(code, description);
Error.Custom((int)yourCategory, code, description);
```

Each category maps to an HTTP status code by convention:
- NotFound → 404
- Conflict → 409
- Validation → 400
- Unauthorized → 401
- Forbidden → 403
- Failure / Unexpected → 500

### 18.3 Composition

```csharp
ErrorOr<int> result = ParseAge(input)
    .Then(age => ValidateAge(age))
    .Then(age => SaveAge(age));

// or async
ErrorOr<Booking> result = await sessionResult
    .ThenAsync(s => LoadSeatsAsync(s.Id))
    .ThenAsync(seats => ReserveAsync(seats));
```

`Then` runs the next step only if the previous was a success. On error, it short-circuits.

### 18.4 The Match pattern (used in endpoints)

```csharp
return result.Match(
    success => Results.Ok(success),
    errors => errors.ToProblemDetails());
```

This is the *only* place in the codebase where errors become HTTP responses. The extension method `ToProblemDetails()` lives in `SharedKernel.Application` and maps a `List<Error>` to RFC 7807.

### 18.5 Domain errors as constants

Don't string-literal `Error.NotFound("Session.NotFound", ...)` everywhere. Centralize:

```csharp
// Catalog.Domain/DomainErrors.cs
public static class DomainErrors
{
    public static class Session
    {
        public static readonly Error NotFound = Error.NotFound(
            "Session.NotFound", "The requested session does not exist.");

        public static readonly Error AlreadyStarted = Error.Conflict(
            "Session.AlreadyStarted", "The session has already started.");
    }

    public static class Seat
    {
        public static readonly Error Taken = Error.Conflict(
            "Seat.Taken", "One or more seats are already reserved.");
    }
}
```

Then `return DomainErrors.Session.NotFound;`. Discoverable, refactorable, consistent.

### Recommended reading
- ErrorOr repo: <https://github.com/amantinband/error-or>
- Amichai Mantinband's videos on result pattern.
- "Functional Error Handling in C#" — Vladimir Khorikov.

---

## 19. RFC 7807 ProblemDetails

### 19.1 What it is

RFC 7807 (2016) defines a JSON format for HTTP error responses:

```json
{
  "type": "https://example.com/probs/seat-taken",
  "title": "Seat already booked.",
  "status": 409,
  "detail": "Seat #A12 is already reserved by another user.",
  "instance": "/v1/reservations",
  "traceId": "00-7c8f...-01",
  "errors": {
    "SeatId": ["Seat #A12 is already reserved."]
  }
}
```

Fields:
- `type` — URI identifying the problem class (often documentation link).
- `title` — short human-readable summary.
- `status` — HTTP status.
- `detail` — specific explanation for this occurrence.
- `instance` — the URI that produced the error.
- Plus any extension fields (Eventify adds `traceId` and per-field `errors`).

### 19.2 ASP.NET Core support

`Results.Problem(...)` and `Results.ValidationProblem(...)` produce the right shape. Combined with `app.UseExceptionHandler()`, you get RFC 7807 for free.

Eventify's extension:

```csharp
public static IResult ToProblemDetails(this List<Error> errors)
{
    if (errors.Count == 0)
        return Results.Problem("An error occurred.");

    var first = errors[0];
    var status = first.Type switch
    {
        ErrorType.Validation   => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
        ErrorType.NotFound     => StatusCodes.Status404NotFound,
        ErrorType.Conflict     => StatusCodes.Status409Conflict,
        _                      => StatusCodes.Status500InternalServerError
    };

    if (first.Type == ErrorType.Validation)
    {
        var modelState = errors.GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return Results.ValidationProblem(modelState, statusCode: status);
    }

    return Results.Problem(
        title: first.Code,
        detail: first.Description,
        statusCode: status,
        type: $"https://eventify.local/problems/{first.Code}");
}
```

### Recommended reading
- RFC 7807: <https://www.rfc-editor.org/rfc/rfc7807>
- RFC 9457 (the 2023 successor): <https://www.rfc-editor.org/rfc/rfc9457>
- ASP.NET ProblemDetails docs.

---

## 20. Global exception middleware

For everything that *does* throw (bugs, infrastructure), one piece of middleware catches and converts:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
        _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);

        var (status, title) = ex switch
        {
            DomainException de  => (StatusCodes.Status422UnprocessableEntity, de.Message),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            _                   => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = _env.IsDevelopment() ? ex.ToString() : null,
            Extensions = { ["traceId"] = traceId }
        }, ct);

        return true;
    }
}
```

Registered:

```csharp
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
app.UseExceptionHandler();
```

In Production, stack traces are *not* exposed — only the traceId. Logs (with full stack) are correlated by traceId via Serilog (Section 52).

### Recommended reading
- "Handling exceptions in ASP.NET Core" — Microsoft docs.
- "IExceptionHandler in .NET 8" — Andrew Lock.

---

# Part VI — Microservices patterns

This is the deep end. Each of these patterns exists because *the moment you split one service into many, distributed-systems failure modes appear*. Read each section twice.

## 21. Database-per-service

### 21.1 The rule

Every service owns its data. No other service may query its database. Sharing is via gRPC (sync) or events (async). Period.

### 21.2 Why

- **Schema autonomy.** Catalog can change a column without coordinating with Booking.
- **Failure isolation.** Booking's DB outage doesn't take Catalog down.
- **Bounded context integrity.** A shared DB is the silent killer of microservices — every service starts joining other services' tables and the architecture collapses into a distributed monolith with extra latency.
- **Independent scaling.** Catalog can read-replicate; Booking can shard.

### 21.3 Logical vs physical separation

Eventify uses **logical** separation: six databases inside one Postgres container.

```
postgres (single container)
├── eventify_identity
├── eventify_catalog
├── eventify_booking
├── eventify_payment
├── eventify_ticket
└── eventify_notification
```

Each service has its own DB user with `GRANT` only on its own database. The container is one for dev ergonomics; in production you would have separate Postgres instances per service or use a managed multi-tenant DB.

The rule that matters: **no `SELECT FROM other_service.table` across databases**, not even with Postgres `dblink`. The temptation to "just join" is the slippery slope.

### 21.4 How services share data without sharing DBs

| Need | Mechanism |
|---|---|
| Real-time read of small data | gRPC call (Booking → Catalog.GetSessionDetails) |
| Eventually-consistent denormalized data | Subscribe to integration events, store a local copy (Ticket caches seat labels) |
| Cross-service search | Push to a dedicated search index (Iter 5 idea) |
| Reporting / analytics | Separate read-only ETL pipeline (out of scope) |

### Recommended reading
- "Microservices Patterns" — Chris Richardson, chapter 2.
- "Building Microservices, 2nd ed." — Sam Newman, chapter on data.
- Article: "Pattern: Database per service" — <https://microservices.io/patterns/data/database-per-service.html>

---

## 22. The Saga pattern

### 22.1 The problem

A booking spans Booking + Payment + Ticket + Notification. You cannot wrap them in a distributed transaction (2PC) without sacrificing availability and reasonable performance. You need eventual consistency *with* a way to compensate on failure.

A **Saga** is a sequence of local transactions where each step publishes an event/command that triggers the next step. If a step fails, *compensating* transactions roll back the earlier steps' effects (semantically, not literally — you can't "un-charge" without issuing a refund).

### 22.2 Orchestration vs Choreography

**Choreography**: each service listens to events and decides on its own what to do. No central coordinator.

```
Booking publishes BookingCreated
  → Payment consumes, charges, publishes PaymentSucceeded
    → Ticket consumes, issues tickets, publishes TicketIssued
      → Notification consumes, sends email
```

Pros: simple to add a new consumer; loosely coupled.
Cons: business flow is invisible — distributed across N services. Debugging "why did this booking fail at step 4?" requires tracing every service.

**Orchestration**: one service holds the state machine and explicitly tells others what to do.

```
BookingSaga (in Booking service)
  state: Pending
  on SeatsReserved → send ProcessPaymentCommand to Payment
  on PaymentSucceeded → publish BookingConfirmed, send IssueTicketsCommand to Ticket
  on TicketIssued → state: Confirmed
  on PaymentFailed → publish BookingCancelled, state: Failed
```

Pros: the whole flow is one readable state machine in one place; centralized timeouts and compensation.
Cons: orchestrator becomes a single point of design changes; needs careful failure handling itself.

**Eventify chooses orchestration** for Booking. Rationale: the booking flow is the heart of the app and must be inspectable for interviews/debugging. We use MassTransit Automatonymous (StateMachine) as the orchestrator.

### 22.3 The booking state machine (recap)

See ARCHITECTURE.md §7 for the full diagram. Key states:

```
Pending → AwaitingPayment → Confirmed
                          → Failed
                          → Expired
Confirmed → Refunding → Refunded
                      → RefundFailed
```

Each transition is triggered by a message (event or command). Each state has timeouts (Quartz scheduler in MassTransit).

### 22.4 Compensation

You cannot "rollback" a Stripe charge — you issue a refund. You cannot "delete" a ticket already issued — you mark it Revoked. Compensation is semantically inverse, not literally inverse.

Eventify's saga compensation flow on refund:
1. Admin triggers refund → saga moves to `Refunding`, publishes `BookingRefundRequested`.
2. Payment consumes → calls Stripe Refund API → publishes `RefundCompleted`.
3. Saga consumes → publishes `BookingCancelled`, releases the seats in Booking's own DB.
4. Ticket consumes `BookingCancelled` → revokes tickets.
5. Notification consumes → emails refund confirmation.

Every step is idempotent (you can re-deliver the same event without double-effect) and durable (the saga state lives in Booking's DB).

### Recommended reading
- "Microservices Patterns" — Chris Richardson, chapter 4. Best chapter on saga.
- "Implementing Domain-Driven Design" — Vaughn Vernon, chapter on long-running processes.
- MassTransit Saga docs: <https://masstransit.io/documentation/patterns/saga>
- Caitie McCaffrey, "Distributed Sagas" talk (YouTube).

---

## 23. Transactional Outbox

### 23.1 The dual-write problem

Naïve flow:

```csharp
db.Bookings.Add(booking);
await db.SaveChangesAsync();          // ← DB transaction committed
await bus.Publish(new BookingConfirmedIntegrationEvent(...));  // ← could fail
```

Failure modes:
- DB commit succeeds, app crashes before publish → event lost, downstream never knows the booking confirmed.
- Publish succeeds, DB commit fails → ghost event for a booking that doesn't exist.

You **cannot** atomically write to DB and RabbitMQ — they are separate systems with no shared transaction.

### 23.2 The pattern

Write the event to an **Outbox table** in the *same database* as the aggregate, inside the *same transaction*:

```sql
INSERT INTO bookings ...;
INSERT INTO outbox_messages (id, type, payload, ...);
COMMIT;
```

A separate background process polls `outbox_messages` and publishes them to RabbitMQ. After successful publish, mark the row processed (or delete it).

Guarantees:
- **At-least-once delivery** — if the publisher crashes mid-publish, the row stays unprocessed and gets retried.
- **No dual-write inconsistency** — the event is "saved" the moment the aggregate transaction commits.

Consumers must be **idempotent** to handle duplicates (Section 24).

### 23.3 MassTransit Transactional Outbox

MassTransit provides this out of the box with EF Core:

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<BookingDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h => { h.Username("guest"); h.Password("guest"); });
        cfg.ConfigureEndpoints(ctx);
    });
});
```

MassTransit creates three tables:
- `OutboxMessage` — messages waiting to publish.
- `OutboxState` — per-bus delivery state.
- `InboxState` — for idempotent consumers (Section 24).

When you call `IPublishEndpoint.Publish(...)` *inside* a DbContext scope, MassTransit intercepts and writes the message to the outbox instead of sending it directly. On `SaveChangesAsync`, the outbox row is committed with your aggregate change. A hosted service then ships it to RabbitMQ.

### 23.4 Why pre-save dispatch of domain events?

This is the same pattern but inside a single service. `PublishDomainEventsInterceptor` runs in `SavingChangesAsync`, the domain event handler writes to the outbox table — **in the same transaction** as the aggregate. If we ran post-save, the outbox write would be a separate transaction → dual-write reappears.

### Recommended reading
- "Transactional Outbox" — microservices.io: <https://microservices.io/patterns/data/transactional-outbox.html>
- "Pattern: Polling Publisher" — microservices.io.
- MassTransit outbox docs: <https://masstransit.io/documentation/patterns/transactional-outbox>
- "Designing Data-Intensive Applications" — Martin Kleppmann, chapters 7–9.

---

## 24. Inbox / idempotent consumers

### 24.1 The duplicate-delivery problem

Outbox guarantees *at least once*. RabbitMQ + MassTransit retry on failure. Both can produce duplicate deliveries — the same message arrives twice. The consumer must handle this without double-effecting (don't issue two tickets for one booking, don't charge twice).

### 24.2 Two approaches

**(a) Natural idempotency.** Use a unique constraint on the operation's effect. Example: `Tickets` table has a unique constraint on `(BookingId, SeatId)`. Inserting twice fails on the second attempt — catch the constraint violation and treat as success.

**(b) Inbox table.** Record the message ID before processing. If already recorded, skip.

```sql
CREATE TABLE inbox_state (
    message_id UUID PRIMARY KEY,
    consumer_id VARCHAR(200) NOT NULL,
    received TIMESTAMPTZ NOT NULL,
    consumed TIMESTAMPTZ
);
```

MassTransit's `InboxState` does this automatically when you call `o.UseBusOutbox()` on the consumer side.

### 24.3 Idempotency keys for HTTP

For client-driven retries (browser, mobile), use the `Idempotency-Key` HTTP header (Section 49):

```http
POST /v1/reservations
Idempotency-Key: 7f1a4b18-...

{ "sessionId": "...", "seatIds": [...] }
```

Service stores the key → (status code, response body) for 24h. Repeated requests with the same key return the cached response, even if the original retry was due to a network hiccup the client didn't see.

### Recommended reading
- "Pattern: Idempotent Consumer" — microservices.io.
- "Idempotency-Key Header" IETF draft: <https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/>
- Stripe API idempotency docs.

---

## 25. Distributed locking with RedLock

### 25.1 The problem

Two users try to reserve the same seat at the same instant. Both check availability ("free"), both insert reservation rows, and you have a double-booking.

Single-DB solution: a unique constraint on `(SessionId, SeatId, NotExpired)` — but Postgres can't express "where expired_at > now()" in a unique index without partial indexes, and even then concurrent inserts can race in the window between check and insert.

Better: serialize the *critical section* (check + insert) across all instances of the Booking service.

### 25.2 The naïve Redis lock

```
SET lock:session:{sid}:seat:{seatId} <random> NX EX 10
... do work ...
DEL lock:session:{sid}:seat:{seatId}     (only if still own it)
```

The unlock must be conditional (Lua script comparing the value) so you don't delete someone else's lock if yours expired.

### 25.3 RedLock — the algorithm

Antirez (Redis creator) proposed the **Redlock** algorithm for distributed locking across multiple Redis nodes. Acquire the lock on ≥ N/2+1 instances within a time bound. For single-node Redis (Eventify dev), simple `SET NX EX` is enough. For multi-node production, you'd use `RedLock.net` against a quorum.

```csharp
public sealed class ReservationService
{
    private readonly IDistributedLockProvider _locks;

    public async Task<ErrorOr<Reservation>> ReserveAsync(SessionId sid, IEnumerable<SeatId> seats, ...)
    {
        var resources = seats.Select(s => $"session:{sid.Value}:seat:{s.Value}").ToArray();

        await using var handle = await _locks.AcquireLockAsync(
            resources,
            expiryTime: TimeSpan.FromSeconds(10),
            waitTime: TimeSpan.FromSeconds(2),
            retryTime: TimeSpan.FromMilliseconds(200));

        if (handle is null)
            return DomainErrors.Reservation.SeatsContended;

        // Inside the lock: check availability + insert reservation atomically
        // ...
    }
}
```

### 25.4 Caveats Martin Kleppmann would point out

Distributed locks are *advisory* and assume:
- Clocks are roughly synced.
- The protected operation is shorter than the lock TTL.
- Fencing tokens prevent split-brain on lock expiration.

For Eventify, the seat-reservation critical section is sub-second, and the consequence of a rare race is "two reservations for one seat → second commit fails on a DB-level unique constraint". So distributed lock + DB constraint together is robust enough.

### Recommended reading
- Antirez, "Distributed locks with Redis": <https://redis.io/docs/manual/patterns/distributed-locks/>
- Martin Kleppmann's counter-analysis: "How to do distributed locking" (read both).
- RedLock.net: <https://github.com/samcook/RedLock.net>

---

## 26. Eventual consistency

### 26.1 What you give up

A single relational DB gives you **strong consistency**: every read sees the latest write. In a distributed system across services, that becomes impossible without distributed transactions (and you've already rejected those).

**Eventual consistency**: after a write, replicas/consumers converge to the latest state given enough time and no further writes.

### 26.2 What you must design around

- **Read-your-writes anomalies.** User confirms booking → UI redirects to "My Tickets" → ticket list is still empty for 200ms while the Ticket service catches up. Solutions: optimistic UI updates, polling with backoff, or "pending" placeholder.
- **Causal ordering not guaranteed.** `BookingConfirmed` and `TicketIssued` may arrive at Notification in either order. Either: tolerate it (consumer combines state once both are seen), or: include enough info in one event (BookingConfirmed includes ticket IDs).
- **Out-of-order delivery.** RabbitMQ guarantees order *within a single queue with single consumer*, not across queues. Don't rely on global ordering; key partitions by aggregate ID if order matters.

### 26.3 Patterns that help

- **Read models / projections.** Each service that needs another's data subscribes to events and builds its own local read model. Stale but local, fast, and survives the producer's downtime.
- **Idempotent operations.** As above.
- **Versioned events.** `BookingConfirmedV1`, `BookingConfirmedV2`. Old consumers keep working, new consumers handle new fields.
- **Schema evolution rules.** Add optional fields; never rename or remove. Use semantic versioning at the event level.

### Recommended reading
- "Designing Data-Intensive Applications" — Martin Kleppmann. *The* book on consistency.
- Pat Helland, "Life beyond Distributed Transactions" (paper, free).
- "Event-Driven Microservices" — Adam Bellemare. Excellent on event versioning.

---

# Part VII — Inter-service communication

Three protocols, each used for a specific purpose. Don't reach for the wrong one.

## 27. REST conventions

REST (Representational State Transfer, Roy Fielding 2000) is what Eventify exposes to the React SPA. It is the default for any *public* surface.

### 27.1 The maturity model (Richardson)

- **Level 0** — RPC over HTTP. One URL, POST everything.
- **Level 1** — Resources. URL per resource.
- **Level 2** — HTTP verbs and status codes. `GET /artists`, `POST /artists`, `DELETE /artists/1`.
- **Level 3** — HATEOAS (hypermedia links in responses).

Eventify aims for **Level 2**. Level 3 sounds nice but rarely pays off in practice; SPAs build their own routes.

### 27.2 Conventions

- **Nouns, plural, lowercase, kebab-case.** `/artists`, `/seat-layouts`, not `/getArtists` or `/SeatLayouts`.
- **Verbs map to operations.**
  - `GET` — read (safe, idempotent).
  - `POST` — create (or non-idempotent action).
  - `PUT` — full replacement (idempotent).
  - `PATCH` — partial update (typically with JSON Patch / merge-patch).
  - `DELETE` — remove (idempotent).
- **Status codes match the outcome.** 200 OK, 201 Created (with `Location` header), 204 No Content, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, 422 Unprocessable, 500 Internal Server Error.
- **Nested resources up to 2 levels.** `/sessions/{id}/seats` is fine; `/venues/{vId}/layouts/{lId}/sections/{sId}/seats` is too deep — make it `/sections/{sId}/seats`.
- **Filtering, sorting, paging via query string.** `?status=active&sortBy=name&page=1&pageSize=20`.

### 27.3 Content negotiation

`Content-Type: application/json` and `Accept: application/json` everywhere. Eventify doesn't support XML.

For errors, `application/problem+json` per RFC 7807.

### 27.4 OpenAPI (Swagger)

.NET 10 ships `Microsoft.AspNetCore.OpenApi` which generates an OpenAPI 3.x document from Minimal API metadata. We render it with **Scalar** instead of Swagger UI (cleaner, faster, modern).

```csharp
builder.Services.AddOpenApi();
// ...
app.MapOpenApi();
app.MapScalarApiReference();   // GET /scalar/v1 in dev
```

### Recommended reading
- "REST API Design Rulebook" — Mark Masse.
- Roy Fielding's PhD thesis (free).
- "Web API Design: The Missing Link" — Apigee (free).

---

## 28. gRPC and Protocol Buffers

### 28.1 Why gRPC for internal calls

Use gRPC for **service-to-service synchronous** calls where you control both ends. Benefits over REST/JSON:

- **Schema-first.** A `.proto` file is the source of truth. Client and server code is generated. No drift.
- **Binary protocol.** Protocol Buffers serialize tighter and parse faster than JSON.
- **HTTP/2.** Multiplexed streams; persistent connections; lower latency.
- **Streaming.** Server streaming, client streaming, bidirectional streaming — all native.

Tradeoffs:
- Not browser-friendly (gRPC-Web is a partial workaround). Don't use gRPC for SPA→Gateway traffic.
- Binary is harder to debug with curl. Use `grpcurl`.

### 28.2 A `.proto` file

```proto
syntax = "proto3";
option csharp_namespace = "Eventify.Catalog.Grpc";

package eventify.catalog.v1;

service CatalogService {
    rpc GetSessionDetails (GetSessionDetailsRequest) returns (SessionDetailsResponse);
    rpc ValidateSeats (ValidateSeatsRequest) returns (ValidateSeatsResponse);
}

message GetSessionDetailsRequest {
    string session_id = 1;
}

message SessionDetailsResponse {
    string session_id = 1;
    string event_title = 2;
    google.protobuf.Timestamp starts_at = 3;
    repeated PriceTier price_tiers = 4;
}

message PriceTier {
    string category = 1;
    string currency = 2;
    string amount = 3;          // string for decimal precision
}
```

Lives in `src/BuildingBlocks/Eventify.IntegrationContracts.Grpc/Protos/catalog.proto`.

### 28.3 Server side (Catalog)

```xml
<ItemGroup>
    <Protobuf Include="..\..\BuildingBlocks\Eventify.IntegrationContracts.Grpc\Protos\catalog.proto"
              GrpcServices="Server" Link="Protos\catalog.proto" />
</ItemGroup>
```

```csharp
// Infrastructure/Grpc/CatalogGrpcService.cs
public sealed class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly ISender _sender;
    public CatalogGrpcService(ISender sender) => _sender = sender;

    public override async Task<SessionDetailsResponse> GetSessionDetails(
        GetSessionDetailsRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(
            new GetSessionDetailsQuery(Guid.Parse(request.SessionId)),
            context.CancellationToken);

        if (result.IsError)
            throw new RpcException(new Status(StatusCode.NotFound, result.FirstError.Description));

        return result.Value.ToGrpc();
    }
}

// Program.cs
builder.Services.AddGrpc();
app.MapGrpcService<CatalogGrpcService>();
```

### 28.4 Client side (Booking)

```xml
<Protobuf Include="..\..\BuildingBlocks\Eventify.IntegrationContracts.Grpc\Protos\catalog.proto"
          GrpcServices="Client" Link="Protos\catalog.proto" />
```

```csharp
builder.Services.AddGrpcClient<CatalogService.CatalogServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["CatalogGrpc:Url"]!);
})
.AddPolicyHandler(GetRetryPolicy())     // Polly
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Usage in handler
public sealed class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand, Guid>
{
    private readonly CatalogService.CatalogServiceClient _catalog;
    // ...
    public async Task<ErrorOr<Guid>> Handle(CreateReservationCommand cmd, CancellationToken ct)
    {
        var session = await _catalog.GetSessionDetailsAsync(
            new GetSessionDetailsRequest { SessionId = cmd.SessionId.ToString() },
            cancellationToken: ct);
        // ...
    }
}
```

### 28.5 Versioning

Proto package: `eventify.catalog.v1`. Breaking changes → `eventify.catalog.v2` with a new service definition. Old service stays running for transition.

Non-breaking changes (per protobuf rules):
- Adding new fields with new tag numbers — OK.
- Removing fields → reserve the tag number so it can never be reused.
- Renaming fields → name is irrelevant on the wire (tag number is); but it's a code-level break.

### Recommended reading
- "gRPC: Up & Running" — Kasun Indrasiri.
- Protocol Buffers docs: <https://protobuf.dev/>
- "gRPC for .NET" Microsoft docs.

---

## 29. RabbitMQ and AMQP 0-9-1

### 29.1 Mental model

RabbitMQ is an AMQP 0-9-1 broker. Five concepts you must internalize:

1. **Publisher** — produces messages.
2. **Exchange** — routes messages to queues based on the exchange type and routing key.
3. **Queue** — holds messages until a consumer takes them.
4. **Binding** — a link between exchange and queue with a routing pattern.
5. **Consumer** — takes messages off a queue and processes them.

Exchange types:
- **direct** — exact-match routing key.
- **fanout** — broadcast to all bound queues.
- **topic** — pattern match on dotted keys (`orders.*.created`).
- **headers** — match on message headers.

### 29.2 Delivery guarantees

- **At-most-once** — fire-and-forget; can lose messages. Don't use.
- **At-least-once** — publisher confirms + consumer acks. Default. Can deliver duplicates → need idempotent consumers.
- **Exactly-once** — not actually achievable in distributed systems; what you call "exactly-once" in MassTransit is at-least-once + idempotent consumers via Inbox.

### 29.3 Why MassTransit

Writing RabbitMQ code directly is verbose and error-prone. **MassTransit** is a .NET abstraction that:
- Auto-creates exchanges and queues based on message types and consumer registrations.
- Provides retry, redelivery, scheduling, sagas, outbox, inbox out of the box.
- Decouples your code from RabbitMQ (you could swap to Azure Service Bus, Amazon SQS, Kafka with minimal changes).

You will work with MassTransit, not raw RabbitMQ.

### Recommended reading
- "RabbitMQ in Depth" — Gavin Roy.
- RabbitMQ tutorials (6-part, official): <https://www.rabbitmq.com/getstarted.html>
- AMQP 0-9-1 spec.

---

## 30. MassTransit

### 30.1 Send vs Publish (huge conceptual point)

- **`Publish(event)`** — broadcast to whoever is interested. Used for *integration events*. Zero or many consumers across services. Producer doesn't know who consumes.
- **`Send(command, queueAddress)`** — point-to-point. Used for *commands* in saga orchestration. Exactly one queue receives the message.

```csharp
await publishEndpoint.Publish(new BookingConfirmedIntegrationEvent(...), ct);
await sendEndpoint.Send(new ProcessPaymentCommand(...), ct);
```

Eventify uses Publish for everything *except* saga-internal commands.

### 30.2 Consumer registration

```csharp
services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<BookingConfirmedConsumer>();
    x.AddConsumer<UserRegisteredConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.UseMessageRetry(r => r.Exponential(
            5,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(ctx);
    });
});

public sealed class BookingConfirmedConsumer : IConsumer<BookingConfirmedIntegrationEvent>
{
    private readonly NotificationDbContext _db;
    public BookingConfirmedConsumer(NotificationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<BookingConfirmedIntegrationEvent> context)
    {
        var msg = context.Message;
        // ... write email job to outbox, etc.
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
```

`ConfigureEndpoints(ctx)` creates one queue per consumer, naming it `booking-confirmed-consumer` (kebab-case) and binding it to the exchange for `BookingConfirmedIntegrationEvent`.

### 30.3 Sagas (orchestration)

A saga is a state machine that persists state and reacts to events. MassTransit's `MassTransitStateMachine<TState>`:

```csharp
public sealed class BookingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public Guid ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
}

public sealed class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    public State AwaitingPayment { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<SeatsReservedIntegrationEvent> SeatsReserved { get; private set; } = null!;
    public Event<PaymentSucceededIntegrationEvent> PaymentSucceeded { get; private set; } = null!;

    public BookingStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => SeatsReserved, x => x.CorrelateById(ctx => ctx.Message.ReservationId));
        Event(() => PaymentSucceeded, x => x.CorrelateById(ctx => ctx.Message.BookingId));

        Initially(
            When(SeatsReserved)
                .Then(ctx => ctx.Saga.ReservationId = ctx.Message.ReservationId)
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentSucceeded)
                .Then(ctx => ctx.Saga.PaymentId = ctx.Message.PaymentId)
                .PublishAsync(ctx => ctx.Init<BookingConfirmedIntegrationEvent>(new { ... }))
                .TransitionTo(Confirmed));
    }
}
```

Persistence: register with EF Core:

```csharp
x.AddSagaStateMachine<BookingStateMachine, BookingState>()
 .EntityFrameworkRepository(r =>
 {
     r.ConcurrencyMode = ConcurrencyMode.Optimistic;
     r.ExistingDbContext<BookingDbContext>();
 });
```

### 30.4 Scheduling (timeouts)

```csharp
// Inside state machine
Schedule(() => ReservationExpired, x => x.ExpirationId, s =>
{
    s.Delay = TimeSpan.FromMinutes(10);
    s.Received = e => e.CorrelateById(ctx => ctx.Message.ReservationId);
});

Initially(When(SeatsReserved)
    .Schedule(ReservationExpired, ctx => ctx.Init<ReservationTimeoutMessage>(...))
    .TransitionTo(AwaitingPayment));
```

MassTransit uses RabbitMQ's delayed-message plugin or Quartz to fire the message at the scheduled time.

### Recommended reading
- MassTransit docs: <https://masstransit.io/documentation>
- Chris Patterson's YouTube channel (MassTransit creator).
- "Building Distributed Applications with MassTransit" tutorials.

---

## 31. SignalR with Redis backplane

### 31.1 What SignalR is

A real-time communication library: server pushes messages to connected clients over WebSockets (falling back to Server-Sent Events or long polling).

### 31.2 Hubs

```csharp
public sealed class SeatsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext()!.Request.RouteValues["sessionId"]!.ToString()!;
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await base.OnConnectedAsync();
    }
}

// Program.cs
app.MapHub<SeatsHub>("/hubs/seats/{sessionId}");
```

Server-to-client push from a domain event handler:

```csharp
public sealed class SeatHeldDomainEventHandler : INotificationHandler<SeatHeldDomainEvent>
{
    private readonly IHubContext<SeatsHub> _hub;
    public SeatHeldDomainEventHandler(IHubContext<SeatsHub> hub) => _hub = hub;

    public async Task Handle(SeatHeldDomainEvent e, CancellationToken ct)
    {
        await _hub.Clients.Group(e.SessionId.ToString())
            .SendAsync("SeatHeld", new { e.SeatId, e.ExpiresAt }, ct);
    }
}
```

### 31.3 Redis backplane

When you scale Booking horizontally (>1 instance), a client connected to instance A doesn't see broadcasts from instance B. The **backplane** is a pub/sub channel that all instances subscribe to: when one instance calls `SendAsync`, it publishes to Redis; every instance receives and re-broadcasts to its own clients.

```csharp
builder.Services.AddSignalR().AddStackExchangeRedis("redis:6379", o =>
{
    o.Configuration.ChannelPrefix = RedisChannel.Literal("eventify-signalr");
});
```

### 31.4 Browser side

```ts
const conn = new HubConnectionBuilder()
  .withUrl(`/hubs/seats/${sessionId}`, { accessTokenFactory: () => auth.token })
  .withAutomaticReconnect()
  .build();

conn.on("SeatHeld", ({ seatId, expiresAt }) => setSeatState(seatId, "held", expiresAt));
conn.on("SeatReleased", ({ seatId }) => setSeatState(seatId, "free"));
conn.on("SeatBooked", ({ seatId }) => setSeatState(seatId, "booked"));

await conn.start();
```

### Recommended reading
- SignalR docs: <https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction>
- "SignalR Programming in Microsoft ASP.NET" — Brennan Stehling (dated but conceptually solid).
- Andrew Lock's SignalR scaling articles.

---

## 32. YARP API Gateway

### 32.1 What a gateway does

Single entry point for the SPA. Responsibilities:

- **Routing** — `/api/catalog/*` → `catalog-api:5051`.
- **Auth** — validate JWT once at the edge, forward.
- **CORS** — single origin policy.
- **Rate limiting** — per-IP / per-user throttling.
- **TLS termination** — HTTPS to the browser, HTTP inside the cluster.
- **Aggregation** (optional) — combine multiple backend calls into one response. We don't do this in Eventify.

Without a gateway, the SPA must know every service's URL, every service must implement CORS, and rate-limiting is duplicated.

### 32.2 YARP (Yet Another Reverse Proxy)

Microsoft's modern reverse proxy library. Configuration-driven, hosted in an ASP.NET Core app, extensible with custom middleware.

```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": { "Path": "/api/catalog/{**catch-all}" },
        "Transforms": [
          { "PathRemovePrefix": "/api/catalog" },
          { "RequestHeader": "X-Forwarded-By", "Set": "eventify-gateway" }
        ]
      },
      "booking-route": {
        "ClusterId": "booking-cluster",
        "Match": { "Path": "/api/booking/{**catch-all}" },
        "Transforms": [{ "PathRemovePrefix": "/api/booking" }]
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "primary": { "Address": "http://catalog-api:5051" }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Path": "/health",
            "Interval": "00:00:10"
          }
        }
      },
      "booking-cluster": {
        "Destinations": {
          "primary": { "Address": "http://booking-api:5052" }
        }
      }
    }
  }
}
```

```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.UseAuthentication();
app.UseRateLimiter();
app.MapReverseProxy();
```

### 32.3 Why YARP over Ocelot

- Ocelot is in maintenance mode (low commit activity).
- YARP is Microsoft-supported, used by Bing/Azure.
- Better performance (built on Kestrel + System.Net.Http).
- More flexible transform pipeline.

### Recommended reading
- YARP docs: <https://microsoft.github.io/reverse-proxy/>
- "API Gateway Pattern" — microservices.io.
- "Building Microservices" — Sam Newman, gateway chapter.

---

# Part VIII — Authentication & authorization

This part is dense because identity is the most error-prone surface in any application. Read every section. The protocols are RFC-driven; the implementations are predictable once you understand the protocol.

## 33. OAuth 2.0 flows

### 33.1 The five roles

- **Resource Owner** — the human user.
- **User Agent** — the browser.
- **Client** — the app requesting access (the React SPA, an admin tool, another service).
- **Authorization Server** — issues tokens (Duende IdentityServer in Eventify).
- **Resource Server** — the API being protected (Catalog, Booking, ...).

### 33.2 The flows

OAuth 2.0 defines several grant types. Modern advice (RFC 8252, BCP 240, RFC 9700 the OAuth 2.0 Security BCP) narrows the field:

| Flow | Use for | Status |
|---|---|---|
| **Authorization Code + PKCE** | SPAs, mobile apps, any public client | **Use this** |
| **Client Credentials** | Service-to-service (no user) | **Use this** |
| Authorization Code (no PKCE) | Confidential server-side apps | Acceptable but PKCE preferred |
| Implicit | (historical for SPAs) | **Don't use** (deprecated) |
| Resource Owner Password Credentials (ROPC) | Legacy | **Don't use** |
| Device Code | TVs, CLI tools | Use when applicable |

### 33.3 Authorization Code + PKCE walkthrough

1. SPA wants to log the user in. It generates a random `code_verifier` and computes `code_challenge = SHA256(code_verifier)` base64url-encoded.
2. SPA redirects browser to `https://identity.eventify/connect/authorize?response_type=code&client_id=spa&redirect_uri=...&scope=openid+profile+eventify.read&code_challenge=...&code_challenge_method=S256&state=...&nonce=...`.
3. User logs in at Identity (or already has an SSO session). Identity prompts for consent if needed.
4. Identity redirects back: `https://spa/callback?code=<auth_code>&state=...`.
5. SPA verifies `state`, then POSTs to `https://identity.eventify/connect/token`:
   ```
   grant_type=authorization_code
   code=<auth_code>
   redirect_uri=...
   client_id=spa
   code_verifier=<original verifier>
   ```
6. Identity verifies `code_challenge == SHA256(code_verifier)`, issues `access_token`, `id_token` (OIDC), and `refresh_token`.

PKCE prevents an attacker who intercepts the auth code from exchanging it (they don't have the verifier).

### 33.4 Client Credentials walkthrough

1. Service A POSTs to `/connect/token`:
   ```
   grant_type=client_credentials
   client_id=service-a
   client_secret=<secret>
   scope=eventify.internal
   ```
2. Identity returns `access_token`. No user, no id_token.
3. Service A puts the token in `Authorization: Bearer <token>` and calls Service B.

Used in Eventify when Booking (acting as a service, not on behalf of a user) needs to call an internal API. Most internal calls in Eventify are gRPC and could skip user-bearer tokens entirely; we still issue service tokens for traceability and auditing.

### Recommended reading
- **"OAuth 2 in Action"** — Justin Richer, Antonio Sanso. *The* book.
- RFC 6749 (OAuth 2.0), RFC 7636 (PKCE), RFC 9700 (Security BCP).
- "An Illustrated Guide to OAuth and OpenID Connect" — Okta blog.

---

## 34. OpenID Connect

### 34.1 OAuth 2.0 is for authorization, OIDC is for authentication

OAuth alone says "this app may call API X on behalf of the user". It does *not* say *who* the user is. OIDC layers identity on top:

- Adds the `id_token` (a JWT with user claims).
- Adds the `userinfo` endpoint (richer profile lookup).
- Adds **discovery**: `/.well-known/openid-configuration` returns the IdP's metadata (endpoints, supported scopes, public signing keys).

### 34.2 Scopes that matter

- `openid` — required; opts you into OIDC and triggers id_token issuance.
- `profile` — name, family_name, picture.
- `email` — email, email_verified.
- `offline_access` — request a refresh_token.
- Custom scopes: `eventify.read`, `eventify.write`, `eventify.admin`, `eventify.validator`.

### 34.3 The id_token vs the access_token

- **id_token** is *for the client* (SPA). Proves who logged in. Should not be sent to APIs.
- **access_token** is *for the API*. Carries authorization. Sent in `Authorization: Bearer ...`.

Many developers conflate the two and end up sending id_tokens to APIs that then accept them — a security smell.

### Recommended reading
- OpenID Connect Core spec: <https://openid.net/specs/openid-connect-core-1_0.html>
- OIDC playground: <https://openidconnect.net/>
- "Understanding OpenID Connect" — Vittorio Bertocci on YouTube.

---

## 35. JWT structure and validation

### 35.1 Anatomy

A JWT is three base64url-encoded parts joined by dots:

```
eyJhbGciOiJSUzI1NiIsImtpZCI6IjEyMyJ9.eyJzdWIiOiJ1c2VyMTIzIiwiaWF0IjoxNzE3MjMyMDAwLCJleHAiOjE3MTcyMzU2MDB9.<signature>
```

Decoded:

```json
// Header
{ "alg": "RS256", "kid": "123", "typ": "JWT" }

// Payload (claims)
{
  "sub": "user-uuid",
  "iss": "https://identity.eventify",
  "aud": "eventify.api",
  "exp": 1717235600,
  "iat": 1717232000,
  "scope": "openid profile eventify.read",
  "role": ["Customer"]
}

// Signature: RS256 over base64url(header) + "." + base64url(payload)
```

Standard claims (RFC 7519): `iss`, `sub`, `aud`, `exp`, `nbf`, `iat`, `jti`.

### 35.2 Asymmetric signing (RS256, ES256)

The IdP holds a *private* signing key. Resource servers fetch the matching *public* key from `/.well-known/jwks.json` (referenced via the discovery doc) and verify signatures locally without ever calling Identity.

`kid` (key ID) in the header tells the verifier which key to use — supports key rotation.

### 35.3 Validation in ASP.NET Core

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://identity.eventify";       // base URL of the IdP
        o.Audience  = "eventify.api";                    // expected `aud`
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

The middleware:
1. Pulls `Authorization: Bearer <token>`.
2. Decodes header → finds `kid` → fetches/cached JWKS from `Authority/.well-known/jwks.json`.
3. Verifies signature, issuer, audience, expiry.
4. Populates `HttpContext.User` with claims.

### 35.4 Don't roll your own JWT

Use `Microsoft.AspNetCore.Authentication.JwtBearer`. Don't write base64 + HMAC code. The footguns are real (algorithm confusion, missing audience check, signature stripping).

### Recommended reading
- RFC 7519 (JWT), RFC 7515 (JWS), RFC 7517 (JWK), RFC 7518 (JWA).
- "JWT Handbook" — Auth0 (free PDF).
- "JWT Pwnage Cheatsheet" — read so you know the attacks.

---

## 36. Duende IdentityServer 7

### 36.1 What it is

A library that turns an ASP.NET Core app into a full OIDC + OAuth 2.0 authorization server. Predecessor was IdentityServer4 (free, abandoned). Duende is commercial but free for personal/non-commercial use — appropriate for Eventify.

Responsibilities:
- All OIDC/OAuth endpoints (`/connect/authorize`, `/connect/token`, `/connect/userinfo`, `/connect/endsession`).
- JWKS publication.
- Refresh-token rotation.
- Client and scope configuration.
- Consent screens.

It does **not** manage users — that's ASP.NET Core Identity's job (or any user store you bring).

### 36.2 Minimum configuration

```csharp
builder.Services.AddIdentityServer(o =>
{
    o.Events.RaiseSuccessEvents = true;
    o.Events.RaiseFailureEvents = true;
    o.EmitStaticAudienceClaim = true;
})
.AddConfigurationStore(o =>
{
    o.ConfigureDbContext = b =>
        b.UseNpgsql(connStr, sql => sql.MigrationsAssembly(migrationsAsm));
})
.AddOperationalStore(o =>
{
    o.ConfigureDbContext = b =>
        b.UseNpgsql(connStr, sql => sql.MigrationsAssembly(migrationsAsm));
})
.AddAspNetIdentity<ApplicationUser>();
```

`ConfigurationStore` persists clients, scopes, resources. `OperationalStore` persists tokens, consents, codes.

### 36.3 Clients, scopes, resources

```csharp
new Client
{
    ClientId = "eventify-spa",
    ClientName = "Eventify Web",
    AllowedGrantTypes = GrantTypes.Code,
    RequirePkce = true,
    RequireClientSecret = false,
    RedirectUris = { "https://localhost:5173/auth/callback" },
    PostLogoutRedirectUris = { "https://localhost:5173/" },
    AllowedCorsOrigins = { "https://localhost:5173" },
    AllowedScopes = { "openid", "profile", "eventify.read", "eventify.write" },
    AllowOfflineAccess = true,
    AccessTokenLifetime = 900,                 // 15 min
    RefreshTokenUsage = TokenUsage.OneTimeOnly, // rotation
    AbsoluteRefreshTokenLifetime = 604800       // 7 days
}
```

```csharp
new ApiResource("eventify.api", "Eventify Backend")
{
    Scopes = { "eventify.read", "eventify.write", "eventify.admin", "eventify.validator" }
}
```

### 36.4 Seeding

In dev you seed the configuration store at startup. In production you use the admin UI (Duende Admin or your own).

### Recommended reading
- Duende docs: <https://docs.duendesoftware.com/identityserver/v7>
- "IdentityServer Quickstarts" (still excellent).
- Dominick Baier's talks (co-creator).

---

## 37. ASP.NET Core Identity

### 37.1 What it does

`Microsoft.AspNetCore.Identity` is the user-store framework:
- `IdentityUser` (or your subclass) — the user entity.
- `UserManager<TUser>`, `SignInManager<TUser>`, `RoleManager<TRole>` — services.
- EF Core integration — `IdentityDbContext<TUser>`.
- Password hashing (PBKDF2), lockout, 2FA, email confirmation.

### 37.2 Wiring

```csharp
builder.Services
    .AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connStr))
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireUppercase = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

`ApplicationDbContext` inherits `IdentityDbContext<ApplicationUser>`. Migrations include the Identity schema.

### 37.3 Used with Duende

Duende's `AddAspNetIdentity<ApplicationUser>` wires Identity as the user store. User authentication happens in the standard ASP.NET Core login page (you build it or scaffold it); on successful sign-in, Duende issues tokens.

### Recommended reading
- "ASP.NET Core Identity" Microsoft docs.
- "Pro ASP.NET Core Identity" — Adam Freeman.

---

## 38. Policy-based authorization

### 38.1 Beyond `[Authorize]`

```csharp
[Authorize]                  // any authenticated user
[Authorize(Roles = "Admin")] // role check
[Authorize(Policy = "CanRefund")]  // policy check (preferred)
```

Policies decouple endpoint code from claim names. Define centrally:

```csharp
services.AddAuthorization(o =>
{
    o.AddPolicy("Admin", p => p.RequireRole("Admin"));
    o.AddPolicy("CanRefund", p => p.RequireClaim("permission", "refund:write"));
    o.AddPolicy("Validator", p => p.RequireRole("Validator").RequireClaim("scope", "eventify.validator"));
});

// Minimal API
app.MapPost("/v1/refunds", IssueRefund).RequireAuthorization("CanRefund");
```

### 38.2 Custom requirements

```csharp
public sealed record MinimumAgeRequirement(int Age) : IAuthorizationRequirement;

public sealed class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, MinimumAgeRequirement req)
    {
        var dob = ctx.User.FindFirst("birthdate")?.Value;
        if (dob is not null && DateTime.Parse(dob) <= DateTime.Today.AddYears(-req.Age))
            ctx.Succeed(req);
        return Task.CompletedTask;
    }
}

services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
services.AddAuthorization(o => o.AddPolicy("18+", p => p.Requirements.Add(new MinimumAgeRequirement(18))));
```

### 38.3 Resource-based authorization

When the policy depends on the *resource* (this user owns this booking), use `IAuthorizationService` inside the handler:

```csharp
var authResult = await _authService.AuthorizeAsync(User, booking, "BookingOwner");
if (!authResult.Succeeded) return Results.Forbid();
```

### Recommended reading
- "ASP.NET Core Authorization" Microsoft docs.
- Andrew Lock's authorization blog series.

---

# Part IX — Payment integration (Stripe)

## 39. Stripe domain model

You only need to know four objects for Eventify:

| Object | Purpose |
|---|---|
| **Customer** | A long-lived buyer record. Optional for one-off payments. |
| **PaymentIntent** | Represents *an intent to collect a payment*. Lifecycle: `requires_payment_method → requires_confirmation → requires_action → processing → succeeded` (or `canceled`/`failed`). |
| **PaymentMethod** | The actual card / wallet that gets charged. Created by Stripe.js in the browser; backend never sees raw card data (PCI scope minimization). |
| **Refund** | A reversal of a succeeded PaymentIntent, full or partial. |

You **never handle card numbers** server-side. Stripe.js (the JS SDK in the browser) collects the card, sends it directly to Stripe's API, and returns a `PaymentMethod` ID to your backend. You then attach that PaymentMethod to a PaymentIntent and confirm.

### Recommended reading
- Stripe docs: <https://stripe.com/docs/payments/payment-intents>
- "Stripe in Practice" — Stripe blog series.

---

## 40. PaymentIntent flow

The canonical SCA-compliant flow (Strong Customer Authentication required in EU under PSD2):

1. **Backend creates PaymentIntent.**
   ```csharp
   var service = new PaymentIntentService();
   var intent = await service.CreateAsync(new PaymentIntentCreateOptions
   {
       Amount = (long)(booking.TotalAmount.Amount * 100),  // cents
       Currency = booking.TotalAmount.Currency.ToLower(),
       Metadata = new Dictionary<string, string>
       {
           { "booking_id", booking.Id.ToString() },
           { "user_id", booking.UserId.ToString() }
       },
       AutomaticPaymentMethods = new() { Enabled = true }
   }, idempotencyKeyOptions, ct);
   ```
   Returns `client_secret` (a one-time token).
2. **Backend returns `client_secret` to SPA** in the response to `POST /payments`.
3. **SPA confirms with Stripe.js**:
   ```ts
   const stripe = await loadStripe(publishableKey);
   const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret, {
       payment_method: { card: elements.getElement(CardElement)! }
   });
   ```
   This handles 3D Secure flows automatically.
4. **Stripe sends webhook** `payment_intent.succeeded` (or `.failed`) to your backend.
5. **Backend processes webhook**, marks Payment succeeded, publishes `PaymentSucceededIntegrationEvent`, which the Booking saga consumes.

**Note**: do not rely on the SPA's `confirmCardPayment` result to mark the payment succeeded. The browser could close, lose connectivity, etc. The webhook is the source of truth.

### 40.1 Idempotency on Create

Stripe supports the `Idempotency-Key` header on POSTs. Same key → same response (cached for 24h). Use the booking ID as the key:

```csharp
var requestOptions = new RequestOptions { IdempotencyKey = $"booking-{booking.Id}" };
```

Without this, a retried `POST /payments` (e.g., timeout) creates a duplicate PaymentIntent.

### Recommended reading
- "Accept a payment" Stripe quickstart.
- "Stripe SCA Guide" — Stripe docs.

---

## 41. Webhook signing and idempotency

### 41.1 Signature verification (mandatory)

Stripe signs every webhook with a secret (`whsec_...`). Verify in the controller — *never* trust an unverified webhook:

```csharp
app.MapPost("/webhooks/stripe", async (HttpRequest req, IOptions<StripeOptions> opt, IPaymentWebhookProcessor proc) =>
{
    using var reader = new StreamReader(req.Body);
    var json = await reader.ReadToEndAsync();
    var sigHeader = req.Headers["Stripe-Signature"].ToString();

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(json, sigHeader, opt.Value.WebhookSecret);
    }
    catch (StripeException ex)
    {
        return Results.BadRequest($"Invalid signature: {ex.Message}");
    }

    await proc.ProcessAsync(stripeEvent);
    return Results.Ok();
});
```

### 41.2 Webhook idempotency

Stripe retries webhooks until they get a 2xx response. The same `event.id` can arrive multiple times. Dedup:

```csharp
public sealed class StripeWebhookProcessor
{
    private readonly PaymentDbContext _db;

    public async Task ProcessAsync(Event stripeEvent)
    {
        var exists = await _db.WebhookEvents.AnyAsync(w => w.Id == stripeEvent.Id);
        if (exists) return;        // already processed; ack OK

        _db.WebhookEvents.Add(new StripeWebhookEvent
        {
            Id = stripeEvent.Id,
            Type = stripeEvent.Type,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        switch (stripeEvent.Type)
        {
            case Events.PaymentIntentSucceeded:
                var pi = (PaymentIntent)stripeEvent.Data.Object;
                await HandleSucceededAsync(pi);
                break;
            case Events.PaymentIntentPaymentFailed:
                await HandleFailedAsync((PaymentIntent)stripeEvent.Data.Object);
                break;
        }

        await _db.SaveChangesAsync();
    }
}
```

The dedup row + state change + integration-event-to-outbox all live in one transaction. Belongs to the **Inbox** pattern (Section 24).

### 41.3 Local development

Use the Stripe CLI:

```bash
stripe listen --forward-to https://localhost:5053/webhooks/stripe
```

Prints a `whsec_...` secret for local use. Allows testing the full flow with `stripe trigger payment_intent.succeeded`.

### Recommended reading
- "Webhooks" Stripe guide.
- "Best practices for using webhooks" Stripe docs.
- Stripe.NET repo and samples.

---

# Part X — EF Core advanced

You read EF Core basics in Section 3. This part covers the patterns Eventify specifically uses.

## 42. ISaveChangesInterceptor

### 42.1 The hook

`ISaveChangesInterceptor` lets you intercept `SaveChanges`/`SaveChangesAsync` *before* and *after* the call to the underlying database. Methods:

- `SavingChangesAsync(DbContextEventData, InterceptionResult<int>, CT)` — pre-save.
- `SavedChangesAsync(SaveChangesCompletedEventData, int, CT)` — post-save.
- `SaveChangesFailedAsync(...)` — on exception.

Eventify uses two interceptors registered on every service `DbContext`.

### 42.2 UpdateAuditableInterceptor

```csharp
public sealed class UpdateAuditableInterceptor : SaveChangesInterceptor
{
    private readonly IClock _clock;

    public UpdateAuditableInterceptor(IClock clock) => _clock = clock;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ((IAuditable)entry.Entity).CreatedAt = now;
                    break;
                case EntityState.Modified:
                    ((IAuditable)entry.Entity).UpdatedAt = now;
                    break;
            }
        }

        // Also catch owned entities with changed state — they don't appear as IAuditable directly.
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

`IAuditable` is *internal* to SharedKernel, so application code can never set audit fields directly — only the interceptor can.

### 42.3 PublishDomainEventsInterceptor (pre-save, critical detail)

```csharp
public sealed class PublishDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public PublishDomainEventsInterceptor(IPublisher publisher) => _publisher = publisher;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return result;

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IClearableAggregate>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        // Materialize ALL events BEFORE clearing — AsReadOnly() is a live wrapper, not a copy.
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var a in aggregates)
            a.ClearDomainEvents();

        foreach (var e in events)
            await _publisher.Publish(e, cancellationToken);

        return result;
    }
}
```

Why pre-save:
1. EF Core detaches `Deleted` entries from the ChangeTracker after commit, so any `*DeletedDomainEvent` would never be dispatched if we ran post-save.
2. Handlers that write integration events to the Outbox table must execute *inside* the same transaction. Pre-save guarantees the publisher → handler → outbox-row writes all happen before the final `COMMIT`.

### 42.4 Registration

```csharp
services.AddScoped<UpdateAuditableInterceptor>();
services.AddScoped<PublishDomainEventsInterceptor>();

services.AddDbContext<CatalogDbContext>((sp, opt) =>
{
    opt.UseNpgsql(connStr);
    opt.AddInterceptors(
        sp.GetRequiredService<UpdateAuditableInterceptor>(),
        sp.GetRequiredService<PublishDomainEventsInterceptor>());
});
```

### Recommended reading
- "EF Core Interceptors" Microsoft docs.
- "Implementing the Outbox Pattern with EF Core" — Milan Jovanović.

---

## 43. Value converters and strongly-typed IDs

### 43.1 The mechanism

A `ValueConverter<TModel, TProvider>` tells EF Core how to translate a CLR type to/from a database type.

```csharp
public sealed class ArtistIdConverter : ValueConverter<ArtistId, Guid>
{
    public ArtistIdConverter()
        : base(id => id.Value, value => new ArtistId(value)) { }
}
```

### 43.2 Per-property

```csharp
builder.Property(a => a.Id).HasConversion<ArtistIdConverter>();
```

### 43.3 Globally per type

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<ArtistId>().HaveConversion<ArtistIdConverter>();
    configurationBuilder.Properties<EventId>().HaveConversion<EventIdConverter>();
    // ... etc
}
```

For Eventify's many strongly-typed IDs, this avoids duplicating per-property configuration.

### 43.4 Foreign keys also need it

If `Event` holds `ArtistId ArtistId`, the FK column needs the same converter. EF Core handles this automatically if the property type is registered globally.

### Recommended reading
- "Value Conversions" EF Core docs.
- "Strongly Typed Ids with EF Core" — Andrew Lock.

---

## 44. Owned entities (Money)

### 44.1 The pattern

An owned entity is part of another entity's lifecycle but has no independent identity. EF Core flattens it into the parent's table.

```csharp
public sealed class PriceTier : Entity<PriceTierId>
{
    public SectionCategory Category { get; private set; }
    public Money Price { get; private set; }   // value object

    // ...
}

// Configuration
builder.OwnsOne(p => p.Price, m =>
{
    m.Property(x => x.Amount).HasColumnName("price_amount").HasColumnType("numeric(18,2)");
    m.Property(x => x.Currency).HasColumnName("price_currency").HasMaxLength(3);
});
```

Resulting table:

```sql
CREATE TABLE price_tiers (
    id uuid PRIMARY KEY,
    category int NOT NULL,
    price_amount numeric(18,2) NOT NULL,
    price_currency varchar(3) NOT NULL,
    -- ...
);
```

No separate `prices` table. The Money VO is just two columns on the PriceTier row.

### 44.2 When to use owned vs separate entity

- **Owned** when the value has no identity, no lifecycle, and is always tied to one parent (Money, Address, DateRange).
- **Separate entity** when it has identity, lifecycle, or is referenced by multiple parents.

### Recommended reading
- "Owned Entity Types" EF Core docs.
- "Domain-Driven Design Distilled" — Vernon, chapter on value objects.

---

## 45. Migrations strategy

### 45.1 One migration per change

When you change the model, generate a migration with a descriptive name:

```bash
dotnet ef migrations add AddPriceTiers --project Infrastructure --startup-project Api
```

The migration file is C# code that applies the diff. **Always read it** — EF Core occasionally produces destructive SQL when subtler `ALTER` would do.

### 45.2 Naming convention

Eventify uses `YYYY_MM_DD_HHMM_DescriptiveName.cs`. Lexicographic order = chronological order.

### 45.3 Applying in dev

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
await db.Database.MigrateAsync();
```

### 45.4 Applying in K8s

Don't migrate on container startup in production. Reason: a slow migration delays pod readiness; worse, multiple pods can race. Use a Kubernetes `Job` with `initContainers` pattern:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: catalog-migrations
spec:
  template:
    spec:
      restartPolicy: Never
      containers:
      - name: migrate
        image: eventify-catalog:latest
        command: ["dotnet", "Eventify.Catalog.Api.dll", "--migrate-only"]
```

Or generate idempotent SQL with `dotnet ef migrations script --idempotent` and apply it manually.

### 45.5 Don't edit applied migrations

Once a migration is on a teammate's DB (or CI), do not modify it. Generate a new migration to correct.

### Recommended reading
- "Managing Migrations" EF Core docs.
- "Database Migrations with EF Core" — Tim Corey YouTube.

---

## 46. Concurrency control

### 46.1 Optimistic concurrency

When two users edit the same row simultaneously, EF Core can detect the conflict using a *concurrency token*. Add a `rowversion` (Postgres: `xmin` or a manual `int` version column):

```csharp
public sealed class Reservation : AggregateRoot<ReservationId>
{
    public uint Version { get; private set; }
    // ...
}

builder.Property(r => r.Version).IsRowVersion();
```

On UPDATE, EF Core adds `WHERE id = ? AND version = ?`. If zero rows affected, it throws `DbUpdateConcurrencyException`.

### 46.2 Saga concurrency

MassTransit saga repositories support optimistic or pessimistic concurrency:

```csharp
.EntityFrameworkRepository(r =>
{
    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
    r.ExistingDbContext<BookingDbContext>();
});
```

Optimistic + retry is usually correct.

### 46.3 Postgres `xmin` as concurrency token

Postgres has a built-in system column `xmin` (transaction ID of the row's last writer). EF Core 7+ supports it:

```csharp
builder.UseXminAsConcurrencyToken();
```

No need for an explicit version column.

### Recommended reading
- "Handling Concurrency Conflicts" EF Core docs.
- "Postgres MVCC" official docs.

---

# Part XI — API design

## 47. URL-segment API versioning

### 47.1 Three common strategies

| Strategy | Example | Pros | Cons |
|---|---|---|---|
| **URL segment** | `/v1/artists` | Visible, cacheable, browser-friendly | Path proliferation |
| Header | `X-API-Version: 1` | Clean URLs | Hidden from logs, harder to test in browser |
| Media type | `Accept: application/vnd.eventify.v1+json` | "Pure REST" | Cumbersome; nobody loves it |

Eventify uses **URL segment** for visibility and simplicity. Stripe, GitHub, Twilio all use this.

### 47.2 Setup with Asp.Versioning.Http

```csharp
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
    o.ReportApiVersions = true;
})
.AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

// In Carter module
app.MapGroup("/v{version:apiVersion}/artists")
   .HasApiVersion(new ApiVersion(1, 0));
```

### 47.3 Deprecation

When you bump to `/v2`, mark `/v1` deprecated:

```csharp
.HasApiVersion(new ApiVersion(1, 0))
.HasDeprecatedApiVersion(new ApiVersion(1, 0));
```

This adds `api-supported-versions: 2.0` and `api-deprecated-versions: 1.0` headers, plus the RFC 8594 `Sunset` and `Deprecation` headers if you configure them. Keep `v1` running for 90 days, then remove.

### 47.4 When to bump

- **Breaking** (rename a field, change response shape, change semantics) → new version.
- **Non-breaking** (add an optional field, add a new endpoint, add an enum value if clients ignore unknown values) → stay in current version.

Lean toward not bumping. Each version doubles your maintenance surface.

### Recommended reading
- "API Versioning" Microsoft docs.
- "Building Evolvable Web APIs with ASP.NET" — Glenn Block.

---

## 48. Pagination strategies

### 48.1 Offset pagination (Eventify's default)

```
GET /v1/events?page=1&pageSize=20&sortBy=startsAt&sortDir=asc
```

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public static async Task<PagedResult<TDto>> ToPagedAsync<TEntity, TDto>(
    this IQueryable<TEntity> source,
    int page, int pageSize,
    Expression<Func<TEntity, TDto>> projection,
    CancellationToken ct)
{
    var total = await source.CountAsync(ct);
    var items = await source
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(projection)
        .ToListAsync(ct);

    return new PagedResult<TDto>(
        items, page, pageSize, total,
        (int)Math.Ceiling(total / (double)pageSize));
}
```

Pros: simple, familiar.
Cons: `COUNT(*)` cost grows with table size; "page drift" — if new rows are added between page reads, items shift.

### 48.2 Cursor (keyset) pagination

```
GET /v1/events?after=eyJpZCI6Ii4uLiJ9&limit=20
```

Instead of `OFFSET`, you remember the last seen sort key and ask for "items after this key":

```sql
SELECT * FROM events
WHERE (starts_at, id) > (:lastStartsAt, :lastId)
ORDER BY starts_at, id
LIMIT 20;
```

Pros: O(log n) regardless of page depth; no drift; cache-friendly.
Cons: no random-access to page N; total count is hard.

Eventify uses **offset** for MVP simplicity and may add cursor for high-volume admin endpoints later.

### 48.3 Conventions

- `pageSize` defaults to 20, max 100 (server-enforced).
- `sortBy` whitelisted per endpoint (`["startsAt", "title"]`) — never accept arbitrary fields (SQL injection adjacent).
- Always include `TotalCount` and `TotalPages` so the SPA can render pagers.

### Recommended reading
- "Pagination Strategies" — Use The Index, Luke.
- "Keyset Pagination" — Markus Winand.

---

## 49. The Idempotency-Key header

### 49.1 Why

`POST /reservations` is *not* idempotent — calling it twice creates two reservations. But the network is unreliable: a client may retry a POST after a timeout, not knowing whether the original landed.

Solution: client generates a unique key (UUID) per logical operation and sends it in `Idempotency-Key`. Server stores `key → response` and returns the cached response on retry.

### 49.2 Implementation sketch

```csharp
public sealed class IdempotencyKey
{
    public string Key { get; init; } = null!;
    public string RequestHash { get; init; } = null!;
    public int ResponseStatus { get; init; }
    public string ResponseBody { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ApplicationDbContext db)
    {
        if (ctx.Request.Method != "POST" ||
            !ctx.Request.Headers.TryGetValue("Idempotency-Key", out var keyValue))
        {
            await _next(ctx);
            return;
        }

        var key = keyValue.ToString();
        var existing = await db.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key);
        if (existing is not null)
        {
            ctx.Response.StatusCode = existing.ResponseStatus;
            await ctx.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        // Capture response
        var originalBody = ctx.Response.Body;
        using var memStream = new MemoryStream();
        ctx.Response.Body = memStream;

        await _next(ctx);

        memStream.Position = 0;
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();

        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            RequestHash = await HashRequestAsync(ctx.Request),
            ResponseStatus = ctx.Response.StatusCode,
            ResponseBody = responseBody,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody);
    }
}
```

Background job sweeps keys older than 24h.

### 49.3 What counts as the "same" request

Compare request body hashes. If the client reuses the same key with a *different* body, return 422 — that's a programming error on the client side, not a legitimate retry.

### Recommended reading
- IETF draft: <https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/>
- Stripe API idempotency docs.

---

# Part XII — Endpoint composition and mapping

## 50. Carter modules

### 50.1 The problem Carter solves

Minimal APIs in `Program.cs` get unwieldy fast:

```csharp
app.MapGet("/v1/artists/{id:guid}", ...);
app.MapPost("/v1/artists", ...);
app.MapPut("/v1/artists/{id:guid}", ...);
// ... 50 more lines per aggregate
```

Carter introduces `ICarterModule` — one class per logical grouping (per aggregate in Eventify):

```csharp
public sealed class ArtistsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/artists")
            .WithTags("Artists")
            .RequireAuthorization();

        group.MapGet("/", ListArtists)
            .AllowAnonymous()
            .Produces<PagedResult<ArtistDto>>()
            .WithName("ListArtists");

        group.MapGet("/{id:guid}", GetArtist)
            .AllowAnonymous()
            .Produces<ArtistDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateArtist)
            .RequireAuthorization("Admin")
            .Produces<Guid>(StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListArtists(
        int page, int pageSize, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ListArtistsQuery(page, pageSize), ct);
        return result.Match(Results.Ok, errs => errs.ToProblemDetails());
    }
}
```

Carter auto-discovers all `ICarterModule` classes and calls `AddRoutes` on each.

### 50.2 Wiring

```csharp
builder.Services.AddCarter();
app.MapCarter();
```

### 50.3 Why not Controllers

- Less ceremony (no class inheritance, no `[ApiController]` attribute).
- Faster runtime (skips MVC pipeline).
- Endpoint metadata is colocated (`Produces<>`, `RequireAuthorization`, etc.).
- Matches the "thin endpoint" idiom — there is nowhere to hide business logic.

### 50.4 Why not FastEndpoints

FastEndpoints encourages putting handler logic in the endpoint class itself. In Eventify, that conflicts with MediatR — we'd have two handlers (the endpoint and the MediatR handler) for the same operation. Carter stays minimal: it routes, MediatR handles.

### Recommended reading
- Carter repo: <https://github.com/CarterCommunity/Carter>
- "Why Carter?" — Jonathan Channon (creator) talks.

---

## 51. Manual mapping vs mapper libraries

### 51.1 The verdict

Eventify uses **manual mapping** via static extension methods. No AutoMapper, no Mapster, no Mapperly.

### 51.2 The convention

```csharp
public sealed record ArtistDto(Guid Id, string Name, string? Bio, string? ImageUrl);

public static class ArtistMappingExtensions
{
    public static ArtistDto ToDto(this Artist artist) =>
        new(artist.Id.Value, artist.Name, artist.Bio, artist.ImageUrl);

    public static ArtistListItemDto ToListItemDto(this Artist artist) =>
        new(artist.Id.Value, artist.Name);

    public static IReadOnlyList<ArtistDto> ToDtoList(this IEnumerable<Artist> artists) =>
        artists.Select(a => a.ToDto()).ToList();
}
```

Place the mapping next to the DTO. Anyone reading the DTO sees the projection without searching.

### 51.3 For request to command

```csharp
public sealed record CreateArtistRequest(string Name, string? Bio, string? ImageUrl)
{
    public CreateArtistCommand ToCommand() => new(Name, Bio, ImageUrl);
}
```

### 51.4 For domain to integration event

```csharp
public static class BookingMappingExtensions
{
    public static BookingConfirmedIntegrationEvent ToIntegrationEvent(this Booking b) =>
        new(b.Id.Value, b.UserId.Value, b.SessionId.Value,
            b.TotalAmount.Amount, b.TotalAmount.Currency,
            b.SeatIds.Select(s => s.Value).ToList());
}
```

### 51.5 Why not a library

| Concern | AutoMapper | Manual |
|---|---|---|
| Renames track | Reflection-based, breaks silently | Compile error, find-references works |
| Runtime cost | Reflection + expression compilation | Direct call |
| Hidden config | `MappingProfile` somewhere else | Inline with DTO |
| Debugger | Maps through generated code | Step into directly |
| Licensing | AutoMapper went commercial in v13 | Free |

For 5–10-property aggregates, the cost of a mapper library exceeds the boilerplate it saves. Mapperly (source generator) is the strongest alternative; Eventify still chooses manual to fit the "no magic" preference.

### Recommended reading
- Jimmy Bogard (AutoMapper author), "AutoMapper's design philosophy" — and his own caveats.
- "Why I don't use AutoMapper" — Tim Deschryver.

---

# Part XIII — Observability

You will spend more time debugging a distributed system in production than writing it. Observability is the difference between "the booking failed somewhere" and "the booking failed because the payment webhook arrived 12s late and the saga timed out". Without it, microservices are unmaintainable.

The three pillars: **logs**, **metrics**, **traces**. A fourth practical concern: **health checks**.

## 52. Structured logging with Serilog

### 52.1 Why structured

`Console.WriteLine($"User {id} bought {n} tickets for {total}")` produces a string. Greppable, but only by humans. You can't query "show me all log entries where `n > 10`" without parsing the string.

Structured logs preserve the *parameters* as separate fields:

```json
{
  "@t": "2026-05-17T10:30:15Z",
  "@l": "Information",
  "@m": "User a3f-... bought 4 tickets for 320.00 USD",
  "@mt": "User {UserId} bought {SeatCount} tickets for {Amount} {Currency}",
  "UserId": "a3f-...",
  "SeatCount": 4,
  "Amount": 320.00,
  "Currency": "USD",
  "TraceId": "00-7c8f...-01",
  "Service": "booking"
}
```

Now you can query "all logs where `SeatCount > 10` in service `booking`" in Seq / Elastic / Datadog with one click.

### 52.2 Serilog basics

```csharp
builder.Host.UseSerilog((ctx, sp, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(sp)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "booking")
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
    .WriteTo.Console(formatter: new CompactJsonFormatter())
    .WriteTo.Seq("http://seq:5341"));
```

Inside code, use the standard `ILogger<T>` abstraction (Serilog plugs in as the provider):

```csharp
public sealed class ReservationsService
{
    private readonly ILogger<ReservationsService> _logger;

    public ReservationsService(ILogger<ReservationsService> logger) => _logger = logger;

    public async Task<ErrorOr<ReservationDto>> ReserveAsync(ReserveCommand cmd)
    {
        _logger.LogInformation(
            "Reserving {SeatCount} seats for user {UserId} in session {SessionId}",
            cmd.SeatIds.Count, cmd.UserId, cmd.SessionId);

        // ...

        _logger.LogWarning("Seat {SeatId} contended; retrying", seatId);
        _logger.LogError(ex, "Reservation failed for session {SessionId}", cmd.SessionId);
    }
}
```

**Always use message templates** with `{NamedParameters}`. Never string-interpolate (`$"..."`) — that destroys the structured properties.

### 52.3 Log levels

| Level | When |
|---|---|
| Trace | Per-step diagnostics during development |
| Debug | Detailed flow info, off in production |
| Information | Notable business events ("reservation created", "payment succeeded") |
| Warning | Recoverable conditions (retry succeeded, transient timeout) |
| Error | Operation failed; user/operator visible |
| Critical | System-level failure (DB unavailable) |

Defaults: Information in production, Debug in dev. Override per-namespace in `appsettings.json`.

### 52.4 Correlation across requests

`Enrich.FromLogContext()` lets you push properties into the ambient log context:

```csharp
using (_logger.BeginScope(new Dictionary<string, object> { ["BookingId"] = bookingId }))
{
    _logger.LogInformation("Confirming");
    // every log inside this scope carries BookingId
}
```

OpenTelemetry (next section) adds `TraceId` and `SpanId` automatically — these are how you correlate one user action across all six services.

### 52.5 Centralized sink: Seq

[Seq](https://datalust.co/seq) is a structured-log server with a powerful query language. In Eventify Iter 2 we add it via `docker-compose.observability.yml` and point Serilog at `http://seq:5341`. Query:

```
@Level = 'Error' and Service = 'booking' and BookingId = '...'
```

### Recommended reading
- "Serilog" official docs and `serilog/serilog` GitHub.
- Nicholas Blumhardt's blog (Serilog author).
- "Logging in .NET" — Andrew Lock series.

---

## 53. OpenTelemetry — traces, metrics, logs

### 53.1 What OTel is

A vendor-neutral standard for telemetry: traces (distributed call chains), metrics (numeric time series), and logs. Replaces older proprietary APIs (Application Insights SDK, Jaeger client, Prometheus client).

The three components:
- **API** — what application code uses (`ActivitySource.StartActivity()`, `Meter.CreateCounter()`).
- **SDK** — collects, batches, samples, exports.
- **Exporter** — protocol-specific output (Jaeger, Zipkin, Prometheus, OTLP, console).

### 53.2 Distributed tracing

A **trace** is the story of one request, made up of **spans**. Each span is one unit of work (HTTP request, DB query, message handler) with a start, end, attributes, and parent span. A `TraceId` ties all spans together; each span has a `SpanId`.

Trace propagation across services uses the **W3C Trace Context** standard (HTTP headers `traceparent` and `tracestate`). Every modern .NET client/server respects this automatically when OTel is enabled.

Example trace for a booking confirmation:

```
trace abc-123 (total 450ms)
├─ HTTP POST /v1/payments [Gateway] (450ms)
│  └─ HTTP POST /v1/payments [Payment.Api] (440ms)
│     ├─ POST /v1/payment_intents [Stripe] (180ms)
│     ├─ INSERT payments [Postgres] (8ms)
│     └─ Publish PaymentSucceeded [RabbitMQ] (5ms)
│        └─ Consume PaymentSucceeded [Booking.Saga] (35ms)
│           ├─ UPDATE booking_state [Postgres] (10ms)
│           └─ Send IssueTickets [RabbitMQ] (4ms)
│              └─ Consume IssueTickets [Ticket.Api] (75ms)
│                 ├─ gRPC GetSessionDetails [Catalog.Grpc] (20ms)
│                 └─ INSERT tickets [Postgres] (40ms)
```

Open Jaeger, find the trace, see the whole story in one view. This is *the* superpower of distributed tracing.

### 53.3 .NET setup

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "booking",
        serviceVersion: "1.0",
        serviceInstanceId: Environment.MachineName))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
        .AddNpgsql()
        .AddSource("MassTransit")     // MassTransit emits OTel activities natively
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://jaeger:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint();    // GET /metrics
```

`AddAspNetCoreInstrumentation` automatically creates a span for every incoming HTTP request. `AddHttpClientInstrumentation` for every outgoing HTTP call. EF Core, gRPC, MassTransit, RabbitMQ — all auto-instrumented.

You only need custom spans for genuinely interesting work units that aren't already traced:

```csharp
private static readonly ActivitySource ActivitySource = new("Eventify.Booking");

public async Task<Reservation> ReserveAsync(...)
{
    using var activity = ActivitySource.StartActivity("ReserveSeats");
    activity?.SetTag("session.id", sessionId.ToString());
    activity?.SetTag("seat.count", seats.Count);

    // ... work ...
}
```

### 53.4 Metrics

The `System.Diagnostics.Metrics` API:

```csharp
private static readonly Meter Meter = new("Eventify.Booking");
private static readonly Counter<int> ReservationsCounter =
    Meter.CreateCounter<int>("eventify.reservations.created", "count", "Number of reservations created");
private static readonly Histogram<double> ReservationLatency =
    Meter.CreateHistogram<double>("eventify.reservations.latency", "ms", "Time to create reservation");

public async Task ReserveAsync(...)
{
    var sw = Stopwatch.StartNew();
    try
    {
        // ...
        ReservationsCounter.Add(1, KeyValuePair.Create<string, object?>("status", "success"));
    }
    finally
    {
        ReservationLatency.Record(sw.Elapsed.TotalMilliseconds);
    }
}
```

Prometheus scrapes `/metrics`, stores time series, Grafana queries and graphs. Standard dashboards: request rate, p50/p95/p99 latency, error rate per endpoint, GC, thread pool, working set.

### 53.5 Sampling

In production you can't store 100% of traces (cost, volume). Strategies:
- **Head-based sampling**: decide at the start (1% random).
- **Tail-based sampling**: decide at the end, prefer traces with errors or high latency. Requires a collector like the OpenTelemetry Collector.

Eventify dev: 100% sampling (it's a learning project). Production: head-based 10% with always-on for errors.

### Recommended reading
- "Observability Engineering" — Charity Majors, Liz Fong-Jones, George Miranda. The book.
- OpenTelemetry docs: <https://opentelemetry.io/docs/languages/net/>
- "Distributed Tracing in Practice" — Austin Parker.
- Honeycomb's blog (Charity Majors).

---

## 54. Health checks

### 54.1 The contract

A service exposes an HTTP endpoint that returns 200 if healthy, 503 if not. Kubernetes uses this for **liveness** (restart unhealthy pods) and **readiness** (route traffic only to ready pods). Load balancers, monitoring, and dashboards consume it.

### 54.2 ASP.NET Core setup

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connStr, name: "postgres", tags: new[] { "db", "ready" })
    .AddRabbitMQ(rabbitConnFactory, name: "rabbit", tags: new[] { "bus", "ready" })
    .AddRedis(redisConnStr, name: "redis", tags: new[] { "cache" })
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 54.3 Liveness vs Readiness — the distinction matters

- **Liveness** answers "is the process alive?". If liveness fails, K8s kills the pod and starts a new one. Should *not* depend on DB connectivity — if Postgres is down, restarting every booking pod won't help and may cause a thundering herd.
- **Readiness** answers "can the process serve traffic right now?". If readiness fails, K8s stops routing requests to this pod (but doesn't kill it). Should include DB and downstream dependencies.

Liveness should be a trivial "I'm here". Readiness should reflect real ability to serve.

### 54.4 HealthChecks UI

`AspNetCore.HealthChecks.UI` aggregates health from all services into one dashboard. Useful in Iter 4+ for local + K8s demos.

### Recommended reading
- "Health checks in ASP.NET Core" Microsoft docs.
- "Kubernetes Probes" — Kubernetes docs (liveness/readiness/startup).

---

# Part XIV — Resilience

## 55. Polly v8 and Microsoft.Extensions.Http.Resilience

### 55.1 The problem

Networks fail. Services restart. Databases hiccup. A naïve `await httpClient.GetAsync(url)` throws on the first transient blip, propagates a 500 to the user, and pages someone at 3am.

**Resilience policies** wrap calls in patterns that survive transient failure:

| Policy | What it does |
|---|---|
| **Retry** | Re-attempt after a delay (with backoff + jitter) on transient errors |
| **Circuit Breaker** | After N consecutive failures, stop trying for a period (let the downstream recover) |
| **Timeout** | Cap the wait time per call |
| **Bulkhead** | Limit concurrent calls to a downstream (isolate failure) |
| **Fallback** | Provide a default response when all else fails |
| **Hedging** | Send a parallel request if the first is slow; use whichever returns first |

Polly composes these into a **resilience pipeline**.

### 55.2 Polly v8 — the modern API

Polly v8 (2023) rewrote the API around `ResiliencePipeline<T>`:

```csharp
var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => r.StatusCode >= HttpStatusCode.InternalServerError),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    })
    .Build();

var response = await pipeline.ExecuteAsync(async ct => await httpClient.GetAsync(url, ct));
```

### 55.3 The .NET 8+ shortcut: standard resilience handler

`Microsoft.Extensions.Http.Resilience` ships a one-call setup that adds a sensible default pipeline to any `HttpClient`:

```csharp
services.AddHttpClient<ICatalogClient, CatalogClient>(c =>
{
    c.BaseAddress = new Uri(config["Catalog:Url"]!);
})
.AddStandardResilienceHandler();
```

Standard handler = total request timeout + retry (3 attempts, exponential backoff, jitter) + circuit breaker + per-attempt timeout. Tunable via options.

For gRPC clients:

```csharp
services.AddGrpcClient<CatalogService.CatalogServiceClient>(o =>
{
    o.Address = new Uri(config["CatalogGrpc:Url"]!);
})
.AddStandardResilienceHandler();
```

### 55.4 What you should *not* retry

- **Non-idempotent operations.** Retrying `POST /payments` without an idempotency key creates duplicate charges. Combine with `Idempotency-Key` (Section 49) before enabling retries.
- **4xx errors.** A `400 Bad Request` will be 400 next time too. Retry only 5xx, timeouts, and network exceptions.
- **Saga-internal MassTransit messages.** MassTransit has its own retry policies tuned for the bus. Don't double-retry.

### 55.5 Circuit-breaker mental model

States:
- **Closed** — normal; requests flow.
- **Open** — too many failures; reject immediately for `BreakDuration`.
- **Half-Open** — after duration, allow a few test requests. If they succeed, close. If not, re-open.

A circuit breaker protects you *and the downstream*. Hammering a failing service prolongs its recovery; backing off lets it heal.

### Recommended reading
- Polly v8 docs: <https://www.pollydocs.org/>
- "Microsoft.Extensions.Http.Resilience" — Microsoft docs.
- "Release It! 2nd ed." — Michael Nygard. The book on resilience patterns; coined "circuit breaker" and "bulkhead" in this domain.

---

# Part XV — Testing

Eventify enforces a strict pyramid: many unit tests, fewer integration tests, very few E2E tests. Architecture tests sit alongside as compile-time-style guarantees.

## 56. xUnit, FluentAssertions, Moq

### 56.1 xUnit

The .NET testing framework Eventify uses. Tests are methods marked `[Fact]` (no parameters) or `[Theory]` + `[InlineData(...)]` / `[MemberData(...)]` (parameterized).

```csharp
public sealed class ReservationTests
{
    [Fact]
    public void Create_NewReservation_RaisesReservationCreatedEvent()
    {
        // Arrange
        var clock = new FakeClock(DateTimeOffset.Parse("2026-05-17T10:00:00Z"));

        // Act
        var reservation = Reservation.Create(
            new UserId(Guid.NewGuid()),
            new SessionId(Guid.NewGuid()),
            TimeSpan.FromMinutes(10),
            clock);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Pending);
        reservation.ExpiresAt.Should().Be(DateTimeOffset.Parse("2026-05-17T10:10:00Z"));
        reservation.DomainEvents.Should().ContainSingle(e => e is ReservationCreatedDomainEvent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddSeat_AfterStatusChanges_Throws(int statusOffset)
    {
        // ...
    }
}
```

Tests in one class share construction via the constructor (no `[SetUp]`); cleanup via `IDisposable`/`IAsyncDisposable`. Shared expensive state across tests in a class collection uses `IClassFixture<T>`.

### 56.2 Arrange / Act / Assert

The structure of every test. Three sections, separated by blank lines, with an explanatory comment if needed. One assertion per concept (you can have multiple `Should()` calls verifying one concept).

### 56.3 FluentAssertions

Readable, message-rich assertion library:

```csharp
result.Should().Be(expected);
collection.Should().HaveCount(3).And.Contain(x => x.Id == id);
action.Should().Throw<DomainException>().WithMessage("*expired*");
asyncAction.Should().ThrowAsync<InvalidOperationException>();
dto.Should().BeEquivalentTo(expected, opt => opt.Excluding(x => x.CreatedAt));
```

On failure, FluentAssertions produces messages like "Expected `collection` to have count 3, but found 2." — beats xUnit's plain `Assert.Equal(3, collection.Count)`.

### 56.4 Moq

Library for creating test doubles (mocks, stubs):

```csharp
var repoMock = new Mock<IReservationRepository>();
repoMock.Setup(r => r.GetByIdAsync(It.IsAny<ReservationId>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(existingReservation);

var clockMock = new Mock<IClock>();
clockMock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.Parse("2026-05-17T10:00:00Z"));

var handler = new ConfirmReservationCommandHandler(repoMock.Object, clockMock.Object, dbMock.Object);

var result = await handler.Handle(new ConfirmReservationCommand(reservationId), CancellationToken.None);

result.IsError.Should().BeFalse();
repoMock.Verify(r => r.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()), Times.Once);
```

Use mocks for **collaborators with side effects** (repositories, gateways, clocks). Don't mock value objects or pure functions — just construct them.

#### Hand-rolled fakes vs Moq

For interfaces you mock often, a hand-rolled fake is often clearer:

```csharp
public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; }
    public FakeClock(DateTimeOffset now) => UtcNow = now;
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
```

Use Moq when the interface has many methods and only one or two matter per test.

### 56.5 What to unit-test

| Layer | Test it? | What |
|---|---|---|
| **Domain** | Yes, heavily | Aggregate invariants, value object validation, state transitions, domain event raising |
| **Application** | Yes | Handler logic with mocked repositories — validation paths, error paths, success path |
| **Infrastructure** | Mostly no | Test via integration; EF Core configuration is exercised by real-DB tests |
| **Api** (Carter modules) | Light | Routing and authorization; logic is in handlers |

Memory note: Eventify rule is **tests-after**, not TDD. Write the aggregate, then the tests, in the same PR.

### Recommended reading
- "Unit Testing Principles, Practices, and Patterns" — Vladimir Khorikov. The single best book on this.
- xUnit docs.
- "FluentAssertions" docs.
- "The Art of Unit Testing, 3rd ed." — Roy Osherove.

---

## 57. Testcontainers

### 57.1 Why

Integration tests against mocks lie. The DB constraints, the EF Core SQL translation, the RabbitMQ topology — none are exercised. Eventify mandates real infrastructure for integration tests (CLAUDE.md), and **Testcontainers** is how we do it without a long-running shared environment.

### 57.2 The idea

Testcontainers spins up disposable Docker containers from your test code. Each test class (or session) starts fresh Postgres, RabbitMQ, Redis, etc., runs the tests, and disposes the containers.

```csharp
public sealed class BookingIntegrationFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("eventify_booking_test")
        .Build();

    public RabbitMqContainer Rabbit { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    public string DbConnectionString => Postgres.GetConnectionString();
    public string RabbitConnectionString => Rabbit.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();
        await Rabbit.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Rabbit.DisposeAsync();
    }
}

[CollectionDefinition("Booking-Integration")]
public sealed class BookingIntegrationCollection : ICollectionFixture<BookingIntegrationFixture> { }

[Collection("Booking-Integration")]
public sealed class ReservationFlowTests
{
    private readonly BookingIntegrationFixture _fx;
    public ReservationFlowTests(BookingIntegrationFixture fx) => _fx = fx;

    [Fact]
    public async Task POST_reservations_creates_row_and_publishes_event()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Db", _fx.DbConnectionString)
                                       .UseSetting("RabbitMq:Host", _fx.RabbitConnectionString));
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/reservations", new { sessionId = ..., seatIds = ... });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var rows = await db.Reservations.CountAsync();
        rows.Should().Be(1);
    }
}
```

### 57.3 WebApplicationFactory

ASP.NET Core ships `Microsoft.AspNetCore.Mvc.Testing` which exposes `WebApplicationFactory<TEntryPoint>` — boots the entire app in-process, swaps DI, returns an `HttpClient`. Combined with Testcontainers, you can run end-to-end tests of any endpoint against real DB + bus.

### 57.4 Cost

Slower than unit tests (container startup ~2-5s per test class). Use `IClassFixture<T>` or collection fixtures to share containers across tests where isolation isn't required. Reset DB state between tests with `Respawn` (a library that truncates all tables fast).

### Recommended reading
- Testcontainers for .NET docs: <https://dotnet.testcontainers.org/>
- "Integration Testing ASP.NET Core" — Microsoft docs.
- "Respawn" — Jimmy Bogard.

---

## 58. NetArchTest

### 58.1 What it does

NetArchTest is a fluent library that asserts architectural rules in *test code*. Failed rules fail the build.

```csharp
public sealed class ArchitectureTests
{
    private const string DomainNs = "Eventify.Catalog.Domain";
    private const string AppNs = "Eventify.Catalog.Application";
    private const string InfraNs = "Eventify.Catalog.Infrastructure";

    [Fact]
    public void Domain_should_not_depend_on_Application_or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Artist).Assembly)
            .Should()
            .NotHaveDependencyOnAny(AppNs, InfraNs, "Microsoft.EntityFrameworkCore", "MediatR")
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Handlers_should_be_sealed()
    {
        Types.InAssembly(typeof(CreateArtistCommandHandler).Assembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .GetResult().IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AggregateRoots_should_have_private_parameterless_ctor()
    {
        // ... custom check
    }
}
```

### 58.2 What rules Eventify enforces

- Domain has no dependencies on Application, Infrastructure, EF Core, MediatR, etc.
- Application depends only on Domain (plus MediatR/FluentValidation/ErrorOr).
- Infrastructure may depend on anything but is not depended on by Domain or Application.
- Carter modules live only in Api.
- Handlers are `sealed`.
- Commands and queries are `sealed record`s.

### 58.3 Why it matters

Layering rules degrade silently. A junior dev adds `using Microsoft.EntityFrameworkCore;` to the Domain project to "just sort it quickly". Six months later, your Domain is no longer pure. NetArchTest catches it in the very PR that introduces it.

### Recommended reading
- NetArchTest repo: <https://github.com/BenMorris/NetArchTest>
- ArchUnitNET (alternative): <https://github.com/TNG/ArchUnitNET>
- "Just Enough Software Architecture" — George Fairbanks.

---

# Part XVI — DevOps

## 59. Docker fundamentals

### 59.1 Mental model

A **container** is a process (or process tree) running with its own filesystem, network, and process namespace. It is *not* a VM — it shares the host kernel.

An **image** is a frozen filesystem + metadata (entrypoint, env vars, exposed ports). Containers are running instances of images.

A **layer** is a diff in the image filesystem. Each Dockerfile instruction creates a layer; layers are cached and shared between images.

### 59.2 A Dockerfile for a .NET service

```dockerfile
# syntax=docker/dockerfile:1.7

# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore as a separate layer for cache efficiency
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/BuildingBlocks/Eventify.SharedKernel/Eventify.SharedKernel.csproj BuildingBlocks/Eventify.SharedKernel/
COPY src/BuildingBlocks/Eventify.IntegrationEvents/Eventify.IntegrationEvents.csproj BuildingBlocks/Eventify.IntegrationEvents/
COPY src/Services/Catalog/Eventify.Catalog.Domain/Eventify.Catalog.Domain.csproj Services/Catalog/Eventify.Catalog.Domain/
COPY src/Services/Catalog/Eventify.Catalog.Application/Eventify.Catalog.Application.csproj Services/Catalog/Eventify.Catalog.Application/
COPY src/Services/Catalog/Eventify.Catalog.Infrastructure/Eventify.Catalog.Infrastructure.csproj Services/Catalog/Eventify.Catalog.Infrastructure/
COPY src/Services/Catalog/Eventify.Catalog.Api/Eventify.Catalog.Api.csproj Services/Catalog/Eventify.Catalog.Api/
RUN dotnet restore Services/Catalog/Eventify.Catalog.Api/Eventify.Catalog.Api.csproj

COPY src/ ./
RUN dotnet publish Services/Catalog/Eventify.Catalog.Api/Eventify.Catalog.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5051
USER $APP_UID
ENTRYPOINT ["dotnet", "Eventify.Catalog.Api.dll"]
```

Key practices:
- **Multi-stage build** — SDK image is huge (~700MB), runtime image is small (~200MB). Don't ship the SDK.
- **Restore-only layer** — `dotnet restore` is the slowest step. Cache it by copying only `.csproj` files first.
- **`USER $APP_UID`** — run as non-root (security).
- **Pinned base images** — never `latest`; pin major.minor.

### 59.3 Useful commands

```bash
docker build -t eventify-catalog:1.0 -f deploy/docker/catalog.Dockerfile .
docker run --rm -p 5051:5051 -e ConnectionStrings__Db="..." eventify-catalog:1.0
docker images
docker ps
docker logs <container>
docker exec -it <container> /bin/sh
docker system prune -af   # nuke unused images/containers
```

### 59.4 .dockerignore

Mandatory. Exclude `bin/`, `obj/`, `.git/`, `.vs/`, `node_modules/`, etc. Otherwise build context balloons and you accidentally COPY local secrets.

### Recommended reading
- "Docker Deep Dive" — Nigel Poulton.
- "Docker for .NET Developers" — Steve Gordon.
- Official Docker docs.

---

## 60. Docker Compose

### 60.1 What it does

Defines and runs multi-container apps in one YAML file. One command brings up your whole local environment.

```yaml
# docker-compose.yml — infrastructure layer
services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: eventify
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_MULTIPLE_DATABASES: identity,catalog,booking,payment,ticket,notification
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./deploy/postgres/init-multiple-dbs.sh:/docker-entrypoint-initdb.d/init.sh:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U eventify"]
      interval: 5s
      retries: 10

  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  mailhog:
    image: mailhog/mailhog
    ports:
      - "1025:1025"
      - "8025:8025"

volumes:
  pgdata:
```

### 60.2 Layered compose files

```bash
# Infrastructure only
docker compose -f docker-compose.yml up -d

# + app services
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d

# + observability
docker compose -f docker-compose.yml -f docker-compose.app.yml -f docker-compose.observability.yml up -d
```

Each file overrides/extends the previous. Lets devs spin up just what they need.

### 60.3 Networking

By default, Compose creates a bridge network where each service is reachable by its service name. `catalog-api` can call `http://postgres:5432` and `http://rabbitmq:5672` directly.

### 60.4 Depends_on + healthcheck

```yaml
catalog-api:
  depends_on:
    postgres:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
```

Without `condition: service_healthy`, `depends_on` only waits for the container to *start*, not to be ready — your service will boot before Postgres accepts connections. Always couple with healthchecks.

### Recommended reading
- Compose spec: <https://compose-spec.io/>
- "Docker Compose: Up and Running" — Karl Matthias.

---

## 61. Kubernetes essentials

### 61.1 The core objects

| Object | What it is |
|---|---|
| **Pod** | One or more containers sharing network + storage. Smallest deployable unit. Ephemeral. |
| **Deployment** | Manages a desired number of replicas of a Pod template; handles rolling updates and rollback. |
| **Service** | Stable virtual IP + DNS name in front of a set of Pods. Load-balances across them. |
| **ConfigMap** | Key/value config injected as env vars or files. |
| **Secret** | Like ConfigMap but base64 (and ideally encrypted) for credentials. |
| **Ingress** | HTTP(S) routing from outside the cluster to Services. |
| **StatefulSet** | Like Deployment but each Pod has stable identity + persistent storage. For databases, brokers. |
| **Job** | Run a Pod to completion (one-off tasks like migrations). |
| **CronJob** | Job on a schedule. |
| **PersistentVolume / PVC** | Cluster storage abstraction. |
| **Namespace** | Logical isolation within a cluster (one namespace per environment, team, app). |

### 61.2 The reconciliation model

You don't tell K8s to "create a Pod". You declare desired state ("I want 3 replicas of `catalog-api`"). The control plane continuously compares actual to desired and converges. If a Pod dies, K8s starts another. If a node fails, K8s reschedules its Pods elsewhere.

### 61.3 A minimal Deployment + Service

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: catalog-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: catalog-api
  template:
    metadata:
      labels:
        app: catalog-api
    spec:
      containers:
      - name: catalog-api
        image: ghcr.io/<user>/eventify-catalog:1.0
        ports:
        - containerPort: 5051
        env:
        - name: ConnectionStrings__Db
          valueFrom:
            secretKeyRef:
              name: catalog-db
              key: connection-string
        livenessProbe:
          httpGet: { path: /health/live, port: 5051 }
          initialDelaySeconds: 10
        readinessProbe:
          httpGet: { path: /health/ready, port: 5051 }
          initialDelaySeconds: 5
        resources:
          requests: { cpu: "100m", memory: "256Mi" }
          limits:   { cpu: "500m", memory: "512Mi" }
---
apiVersion: v1
kind: Service
metadata:
  name: catalog-api
spec:
  selector: { app: catalog-api }
  ports:
  - port: 5051
    targetPort: 5051
```

### 61.4 Kustomize

Built into `kubectl` since 1.14. Lets you have a `base/` directory with the manifests and `overlays/local/`, `overlays/staging/`, etc., applying patches per environment without templating.

```
deploy/k8s/
├── base/
│   ├── catalog/
│   │   ├── deployment.yaml
│   │   ├── service.yaml
│   │   └── kustomization.yaml
│   └── kustomization.yaml
└── overlays/
    └── local/
        ├── kustomization.yaml
        └── catalog-config-patch.yaml
```

```bash
kubectl apply -k deploy/k8s/overlays/local
```

Helm is the alternative (templating-based); Kustomize is simpler and good enough for Eventify.

### 61.5 Migrations as a Job

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: catalog-migrate
spec:
  template:
    spec:
      restartPolicy: Never
      containers:
      - name: migrate
        image: ghcr.io/<user>/eventify-catalog:1.0
        env:
        - name: ConnectionStrings__Db
          valueFrom: { secretKeyRef: { name: catalog-db, key: connection-string } }
        command: ["dotnet", "Eventify.Catalog.Api.dll", "--migrate-only"]
```

Run before rolling out new app pods.

### 61.6 Local K8s

- **minikube** — single-node K8s in a VM.
- **k3d** — k3s (lightweight K8s) in Docker. Faster than minikube.
- **kind** — K8s in Docker.

Pick one for Iter 5. k3d is the most ergonomic.

### Recommended reading
- "Kubernetes Up & Running, 3rd ed." — Hightower, Burns, Beda.
- "Kubernetes in Action, 2nd ed." — Marko Lukša.
- Official tutorials: <https://kubernetes.io/docs/tutorials/>.

---

## 62. GitHub Actions

### 62.1 The model

YAML workflow files in `.github/workflows/`. Trigger on events (`push`, `pull_request`, `schedule`, `workflow_dispatch`). Each workflow has jobs; each job runs on a runner (Ubuntu/Windows/Mac) and has steps.

### 62.2 Eventify's build workflow

```yaml
name: build
on:
  push: { branches: [main] }
  pull_request: { branches: [main] }

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '10.0.x' }
    - name: Restore
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore --configuration Release
    - name: Unit Tests
      run: dotnet test --no-build --configuration Release --filter "Category!=Integration" --logger "trx;LogFileName=results.trx"
    - name: Architecture Tests
      run: dotnet test tests/Eventify.ArchitectureTests/ --no-build --configuration Release
    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results
        path: '**/*.trx'
```

### 62.3 Integration tests workflow

```yaml
name: integration-tests
on:
  pull_request: { branches: [main] }

jobs:
  integration:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with: { dotnet-version: '10.0.x' }
    - name: Integration tests (Testcontainers boots Postgres/RabbitMQ on demand)
      run: dotnet test --filter "Category=Integration" --logger "trx"
```

GitHub-hosted Ubuntu runners ship Docker — Testcontainers Just Works.

### 62.4 Image build & push

```yaml
name: docker-publish
on:
  push:
    tags: ['v*']

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions: { contents: read, packages: write }
    strategy:
      matrix:
        service: [identity, catalog, booking, payment, ticket, notification, gateway]
    steps:
    - uses: actions/checkout@v4
    - uses: docker/login-action@v3
      with:
        registry: ghcr.io
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    - uses: docker/build-push-action@v5
      with:
        context: .
        file: deploy/docker/${{ matrix.service }}.Dockerfile
        push: true
        tags: ghcr.io/${{ github.repository_owner }}/eventify-${{ matrix.service }}:${{ github.ref_name }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
```

The `matrix` runs the build job once per service in parallel.

### 62.5 Branch protection

In GitHub repo settings, protect `main`:
- Require PR.
- Require status checks (`build`, `integration-tests`) to pass.
- Require linear history (no merge commits) — keeps history clean.
- Require signed commits (stretch).

### Recommended reading
- "GitHub Actions" docs.
- "Continuous Delivery" — Jez Humble, David Farley (the book).
- "Learning GitHub Actions" — Brent Laster.

---

# Part XVII — Frontend

The React SPA in `src/Web/eventify-web/`. This part is shorter — frontend is not the main learning surface — but you still need to internalize the patterns.

## 63. React + Vite + TypeScript

### 63.1 React in 2026

React 19 (stable since 2024). You'll use:
- **Function components only** — no classes.
- **Hooks** — `useState`, `useEffect`, `useMemo`, `useCallback`, `useRef`, `useReducer`, plus custom hooks.
- **JSX** with TypeScript (`.tsx` files).
- **No legacy lifecycle methods**, no `this`, no `componentDidMount`.

```tsx
type SeatProps = { seat: Seat; onSelect: (id: SeatId) => void };

export function SeatButton({ seat, onSelect }: SeatProps) {
    const handleClick = useCallback(() => onSelect(seat.id), [seat.id, onSelect]);
    return (
        <button
            disabled={seat.status !== 'free'}
            onClick={handleClick}
            aria-label={`Seat ${seat.label}`}>
            {seat.label}
        </button>
    );
}
```

### 63.2 Vite

Build tool. Dev server is instant (ES modules natively), production build is Rollup-based and tree-shakes aggressively.

```bash
npm create vite@latest eventify-web -- --template react-ts
npm install
npm run dev      # http://localhost:5173
npm run build    # outputs dist/
npm run preview  # serves the production build
```

### 63.3 TypeScript

Use strict mode (`"strict": true` in `tsconfig.json`). Don't use `any`. If a type is wrong, fix the type, don't cast.

```ts
// tsconfig.json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "jsx": "react-jsx",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

### Recommended reading
- React docs (rewritten 2023): <https://react.dev/learn>
- "Effective TypeScript, 2nd ed." — Dan Vanderkam.
- "TypeScript Deep Dive" — Basarat (free online).

---

## 64. TanStack Router and Query

### 64.1 TanStack Router

Type-safe routing. Define your route tree once; the router gives you typed params and search.

```ts
import { createRoute, createRootRoute, createRouter } from '@tanstack/react-router';

const rootRoute = createRootRoute({ component: RootLayout });

const sessionRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: '/sessions/$sessionId',
    component: SessionPage,
    loader: ({ params }) => queryClient.ensureQueryData(sessionQuery(params.sessionId))
});

export const router = createRouter({
    routeTree: rootRoute.addChildren([sessionRoute, /* ... */])
});

// Usage — fully typed
const navigate = useNavigate();
navigate({ to: '/sessions/$sessionId', params: { sessionId: '123' } });
```

### 64.2 TanStack Query

Server state. Caches responses, handles loading/error/stale states, retries, refetches on window focus.

```tsx
function useSession(sessionId: string) {
    return useQuery({
        queryKey: ['session', sessionId],
        queryFn: () => api.getSession(sessionId),
        staleTime: 30_000
    });
}

function SessionPage() {
    const { sessionId } = useParams({ from: '/sessions/$sessionId' });
    const { data: session, isLoading, error } = useSession(sessionId);

    if (isLoading) return <Spinner />;
    if (error) return <ErrorBanner error={error} />;
    return <SessionView session={session!} />;
}
```

Mutations for writes:

```tsx
const createReservation = useMutation({
    mutationFn: (cmd: CreateReservationRequest) => api.createReservation(cmd),
    onSuccess: (res) => {
        queryClient.invalidateQueries({ queryKey: ['my-bookings'] });
        navigate({ to: '/checkout/$reservationId', params: { reservationId: res.id } });
    }
});

createReservation.mutate({ sessionId, seatIds });
```

### 64.3 Why Query instead of Redux for server state

Redux/Zustand are for *client* state. Server state has different concerns: caching, deduplication, freshness, retries. Query specializes for it. Combining `Query + Zustand + Router` covers ~95% of SPA state needs without Redux.

### Recommended reading
- TanStack docs (Router + Query): <https://tanstack.com/>
- "Practical React Query" — TkDodo blog series.

---

## 65. Zustand

Lightweight client-state store. Hook-based, no boilerplate.

```ts
import { create } from 'zustand';

type SelectedSeatsState = {
    seatIds: Set<string>;
    toggle: (id: string) => void;
    clear: () => void;
};

export const useSelectedSeats = create<SelectedSeatsState>((set) => ({
    seatIds: new Set(),
    toggle: (id) => set((s) => {
        const next = new Set(s.seatIds);
        next.has(id) ? next.delete(id) : next.add(id);
        return { seatIds: next };
    }),
    clear: () => set({ seatIds: new Set() })
}));

// In component
const { seatIds, toggle } = useSelectedSeats();
```

Use Zustand for: current selection state, cart-like state, UI preferences, auth user (with caveats — sometimes Query is fine for auth).

Don't use Zustand for server data you can get from the API.

### Recommended reading
- Zustand docs: <https://zustand.docs.pmnd.rs/>

---

## 66. shadcn/ui + Tailwind

### 66.1 Tailwind CSS

Utility-first CSS. Instead of writing `.btn-primary { ... }`, you compose utilities in JSX:

```tsx
<button className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:ring-2 focus:ring-blue-400 disabled:opacity-50">
    Reserve
</button>
```

Critics call it inline-style abuse. Practitioners point out the consistency, the trivial dead-code elimination (PurgeCSS), and the speed of building UIs.

### 66.2 shadcn/ui

Not a npm library — a CLI that **copies** accessible component implementations into your codebase. You own the code; customize freely.

```bash
npx shadcn-ui@latest init
npx shadcn-ui@latest add button dialog input form
```

Components use Radix UI primitives (accessibility) + Tailwind (styling). The combination is the de-facto standard in modern React.

### Recommended reading
- Tailwind docs: <https://tailwindcss.com/docs>
- shadcn/ui: <https://ui.shadcn.com/>

---

## 67. React Hook Form + Zod

### 67.1 Forms without ceremony

```tsx
const schema = z.object({
    email: z.string().email(),
    password: z.string().min(8)
});
type FormData = z.infer<typeof schema>;

function LoginForm() {
    const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
        resolver: zodResolver(schema)
    });

    return (
        <form onSubmit={handleSubmit(onSubmit)}>
            <input {...register('email')} />
            {errors.email && <span>{errors.email.message}</span>}
            <input type="password" {...register('password')} />
            <button type="submit">Sign in</button>
        </form>
    );
}
```

React Hook Form keeps the form uncontrolled (fast), Zod validates the schema (type-safe), and you write zero `useState` for fields.

### Recommended reading
- React Hook Form docs: <https://react-hook-form.com/>
- Zod docs: <https://zod.dev/>

---

## 68. oidc-client-ts

OIDC for the SPA. Implements Authorization Code + PKCE.

```ts
import { UserManager } from 'oidc-client-ts';

export const userManager = new UserManager({
    authority: 'https://identity.eventify',
    client_id: 'eventify-spa',
    redirect_uri: `${window.location.origin}/auth/callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: 'code',
    scope: 'openid profile eventify.read eventify.write offline_access',
    automaticSilentRenew: true
});

// Initiate login
userManager.signinRedirect();

// On /auth/callback route
const user = await userManager.signinRedirectCallback();
console.log(user.access_token);

// Inject token into API requests (TanStack Query default fetcher)
async function authedFetch(url: string, init?: RequestInit) {
    const user = await userManager.getUser();
    return fetch(url, { ...init, headers: { ...init?.headers, Authorization: `Bearer ${user!.access_token}` } });
}
```

Library handles PKCE, code exchange, silent renew via hidden iframe (or refresh token if `offline_access`).

### Recommended reading
- oidc-client-ts: <https://github.com/authts/oidc-client-ts>
- "OAuth 2.0 for Browser-Based Apps" — IETF BCP 240.

---

## 69. Stripe Elements

Pre-built UI components for card capture, fully PCI-compliant.

```tsx
import { loadStripe } from '@stripe/stripe-js';
import { Elements, CardElement, useStripe, useElements } from '@stripe/react-stripe-js';

const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PK);

function CheckoutPage({ clientSecret }: { clientSecret: string }) {
    return (
        <Elements stripe={stripePromise} options={{ clientSecret }}>
            <PayForm />
        </Elements>
    );
}

function PayForm() {
    const stripe = useStripe();
    const elements = useElements();

    async function onSubmit(e: FormEvent) {
        e.preventDefault();
        const { error, paymentIntent } = await stripe!.confirmCardPayment(clientSecret, {
            payment_method: { card: elements!.getElement(CardElement)! }
        });
        if (error) showError(error.message);
        else if (paymentIntent.status === 'succeeded') navigate({ to: '/bookings' });
    }

    return (
        <form onSubmit={onSubmit}>
            <CardElement />
            <button disabled={!stripe}>Pay</button>
        </form>
    );
}
```

The card data goes browser → Stripe direct. Your server only sees the `client_secret` and the eventual webhook.

### Recommended reading
- Stripe React docs: <https://stripe.com/docs/stripe-js/react>
- Stripe testing card numbers: <https://stripe.com/docs/testing>

---

## 70. @microsoft/signalr

```ts
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export async function connectToSeatHub(sessionId: string, getToken: () => Promise<string>) {
    const conn = new HubConnectionBuilder()
        .withUrl(`/hubs/seats/${sessionId}`, { accessTokenFactory: getToken })
        .withAutomaticReconnect([0, 1000, 5000, 15000])
        .configureLogging(LogLevel.Information)
        .build();

    conn.on('SeatHeld', ({ seatId, expiresAt }) => seatStore.setHeld(seatId, expiresAt));
    conn.on('SeatReleased', ({ seatId }) => seatStore.setFree(seatId));
    conn.on('SeatBooked', ({ seatId }) => seatStore.setBooked(seatId));

    await conn.start();
    return conn;
}

// In SessionPage
useEffect(() => {
    let conn: HubConnection | undefined;
    connectToSeatHub(sessionId, () => userManager.getUser().then(u => u!.access_token))
        .then(c => { conn = c; });
    return () => { conn?.stop(); };
}, [sessionId]);
```

Auto-reconnect handles transient disconnects. On reconnect, fetch the latest seat state via REST to recover from missed events.

### Recommended reading
- "@microsoft/signalr" npm + docs.
- "Real-Time Web Applications with SignalR" — Microsoft Learn.

---

# Appendix A — Recommended books and resources, ordered by topic

A consolidated list. Books are ordered by *read first* → *read deeper*. Mark items you've finished as you go.

## A.1 Software design and architecture (foundations)

1. **"Clean Code"** — Robert C. Martin. The vocabulary baseline for naming, function size, and "obvious" code.
2. **"The Pragmatic Programmer, 20th Anniversary"** — Hunt & Thomas. Habits and instincts.
3. **"Refactoring, 2nd ed."** — Martin Fowler. Read the smell catalog.
4. **"Clean Architecture"** — Robert C. Martin. The dependency rule across all scales.
5. **"Get Your Hands Dirty on Clean Architecture"** — Tom Hombergs. Pragmatic, code-first.
6. **"Patterns of Enterprise Application Architecture"** — Martin Fowler. Repository, Unit of Work, Active Record, etc. — still the reference.
7. **"Building Evolutionary Architectures, 2nd ed."** — Ford, Parsons, Kua. Fitness functions; how architecture changes over time.

## A.2 Domain-Driven Design

1. **"Learning Domain-Driven Design"** — Vlad Khononov (2021). *Start here.*
2. **"Domain-Driven Design"** — Eric Evans (Blue Book). The canon.
3. **"Implementing Domain-Driven Design"** — Vaughn Vernon (Red Book). Code-heavy, focuses on aggregates and sagas.
4. **"Domain-Driven Design Distilled"** — Vernon. Short overview.
5. **"Patterns, Principles, and Practices of Domain-Driven Design"** — Scott Millett & Nick Tune. .NET focus.
6. **"Introducing EventStorming"** — Alberto Brandolini.
7. Article: "Effective Aggregate Design" (3-part) — Vaughn Vernon.

## A.3 C# / .NET

1. **"C# in Depth, 4th ed."** — Jon Skeet. Language deep dive.
2. **"Effective C#, 3rd ed."** — Bill Wagner. Idioms.
3. **"More Effective C#"** — Bill Wagner.
4. **"Pro .NET Memory Management"** — Konrad Kokosa. When you need to optimize.
5. **"Concurrency in C# Cookbook"** — Stephen Cleary.
6. Stephen Cleary's blog: <https://blog.stephencleary.com/>
7. Andrew Lock's blog: <https://andrewlock.net/>

## A.4 ASP.NET Core & EF Core

1. **"ASP.NET Core in Action, 3rd ed."** — Andrew Lock.
2. **"Entity Framework Core in Action, 2nd ed."** — Jon P Smith.
3. **"Pro ASP.NET Core Identity"** — Adam Freeman.
4. Microsoft docs: <https://learn.microsoft.com/aspnet/core/> and <https://learn.microsoft.com/ef/core/>
5. Julie Lerman's Pluralsight EF Core courses.
6. Milan Jovanović: <https://www.milanjovanovic.tech/> (best modern .NET tutorials).

## A.5 Microservices and distributed systems

1. **"Building Microservices, 2nd ed."** — Sam Newman.
2. **"Microservices Patterns"** — Chris Richardson. Best catalogue of patterns; Saga chapter is canonical.
3. **"Designing Data-Intensive Applications"** — Martin Kleppmann. The book on consistency, replication, and trade-offs.
4. **"Release It! 2nd ed."** — Michael Nygard. Resilience patterns.
5. **"Monolith to Microservices"** — Sam Newman.
6. **"Event-Driven Microservices"** — Adam Bellemare.
7. microservices.io pattern catalog: <https://microservices.io/patterns/index.html>
8. Pat Helland's papers ("Life beyond Distributed Transactions", "Immutability Changes Everything").

## A.6 Messaging and event-driven

1. **"Enterprise Integration Patterns"** — Hohpe & Woolf. *The* messaging book.
2. **"RabbitMQ in Depth"** — Gavin Roy.
3. MassTransit docs: <https://masstransit.io/documentation>
4. RabbitMQ tutorials: <https://www.rabbitmq.com/getstarted.html>
5. "Designing Event-Driven Systems" — Ben Stopford (Confluent, free PDF).

## A.7 APIs (REST, gRPC, OAuth/OIDC)

1. **"REST API Design Rulebook"** — Mark Masse.
2. **"gRPC: Up & Running"** — Kasun Indrasiri.
3. **"OAuth 2 in Action"** — Justin Richer, Antonio Sanso.
4. **"JWT Handbook"** — Auth0 (free PDF).
5. RFCs: 7519 (JWT), 6749 (OAuth 2.0), 7636 (PKCE), 7807 (Problem Details), 9457 (Problem Details revision), 9700 (OAuth 2.0 Security BCP).
6. Vittorio Bertocci's blog and YouTube on OIDC.

## A.8 Frontend (React + ecosystem)

1. React docs (rewritten): <https://react.dev/learn>
2. **"Effective TypeScript, 2nd ed."** — Dan Vanderkam.
3. **"TypeScript Deep Dive"** — Basarat (free online).
4. TanStack docs: <https://tanstack.com/>
5. "Practical React Query" — TkDodo blog series.

## A.9 Testing

1. **"Unit Testing Principles, Practices, and Patterns"** — Vladimir Khorikov. *The* book.
2. **"The Art of Unit Testing, 3rd ed."** — Roy Osherove.
3. **"xUnit Test Patterns"** — Gerard Meszaros.
4. Testcontainers .NET: <https://dotnet.testcontainers.org/>
5. NetArchTest: <https://github.com/BenMorris/NetArchTest>

## A.10 Observability

1. **"Observability Engineering"** — Charity Majors, Liz Fong-Jones, George Miranda.
2. **"Distributed Tracing in Practice"** — Austin Parker et al.
3. OpenTelemetry .NET: <https://opentelemetry.io/docs/languages/net/>
4. Honeycomb blog (Charity Majors).
5. "Logging in .NET" — Andrew Lock blog series.

## A.11 DevOps and Kubernetes

1. **"The Phoenix Project"** — Kim, Behr, Spafford. Required reading.
2. **"The DevOps Handbook"** — Kim, Humble, Debois, Willis.
3. **"Continuous Delivery"** — Jez Humble, David Farley.
4. **"Kubernetes Up & Running, 3rd ed."** — Hightower, Burns, Beda.
5. **"Kubernetes in Action, 2nd ed."** — Marko Lukša.
6. **"Docker Deep Dive"** — Nigel Poulton.

## A.12 Payments & Stripe

1. Stripe docs: <https://stripe.com/docs/payments>
2. Stripe testing guide.
3. Stripe.NET repo + samples.

## A.13 SQL and Postgres

1. **"SQL Antipatterns"** — Bill Karwin.
2. **"PostgreSQL: Up & Running, 4th ed."** — Obe & Hsu.
3. "Use The Index, Luke!" — Markus Winand (free): <https://use-the-index-luke.com/>

## A.14 YouTube channels and conference talks

- **Milan Jovanović** — .NET, Clean Architecture, microservices.
- **Nick Chapsas** — practical C#/ASP.NET deep dives.
- **CodeOpinion** (Derek Comartin) — architecture, microservices, messaging.
- **Jimmy Bogard** at NDC — DDD + MediatR + MassTransit.
- **Vladimir Khorikov** — testing, DDD, functional thinking in C#.
- **Vittorio Bertocci** at NDC and Identiverse — identity.

## A.15 Reading order for Eventify specifically

If you can only read a handful, do them in this order:

1. **"Learning Domain-Driven Design"** — Khononov.
2. **"Get Your Hands Dirty on Clean Architecture"** — Hombergs.
3. **"Microservices Patterns"** — Richardson (chapters 1–6 are the minimum).
4. **"Designing Data-Intensive Applications"** — Kleppmann (chapters 1–11).
5. **"Unit Testing Principles, Practices, and Patterns"** — Khorikov.
6. **"OAuth 2 in Action"** — Richer & Sanso.
7. **"Release It!"** — Nygard.

The rest you can pick up as the iteration requires them.

---

## Document maintenance

This document is the **learning companion** to ARCHITECTURE.md. Update it when:
1. A new technology choice enters the project — add a section.
2. A pattern's rationale changes — update the relevant section and link to the ADR.
3. You find a section is unclear after re-reading — rewrite for your future self.

Pair this with the actual code as it grows. Theory without code rots; code without theory becomes folklore.













