# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication rules

### Critical thinking

Do not automatically agree with everything I write or suggest just because I asked for it.

- If my idea, architecture, or implementation is weak, incorrect, risky, or overengineered — explain why.
- Suggest a better approach when appropriate.
- Prioritize correctness, maintainability, scalability, and simplicity over agreement.
- Challenge poor assumptions and bad design decisions.
- Be direct, constructive, and technically honest.

### Engineering Mindset

- Think like a senior engineer and software architect.
- Prefer production-ready solutions over quick hacks.
- Avoid unnecessary abstractions and overengineering.
- Explain tradeoffs between different approaches.
- Focus on readability, maintainability, and long-term scalability.

## Response Structure

Structure every response using the following sections when applicable:

### What was done

Describe what was implemented, changed, analyzed, or fixed.

### What I need to do

Describe actions required from me:

- commands to run,
- validations to perform,
- decisions to make,
- files to check,
- configurations to update.

### Next Step

Suggest the next logical implementation or improvement step.

---

## Continuous Improvement

When a task is completed:

- Suggest improvements and refactoring opportunities.
- Point out technical debt and architectural concerns.
- Mention possible edge cases.
- Recommend performance, security, testing, and scalability improvements.
- Suggest better patterns or approaches if they exist.

### APIs

- Design APIs consistently and predictably.
- Validate inputs properly.
- Handle errors explicitly.
- Use proper HTTP status codes and meaningful error responses.

### Database

- Optimize database queries when needed.
- Avoid unnecessary database roundtrips.
- Think about indexing and query scalability.
- Consider transaction boundaries carefully.

### Testing

- Suggest unit and integration tests when relevant.
- Cover critical business logic and edge cases.
- Avoid fragile tests.

## Solving problems

When solving problems:

1. Analyze the root cause first.
2. Explain why the issue happens.
3. Suggest the simplest reliable solution.
4. Mention alternative approaches if relevant.
5. Explain tradeoffs and risks.

### Decision Validation

Before suggesting a solution:

- Evaluate whether the solution is actually appropriate for the current project scale and complexity.
- Prefer the simplest solution that satisfies the requirements.
- Explicitly warn when a solution introduces unnecessary complexity.
- Distinguish between “good for learning” and “good for production”.

---

## Output Preferences

When generating code:

- Include only necessary code.
- Avoid unnecessary comments.
- Keep naming consistent.
- Follow existing project conventions when possible.

When reviewing code:

- Point out bugs, bad practices, scalability issues, and maintainability concerns.
- Suggest concrete improvements instead of generic criticism.

When planning features:

- Think about scalability, monitoring, security, and future maintenance.

## Accuracy Rules

- Do not invent APIs, methods, package capabilities, framework behavior, or configuration options.
- If uncertain, explicitly say so instead of guessing.
- Verify assumptions against the existing architecture and codebase.
- Do not pretend code was tested if it was not.

## Collaboration model

**Reading** (Read, Grep, Glob, Bash reads): always allowed, no permission needed.

**Code changes** (Edit, Write on project source files): always ask the user before running.

- Never overwrite existing files without explicitly warning about it.

**Git commands** (commit, push, branch, reset, etc.): always ask the user before running.

**Teaching approach for new services/features:**

1. Claude explains the flow and architectural reasoning first (why, trade-offs, microservices context).
2. User writes the code independently.
3. User shares the result; Claude reviews and gives feedback.
   Claude does **not** show the full ready implementation upfront — the goal is skill-building, not copy-pasting.

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

