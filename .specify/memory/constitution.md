<!--
SYNC IMPACT REPORT — 2026-07-27
================================
Version change: TEMPLATE (unratified) → 1.0.0
Bump rationale: Initial ratification. The file was the unmodified Spec Kit template with
                every principle still a [PLACEHOLDER]; there is no prior version to diff
                against, so this is a MAJOR-slot 1.0.0 adoption rather than an amendment.

Principles defined (8) — all derived from existing project artifacts, none invented:
  I.    Learning Surface Over Production Scale   ← ARCHITECTURE.md §1, CLAUDE.md (mentoring mode)
  II.   Bounded Context Autonomy                 ← ARCHITECTURE.md §3, §12
  III.  The Dependency Rule                      ← ARCHITECTURE.md §11 Conventions, CLAUDE.md
  IV.   Explicit Over Implicit                   ← CLAUDE.md (manual mapping, classic ctors)
  V.    Errors Are Values                        ← ARCHITECTURE.md §8.3, CLAUDE.md
  VI.   Transactional Event Integrity            ← ARCHITECTURE.md §8.4, §8.5, CLAUDE.md
  VII.  Tests After Code, Before Merge           ← ARCHITECTURE.md §14 ("TDD is rejected")
  VIII. One Visual Language                      ← CLAUDE.md (SPA design system, locked decisions)

Added sections:
  - Technology Constraints (locked stack decisions)
  - Development Workflow (collaboration model, ADRs, spec-kit flow)
  - Governance (amendment procedure, versioning policy, compliance review)

Removed sections: none (all template placeholders were replaced)

Templates requiring updates:
  ✅ .specify/templates/tasks-template.md — CONFLICT RESOLVED: lines mandating "write tests
     FIRST, ensure they FAIL before implementation" directly contradicted Principle VII.
     Rewritten to tests-after-in-same-PR ordering.
  ✅ .specify/templates/plan-template.md — no change needed; its Constitution Check section is
     generic ("[Gates determined based on constitution file]") and now resolves against this file.
  ✅ .specify/templates/spec-template.md — no change needed; contains no constitution references
     and mandates no ordering that conflicts with any principle.
  ✅ specs/001-tasks-board/plan.md — Constitution Check was recorded as DEFERRED because no
     ratified constitution existed. Re-evaluated against this file.

Follow-up items:
  ✅ RESOLVED 2026-07-27 — docs/ARCHITECTURE.md §15 Security said "AutoMapper config explicit",
     contradicting Principle IV and the same document's own §8.11 and §9. Corrected to manual
     extension-method mapping.
  ✅ RESOLVED 2026-07-27 — docs/ARCHITECTURE.md §1 listed "Multi-language UI — English only" as
     out of scope, contradicting Principle VIII, CLAUDE.md, and TASKS.md (US-1.1, US-8.5). Row
     removed; a new "In scope: bilingual UI (EN + UK)" subsection records the actual decision,
     including the known US-8.6 gap on localizable validation errors.
  ⚠ OPEN — docs/adr/ is empty. ARCHITECTURE.md §9 lists ADR-0001..0010 as "will live in
    docs/adr/". Principle-level decisions are currently only recorded in prose. Two further
    candidates surfaced in TASKS.md: PriceTier aggregate boundary (US-2.3) and the reset-password
    Option A/B decision (US-1.3).
-->

# Eventify Constitution

Eventify is a distributed ticket-booking platform built as a **portfolio and learning project**.
This constitution records the rules that are not up for renegotiation per feature. Everything else
is a judgement call.

## Core Principles

### I. Learning Surface Over Production Scale

Eventify exists to demonstrate and build middle-to-senior competence in distributed systems. When
a production-grade solution and a simpler solution both satisfy the requirement, **the simpler one
wins** — unless the complexity *is* the learning objective, in which case it MUST be named as such.

- Architectural complexity MUST be justified by either a functional requirement or an explicit
  learning goal. "It's how big systems do it" is not a justification.
- Multi-region, blue-green deployment, horizontal autoscaling, and similar production concerns are
  **out of scope** and MUST NOT be introduced.