| Service      | Architecture style   | Role                                                                        |
|--------------|----------------------|-----------------------------------------------------------------------------|
| Identity     | Clean Arch           | Duende IdentityServer 7 + ASP.NET Identity; OAuth2/OIDC                     |
| Catalog      | Clean Arch           | Artists, Events, Venues, Sessions, PriceTiers; gRPC for inter-service reads |
| Booking      | Clean Arch           | Reservations (Redis RedLock + TTL), Bookings, MassTransit Saga, SignalR     |
| Payment      | Clean Arch           | Stripe PaymentIntent + webhook; Outbox                                      |
| Ticket       | VSA (single project) | QR-coded tickets; validation endpoint                                       |
| Notification | VSA (single project) | MailHog/SendGrid; Outbox-driven consumers                                   |

**SPA:** `src/Web/EventifySpa` — Vite + React 19 + TypeScript; `oidc-client-ts` for Authorization Code + PKCE flow;
`react-router-dom` for routing; Tailwind CSS v4.

**Orchestration:** No .NET Aspire — services run independently via `launchSettings.json` profiles.

**Dev URLs:** Identity `https://localhost:5001`; Catalog `http://localhost:5002`; SPA `https://localhost:5173`.

**Communication:**

- **REST** (external, via YARP): all SPA → service traffic
- **gRPC** (internal sync): Booking → Catalog, Ticket → Catalog
- **RabbitMQ + MassTransit 8.5** (async): integration events + Saga commands
- **SignalR** (real-time): seat map updates, Redis backplane

**Database:** one logical Postgres 17 database per service; no cross-DB queries; data sharing only via gRPC or events.

## BuildingBlocks

Two projects in `src/BuildingBlocks/`:

- **`Eventify.SharedKernel`** — Domain + Application + Infrastructure base classes consolidated:
    - Domain: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `DomainEvent` (abstract record), interfaces (
      `IEntity`, `IAggregateRoot`, `IAuditable`, `IClearableAggregate`, `IDomainEvent`), `DomainException`
    - Application: `ICommand`, `IQuery<T>`, `ICommandHandler`, `IQueryHandler`, `LoggingBehavior`, `ValidationBehavior`,
      `NotFoundException`, `ValidationException`
    - Infrastructure: `BaseDbContext`, `UpdateAuditableInterceptor`, `PublishDomainEventsInterceptor`

- **`Eventify.IntegrationEvents`** — cross-service event contracts; no deps; `IntegrationEvent` abstract record (UUIDv7)

## Key design decisions

**IDs:** All entity IDs use `Guid.CreateVersion7()` (UUIDv7 — time-sortable, B-tree friendly). Aggregates use
strongly-typed IDs as `readonly record struct ArtistId(Guid Value)`.

**Audit fields:** `CreatedAt`/`UpdatedAt` on `Entity<TId>` are mutated only via the internal `IAuditable` interface.
Populated by `UpdateAuditableInterceptor` (EF Core `ISaveChangesInterceptor`) — never override `SaveChangesAsync` per
service.

**Domain events:** `AggregateRoot<TId>.RaiseDomainEvent` is protected. Clearing happens only via internal
`IClearableAggregate`. Dispatch by `PublishDomainEventsInterceptor` (**pre-save**, `SavingChangesAsync`, via MediatR
`IPublisher`). Pre-save is required for two reasons: (1) EF Core detaches deleted entities from the ChangeTracker after
`SaveChanges`, so post-save dispatch would silently lose `*DeletedDomainEvent`s; (2) domain event handlers that write to
the Outbox table must participate in the same DB transaction as the aggregate change — pre-save guarantees this. Events
are materialized with `.ToList()` before `ClearDomainEvents()` to avoid the live-wrapper trap (`AsReadOnly()` wraps the
underlying list, not a copy).

**MediatR 12.2 pin:** `RequestHandlerDelegate<TResponse>` does not accept `CancellationToken` in 12.2. Pipeline
behaviors call `next()` not `next(cancellationToken)`. If bumped to 12.5+, update `LoggingBehavior` and
`ValidationBehavior`.