- Every explanation of new work MUST teach: define terms on first use, explain the *why* and the
  trade-offs, and let the developer write the code. Delivering a finished implementation to
  copy-paste defeats the project's purpose.

**Rationale**: This is the principle that resolves every other trade-off. Without it, a learning
project accretes production ceremony until it teaches nothing and ships nothing.

### II. Bounded Context Autonomy

Each service owns its data absolutely. There is no shared database and no distributed transaction.

- Each service MUST have its own logical database. Cross-database queries and cross-service foreign
  keys are FORBIDDEN.
- A service MUST NOT read another service's tables. Data crosses a boundary in exactly two ways:
  **gRPC** for internal synchronous reads, **RabbitMQ integration events** for asynchronous
  propagation.
- Data duplicated across a boundary MUST be treated as a local copy that is eventually consistent,
  never as a live reference. Values that must not drift (e.g. a reserved seat's price) MUST be
  **captured at the time of the decision**, not re-read later.
- Consistency is **strong within a service** (DB transaction) and **eventual between services**.
  Any design that needs strong consistency across services is wrong and MUST be redesigned.

**Rationale**: This is the one property that makes the system genuinely microservices rather than a
distributed monolith. Violating it is not a shortcut — it removes the reason the project exists.

### III. The Dependency Rule

Layering is mechanically enforced, not a matter of discipline.

- `Domain` has **zero** project dependencies. `Application` → `Domain` only. `Infrastructure` →
  `Domain` + `Application`. `Api` → all three.
- These rules MUST be enforced by NetArchTest in CI, not by code review. A layering violation MUST
  fail the build.
- Clean Architecture (4 projects) is used where complexity earns it — Identity, Catalog, Booking,
  Payment. VSA (single project) is used where it does not — Ticket, Notification. Choosing the
  heavier style for a simple service is a violation of Principle I.
- Naming is `Eventify.{ServiceName}.{Layer}`; namespace mirrors folder path; one aggregate per
  folder in Domain; one handler per file in Application.

**Rationale**: Layering that is merely documented decays silently. Layering that fails the build
cannot.

### IV. Explicit Over Implicit

The codebase prefers compile-time, greppable, obvious constructs over runtime convention and magic.

- **Manual mapping only.** `ToDto` / `ToDomain` / `ToIntegrationEvent` static extension methods
  colocated with the DTO. AutoMapper, Mapster, and Mapperly are FORBIDDEN — full IDE refactoring
  and compile-time safety are worth the boilerplate at this aggregate size.
- **Classic constructors only** on classes: explicit `private readonly` fields assigned in a
  constructor body. Primary constructors on classes are FORBIDDEN. Positional records
  (`record struct ArtistId(Guid Value)`) are fine.
- Value Objects over primitive pairs: money is `Money(decimal, string)`, never a loose
  `decimal` + `string`. IDs are strongly typed (`readonly record struct`), never bare `Guid`.
- Behaviour that is easy to grep beats behaviour that is easy to write. If a reader must know a
  convention to find where something happens, prefer the explicit alternative.

**Rationale**: In a project whose purpose is understanding, an abstraction that hides the mechanism
costs more than the code it saves.

### V. Errors Are Values

Expected failure is a return value. Exceptions signal bugs.

- Application handlers MUST return `ErrorOr<TResult>`. Business failures use
  `Error.NotFound / Conflict / Validation / Unauthorized / Failure`.
- Handlers MUST NOT contain `try`/`catch` for business flow. Endpoints terminate with
  `result.Match(success, errs => errs.ToProblemDetails())`.
- `DomainException` is reserved for invariant violations that indicate a **programming error** —
  a state the domain should have made unreachable. It MUST NOT be used for user-facing validation.
- Validation failures MUST carry machine-readable codes and parameters, not only a pre-formatted
  English string, so clients can localize them.

**Rationale**: Exceptions as control flow hide the failure modes of a distributed system exactly
where they most need to be visible in the type signature.

### VI. Transactional Event Integrity

No state change is published without having been committed, and no message is trusted to arrive
exactly once.

- Every publishing service MUST use the MassTransit Transactional Outbox. The domain change and
  the outbox row MUST commit in the **same database transaction**. Publishing directly to RabbitMQ
  from a handler is FORBIDDEN (the dual-write problem).
- Domain events dispatch **pre-save** (`SavingChangesAsync`), because EF Core detaches deleted
  entities after `SaveChanges` — post-save dispatch would silently lose deletion events — and
  because handlers writing to the outbox must join the same transaction.
- Message delivery is **at-least-once**. Every consumer of an externally-triggered event (Stripe
  webhooks, saga commands, integration events) MUST be idempotent, keyed on a stable identifier.
- Audit fields (`CreatedAt`/`UpdatedAt`) are populated by an EF Core interceptor. Services MUST NOT
  override `SaveChangesAsync` to do it themselves.
- Cross-service workflows MUST be **orchestrated** by the Booking saga, not choreographed, so the
  full lifecycle is debuggable from one place.

**Rationale**: These are the failure modes that make distributed systems hard, and every one of
them is silent. They must be structural guarantees, not things a reviewer might notice.

### VII. Tests After Code, Before Merge

**TDD is explicitly rejected on this project.** Architecture is still forming; tests written first
would be rewritten constantly as aggregates and endpoints evolve.

- Production code is written first. Tests follow **in the same pull request**, before merge. "Tests
  in a follow-up PR" is not acceptable — that is untested code on `main`.
- This ordering applies to **all layers**, Domain included.
- Unit tests cover Domain invariants and Application handlers with mocked dependencies.
- Integration tests MUST run against **real infrastructure** via Testcontainers (Postgres,
  RabbitMQ, Redis). Mocking the database in an integration test is FORBIDDEN — it tests nothing.
- Architecture tests (NetArchTest) run on every PR; integration tests are gated to PRs into `main`
  to keep feedback fast.
- Coverage target: Domain and Application 80%+. Tracked, not gated. Fragile tests are worse than
  missing ones.

**Rationale**: The value of a test is proportional to the stability of what it describes. Writing
tests against an interface that will change tomorrow buys churn, not safety.

### VIII. One Visual Language

The React SPA and the Identity Server Razor Pages are one product to the user and MUST look like it.

- **Brand**: indigo `#6366F1` paired with violet `#8B5CF6`. A second brand hue MUST NOT be
  introduced. Dark surface `#08080F`; glassmorphism surfaces; dark is the default theme.
- **Typography**: JetBrains Mono for wordmark, headings, prices, dates, ticket codes, and caps
  labels; Plus Jakarta Sans for body text. JetBrains Mono MUST NOT be used for long-form text.
- **Localization is not optional and never deferred.** Every user-facing string goes through
  i18next (SPA) or `.resx` (Razor Pages). `en` and `uk` keys MUST be added **in the same PR** as
  the feature. Ukrainian is labelled **UK** (language code), never "UA" (country code).
- Hardcoded UI strings are FORBIDDEN.
- **No CDN dependency may ship to production.** The current `cdn.tailwindcss.com` reference in
  Identity Server's `_Layout.cshtml` is a known, tracked debt (TASKS.md US-1.4) and MUST be paid
  before any production deployment of that service.
- Motion MUST be guarded by `@media (prefers-reduced-motion: reduce)`.

**Rationale**: Two authentication surfaces that look like different products undermine every
trust signal the OAuth flow is supposed to establish. Localization added later is localization
never added.

## Technology Constraints

Locked decisions. Changing any of these requires an amendment plus an ADR.

| Area | Constraint |
|---|---|
| Runtime | .NET 10 (`net10.0`), SDK pinned to `10.0.107` via `global.json` |
| Language | C# `latest`, nullable reference types enabled, implicit usings enabled |
| Packages | Central versioning via `Directory.Packages.props`. Individual `.csproj` files MUST NOT specify versions |
| Identifiers | `Guid.CreateVersion7()` (UUIDv7 — time-sortable, B-tree friendly) for every entity ID |
| Mediation | MediatR **pinned at 12.2**. `RequestHandlerDelegate<TResponse>` takes no `CancellationToken` at this version, so behaviors call `next()`. Bumping to 12.5+ requires updating `LoggingBehavior` and `ValidationBehavior` |
| Endpoints | Minimal APIs via Carter (`ICarterModule` per aggregate). **No MVC controllers anywhere.** No FastEndpoints |
| API shape | URL-segment versioning (`/v1/...`); offset pagination via `PagedResult<T>`; default `pageSize=20`, max `100` |
| Persistence | PostgreSQL 17, one logical database per service; EF Core migrations applied on startup |
| Messaging | RabbitMQ + MassTransit 8.5 |
| Identity | Duende IdentityServer 7; OAuth2 Authorization Code + PKCE |
| SPA | Vite + React 19 + TypeScript, Tailwind CSS v4, `oidc-client-ts`. Components as `const Foo: FC = () => {}`; double quotes; semicolons |
| Orchestration | No .NET Aspire. Services run independently via `launchSettings.json` profiles |
| Secrets | .NET User Secrets in dev. Secrets MUST NOT be committed — no `.env`, no keys in `appsettings.json` |

## Development Workflow

**Collaboration model** — the developer writes the code; the assistant explains, reviews, and
advises.

- Reading the repository (Read, Grep, Glob, read-only shell) is always permitted without asking.
- Modifying project source, and any git operation (commit, push, branch, reset), MUST be confirmed
  by the developer first. Existing files MUST NOT be overwritten without an explicit warning.
- For a new service or feature: the assistant explains the flow and architectural reasoning → the
  developer implements → the assistant reviews. A complete implementation MUST NOT be handed over
  up front (Principle I).
- Technical honesty overrides agreement. A weak, risky, or over-engineered proposal MUST be
  challenged with reasoning, including when the developer proposed it. Uncertainty MUST be stated
  as uncertainty; APIs, package capabilities, and framework behaviour MUST NOT be invented.

**Feature flow**: `/speckit-specify` → `/speckit-clarify` (optional) → `/speckit-plan` →
`/speckit-tasks` → `/speckit-implement`. Specs live in `specs/###-feature-name/`.

**Branching**: one branch per Roadmap phase, as tracked in `docs/TASKS.md`. Unrelated work MUST NOT
share a branch.

**Decision records**: non-trivial architectural choices are recorded as ADRs in `docs/adr/` as they
are implemented — not all upfront. `docs/ARCHITECTURE.md` remains the source of truth for *what* to
build; `docs/TASKS.md` for *how work is sequenced*.

**Documentation drift**: when code and `docs/ARCHITECTURE.md` disagree, the disagreement MUST be
resolved rather than tolerated — either fix the code or correct the document.

## Governance

This constitution supersedes ad-hoc practice. Where it conflicts with `CLAUDE.md`,
`docs/ARCHITECTURE.md`, or a Spec Kit template, **this document wins** and the other artifact MUST
be corrected.

**Amendment procedure**

1. Propose the change with explicit rationale and the migration impact on existing code.
2. Update this file, incrementing the version per the policy below.
3. Propagate to dependent artifacts in the same change: `.specify/templates/*`, `CLAUDE.md`,
   `docs/ARCHITECTURE.md`, and any open `specs/*/plan.md` whose Constitution Check is affected.
4. Record the change in the Sync Impact Report at the top of this file.

**Versioning policy** (semantic)

- **MAJOR** — a principle is removed or redefined in a backward-incompatible way; existing code
  becomes non-compliant.
- **MINOR** — a principle or section is added, or existing guidance is materially expanded.
- **PATCH** — clarification, wording, or typo fixes with no change in meaning.

**Compliance review**

- Every `/speckit-plan` MUST evaluate its Constitution Check against this file and MUST NOT record
  a pass it cannot justify. A principle that does not apply to a given feature is marked **N/A**
  with a reason — not silently skipped.
- Any deviation MUST be recorded in that plan's Complexity Tracking table with the simpler
  alternative that was rejected and why.
- Principles III (layering) and VII (integration tests against real infrastructure) are enforced
  mechanically in CI. The rest are enforced at review time.
- An unjustifiable violation blocks the merge.

**Version**: 1.0.0 | **Ratified**: 2026-07-27 | **Last Amended**: 2026-07-27