**Error handling:** Application handlers return `ErrorOr<TResult>` (uses `ErrorOr` library). Business errors →
`Error.NotFound/Conflict/Validation/Unauthorized/Failure`. `DomainException` only for invariant bugs that should never
reach Domain. No try/catch in handlers; endpoints end with `result.Match(success, errs => errs.ToProblemDetails())`.
Full rationale in `docs/ARCHITECTURE.md` §8.3.

**Endpoints:** Minimal APIs via **Carter** (`ICarterModule` per aggregate under `Endpoints/`). Thin handlers: parse →
`request.ToCommand()` → `sender.Send()` → `result.Match()`. **No MVC controllers anywhere.** No FastEndpoints (conflicts
with MediatR philosophy).

**Mapping:** Manual static extension methods (`ToDto`/`ToDomain`/`ToIntegrationEvent`) colocated with the DTO/event. *
*No mapper libraries** (no AutoMapper / Mapster / Mapperly). Aggregates are small enough that boilerplate is minimal;
full IDE refactoring + compile-time safety wins.

**Money:** Value Object `record Money(decimal Amount, string Currency)` in `SharedKernel`. Validates `Amount >= 0` and
ISO 4217 `Currency` in constructor. EF `OwnsOne(b => b.Money)` flattens to `*_amount` + `*_currency` columns. All
monetary fields use `Money`, never raw `decimal` + `string`.

**API conventions:** URL-segment versioning (`/v1/...`) via `Asp.Versioning.Http`. Offset pagination via
`PagedResult<T>` envelope (`Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`); defaults `pageSize=20`, max `100`.

**Outbox pattern:** All publishing services use MassTransit Transactional Outbox with EF Core. Domain change + outbox
row in one DB transaction.

**Booking Saga:** MassTransit Automatonymous StateMachine in the Booking service (orchestrator, not choreography). Full
state diagram in `docs/ARCHITECTURE.md` §7.

**VSA vs Clean Architecture:** Ticket and Notification are VSA single-project services (less complexity). Identity,
Catalog, Booking, Payment are 4-project Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`).

## C# code style

- **Classic constructors only** — never primary constructors on classes. Use explicit `private readonly` fields assigned
  in constructor body. Positional record syntax (e.g., `record struct ArtistId(Guid Value)`) is fine.
- Nullable reference types enabled (`<Nullable>enable`).
- Implicit usings enabled.
- `LangVersion` set to `latest`.
- Central package versioning via `Directory.Packages.props` — do not specify versions in individual `.csproj` files.

## Project naming & layering

- Projects: `Eventify.{ServiceName}.{Layer}` (e.g., `Eventify.Booking.Domain`).
- Namespace = folder path.
- One aggregate per folder in Domain; one handler per file in Application.
- Dependency rule: `Domain` has zero deps; `Application` → Domain only; `Infrastructure` → Domain + Application; `Api` →
  all three. Enforced by NetArchTest in CI.

## Testing

- xUnit + FluentAssertions + Moq + Testcontainers + NetArchTest.
- Unit tests: Domain invariants + Application handlers (mocked deps).
- Integration tests: real Postgres + RabbitMQ via Testcontainers.
- Architecture tests: `Eventify.ArchitectureTests/` project enforces layering.
- Run integration tests against a real database — do not mock the database in integration tests.

## SPA code style (`src/Web/EventifySpa`)

- **Component declaration:** `const Foo: FC = () => { ... }` with `import type { FC } from "react"`.
- **Styling:** Tailwind CSS v4 — utility classes in JSX, no CSS Modules, no styled-components.
- **Quotes:** double quotes `"` everywhere — imports, strings, JSX attributes.
- **Semicolons:** always at end of statements.
- **Localization:** never hardcode UI strings — always use i18next keys. `en` and `uk` locale files are added together
  every time a new key appears.

## SPA teaching approach

The user is returning to TypeScript/React after a break and needs active guidance:

1. **Before each file:** explain every piece it must contain — imports, types, logic, JSX structure, Tailwind classes.
   Do not assume the user will fill in omitted parts.
2. **Layout/markup is the hardest part** for this user — explain JSX structure and Tailwind utility classes explicitly (
   what each class does visually).
3. **Logic and handlers** are easier but still need explanation of the "why" — especially TypeScript-specific patterns (
   generics, type narrowing, `PropsWithChildren`, etc.).
4. **After the user writes a file**, review it and give concrete, specific feedback.
5. Do **not** show a full ready implementation to copy — explain what to write and let the user write it. See the
   general teaching approach above.

## SPA design system (locked decisions)

The SPA and the Identity Server Razor Pages must share one visual language. Identity Server's `auth.css` (dark
glassmorphism) is the reference. Locked decisions:

- **Brand color:** `--color-brand` is indigo `#6366F1`, paired with violet `--color-brand-2: #8B5CF6`. The old rose
  `#FF3366` is retired. Identity Server already uses indigo→violet — never reintroduce a second brand hue.
- **Language switcher:** label Ukrainian as **UK** (ISO 639-1 language code), English as **EN**. Never "UA" (that is a
  country code). Applies to both SPA and Identity Server `_Layout.cshtml`.
- **Theme:** SPA keeps the light/dark toggle but **defaults to dark** (matches the always-dark Identity Server). New
  visitors land on dark.
- **Animated background:** the gradient drift + blurred orbs from Identity Server's `_Background.cshtml` are ported
  globally into the SPA (`AnimatedBackground` component + `.bg-animated`/`.orb` in `index.css`). Always guard with
  `@media (prefers-reduced-motion: reduce) { animation: none }` in both projects.
- **Navbar / surfaces:** glassmorphism via `bg-surface/70 backdrop-blur-xl` (theme-aware through the `--surface` CSS
  var), not flat `bg-surface`.
- **Typography:** hybrid. **JetBrains Mono** (`font-mono`) for brand wordmark, headings, prices, dates, ticket codes,
  and uppercase caps-labels; **Plus Jakarta Sans** (`font-sans`, default) for body and long-form text. JetBrains Mono is
  the only freely available JetBrains face (monospace) — never use it for paragraph/body text. Both fonts load via Google
  Fonts: SPA in `index.html`, Identity Server in `_Layout.cshtml`. Identity Server applies `font-mono` to `.brand-title`,
  `.card-title`, `.label-caps`, `.status-code`.
- **Language switcher:** a glass **pill** identical to Identity Server's `.lang-pill`/`.lang-link`/`.lang-link-active`
  (rounded-full container, active item = solid `--fg` background with `--bg` text). In the SPA it lives inside the
  Navbar (right side) as a dedicated `LanguageSwitcher` component, not loose buttons.
- **Dark-mode tokens (SPA `.dark`) mirror Identity Server exactly:** `--bg: #08080F`, glass surfaces
  `--surface: rgba(255,255,255,0.055)` / `--surface-2: rgba(255,255,255,0.09)`, `--fg: #FFFFFF`,
  `--fg-muted: rgba(255,255,255,0.45)`, `--border: rgba(255,255,255,0.09)`. Because `--surface` is translucent in dark,
  the Navbar uses `bg-surface backdrop-blur-xl` (no extra `/opacity`). Light-mode tokens stay solid.
- **Icons:** `lucide-react` in the SPA; inline SVG (Heroicons-style) in Razor Pages. Never emoji as icons.

### SPA / Identity Server tech debt

- **Tailwind via CDN in Identity Server:** `_Layout.cshtml` loads `https://cdn.tailwindcss.com` (dev-only). The Razor
  auth pages rely on a handful of Tailwind utility classes (`w-full`, `max-w-md`, `mb-8`, `space-y-5`, `text-center`)
  served by that CDN. Before any production deploy of Identity Server: either build Tailwind into a static bundle or move
  those few classes into `auth.css`. The CDN must not ship to production.
