# Eventify — Roadmap to Project Completion

> As of 2026-07-11. Phase order matters: each phase builds on the previous one.
> Mark items `[x]` as you go. Architecture details live in `ARCHITECTURE.md`.

## Priority legend

- 🔴 — blocks other phases
- 🟡 — required for the end-to-end flow
- 🟢 — quality / polish, order is flexible

---

## Phase 1 — Finish Identity 🔴 (branch `identity`)

Goal: fully working OAuth2/OIDC flow between the SPA and Identity Server, branch merged into `main`.

**📚 Topics to know:**

- OAuth2 Authorization Code flow + PKCE (why implicit flow is dead, what the code verifier/challenge protect against)
- OpenID Connect: `id_token` vs `access_token`, scopes, claims, discovery document (`/.well-known/openid-configuration`)
- Duende IdentityServer concepts: Client, ApiScope, IdentityResource, interaction service
- ASP.NET Core Razor Pages: PageModel, handler methods (`OnGet`/`OnPost`), model binding, `[BindProperty]`
- ASP.NET Core localization: `IStringLocalizer`, `.resx` resources, request culture providers, culture cookie
- Cookie authentication vs token authentication (why Identity Server uses a cookie for its own pages)
- CORS: preflight requests, why the token endpoint needs CORS for a browser SPA

### Tasks

- [ ] **1.1 Localize the Register page**
    - Add missing keys to `Captions.resx` + `Captions.uk-UA.resx` (files already modified — finish the job)
    - Verify the language switcher on the Register page (EN / UK, never "UA")
- [ ] **1.2 End-to-end OIDC flow check**
    - SPA → Login button → redirect to `https://localhost:5001` → login → Callback → SPA with a token
    - Verify: client redirect URIs, CORS, scopes, contents of `id_token`/`access_token`
    - Logout: SPA → Identity → back to SPA, session destroyed on both sides
    - Register → auto-login or redirect to Login (verify the chosen scenario)
- [ ] **1.3 Decision on Forgot/Reset Password**
    - Option A: implement now (requires an email channel — depends on MailHog)
    - Option B: defer to Phase 5 (Notification), record the decision here
    - Decision: _(fill in)_
- [ ] **1.4 Tech debt: remove Tailwind CDN from `_Layout.cshtml`**
    - Move the used utility classes (`w-full`, `max-w-md`, `mb-8`, `space-y-5`, `text-center`) into `auth.css`
    - The CDN must never reach a production build
- [ ] **1.5 Close out the branch**
    - Commit the Register pages and localization
    - PR `identity` → `main`, merge

---

## Phase 2 — Complete Catalog 🔴

Goal: full catalog domain model + first integration events + gRPC server.
Template for new aggregates — the existing `Artists/` slice (Domain → Application → Endpoints).

**📚 Topics to know:**

- DDD tactical patterns: Aggregate, Aggregate Root, Entity vs Value Object, invariants, consistency boundary
- Choosing aggregate boundaries (why PriceTier inside Session vs a separate aggregate is a consistency question)
- State machines in the domain: modeling status transitions (Draft → Published → Cancelled) with guarded methods
- Domain events vs integration events (in-process vs cross-service, and why they are different types)
- EF Core: owned entities (`OwnsOne`), value conversions, migrations, indexes, `ISaveChangesInterceptor`
- Transactional Outbox pattern: the dual-write problem it solves, how MassTransit implements it with EF Core
- RabbitMQ basics: exchanges, queues, bindings; what MassTransit abstracts away
- gRPC: protobuf contracts (`.proto`), unary calls, HTTP/2, gRPC vs REST for internal communication
- CQRS with MediatR: commands vs queries, pipeline behaviors (validation, logging)
- FluentValidation basics

### Tasks

- [ ] **2.1 Venue aggregate**
    - Domain: `Venue`, `VenueId`, address as a Value Object, capacity; Created/Updated/Deleted domain events
    - Application: CRUD commands + queries with validators (follow the Artist pattern)
    - Api: `VenueModule` (Carter), request/response types
- [ ] **2.2 Event aggregate**
    - Domain: `Event`, link to `ArtistId`, statuses (Draft → Published → Cancelled) with transition invariants
    - Application: Create/Update/Publish/Cancel commands + queries
    - Api: `EventModule`
- [ ] **2.3 Session + PriceTier**
    - `Session`: date/time, Event + Venue link, seat count
    - `PriceTier`: name, `Money` VO (never raw decimal!), seat count per tier
    - Decide: is PriceTier a child entity of Session or a separate aggregate (justify via consistency)
- [ ] **2.4 EF Core: configurations + migrations**
    - `OwnsOne` for `Money` (`*_amount` + `*_currency` columns) and for the address
    - Indexes: FK columns, list-filtering fields
- [ ] **2.5 First integration events (RabbitMQ + Outbox)**
    - Contracts in `Eventify.IntegrationEvents`: `EventPublishedIntegrationEvent`, etc.
    - MassTransit Transactional Outbox in Catalog (domain event → handler → outbox row in the same transaction)
    - Verify delivery via the RabbitMQ Management UI
- [ ] **2.6 gRPC server in Catalog**
    - `.proto`: `GetSessionInfo`, `GetPriceTiers` (consumers: Booking and Ticket in later phases)
    - Grpc.AspNetCore, dedicated port/protocol in `launchSettings.json`
- [ ] **2.7 Catalog unit tests**
    - Aggregate invariants (Event status transitions, Money/address validation)
    - Handlers with mocked dependencies (`Eventify.Catalog.UnitTests` already exists)

---

## Phase 3 — Booking 🔴 (the hardest service)

Goal: seat reservation with TTL, orchestration saga, real-time seat map updates.
Before starting: re-read `ARCHITECTURE.md` §7 (saga state diagram).

**📚 Topics to know:**

- Distributed locking: why a single-node lock is not enough, the RedLock algorithm, lock TTL and fencing concerns
- Redis fundamentals: key expiration (TTL), atomic operations, `SETNX` semantics
- Saga pattern: orchestration vs choreography, compensating actions, why long-running transactions can't use 2PC
- MassTransit Automatonymous: states, events, transitions, saga state persistence (EF Core saga repository)
- Message delivery guarantees: at-least-once, idempotent consumers, retries and error queues
- Race conditions and optimistic vs pessimistic concurrency (two users grabbing the same seat)
- SignalR: hubs, groups, connection lifecycle; why a Redis backplane is needed for multiple instances
- gRPC client usage in .NET (typed clients, deadlines, error handling)
- Testcontainers: spinning up Postgres/RabbitMQ/Redis in integration tests

### Tasks

- [ ] **3.1 Service skeleton**
    - 4 Clean Architecture projects following the Catalog layout, add to `Eventify.slnx`
    - Postgres DB, `BaseDbContext`, SharedKernel interceptors
- [ ] **3.2 Domain: Reservation + Booking**
    - `Reservation`: seat list, TTL deadline, statuses (Active → Confirmed / Expired)
    - `Booking`: statuses per the saga diagram, transition invariants
- [ ] **3.3 Redis seat reservation (RedLock + TTL)**
    - Theory before code: why a distributed lock, why TTL, what happens when an instance dies
    - Keys like `seat:{sessionId}:{seatId}`, atomic acquisition of multiple seats
- [ ] **3.4 gRPC client to Catalog**
    - Validate session and current prices when creating a reservation (price is locked at booking time)
- [ ] **3.5 MassTransit Saga (Automatonymous StateMachine)**
    - States and transitions strictly per the §7 diagram
    - Saga commands → Payment; compensations on timeout/payment failure (release seats)
    - Saga state persistence in Postgres (EF Core saga repository)
- [ ] **3.6 SignalR seat map hub**
    - "Seat taken/released" events for clients subscribed to a session
    - Redis backplane (scaling to multiple instances)
- [ ] **3.7 Integration tests**
    - Testcontainers: Postgres + RabbitMQ + Redis
    - Scenarios: happy-path reservation; two users competing for one seat; TTL expiration

---

## Phase 4 — Payment 🟡

Goal: real (test-mode) payment via Stripe, wired into the saga.

**📚 Topics to know:**

- Stripe payment model: PaymentIntent lifecycle, client secret, test mode and test cards
- Webhooks: why polling doesn't work, signature verification (`Stripe-Signature`), replay attacks
- Idempotency: idempotency keys, handling duplicate webhook deliveries
- Secret management: user-secrets in dev, environment variables, why secrets never go into git
- Outbox pattern (recap from Phase 2) applied to webhook-driven event publishing

### Tasks

- [ ] **4.1 Service skeleton** (Clean Architecture, Postgres, Outbox)
- [ ] **4.2 Stripe PaymentIntent**
    - Test mode, keys via user-secrets (never in git)
    - Create a PaymentIntent on a saga command, return the client_secret
- [ ] **4.3 Webhook endpoint**
    - Stripe signature verification (`Stripe-Signature`), idempotent processing
    - `payment_intent.succeeded` / `payment_intent.payment_failed` → integration events via Outbox
- [ ] **4.4 Saga command consumers**
    - Handle `ProcessPayment`, publish `PaymentSucceeded`/`PaymentFailed`

---

## Phase 5 — Ticket + Notification 🟡 (VSA, lighter)

Goal: complete the "booking → ticket → email" chain.

**📚 Topics to know:**

- Vertical Slice Architecture: how it differs from Clean Architecture, when a single project is enough
- QR codes: what goes inside (payload design), signing the payload to prevent forgery (HMAC)
- Ticket validation semantics: one-time use, state transition Issued → Used, race on double scan
- Email delivery: SMTP basics, MailHog as a dev catch-all, transactional email providers (SendGrid)
- Consumer-driven design: reacting to integration events instead of being called directly

### Tasks

- [ ] **5.1 Ticket: ticket generation**
    - VSA single-project skeleton
    - Consumer of `BookingConfirmed` → create a ticket with a QR code (QR payload: ticketId + signature)
    - gRPC client to Catalog (event/session name for the ticket)
- [ ] **5.2 Ticket: validation**
    - QR validation endpoint (one-time use: status Issued → Used)
- [ ] **5.3 Notification**
    - VSA skeleton, consumers: `BookingConfirmed`, `PaymentFailed`, (optionally `UserRegistered`)
    - MailHog in dev; `IEmailSender` abstraction for a future SendGrid
    - If Phase 1 deferred Reset Password — implement its email here

---

## Phase 6 — API Gateway + SPA features 🟡

Goal: the SPA works through a single entry point and covers the full user flow.

**📚 Topics to know:**

- Reverse proxy concept; YARP: routes, clusters, transforms
- JWT validation at the gateway vs at each service (passthrough vs termination trade-offs)
- React data fetching: effects vs data-fetching libraries, loading/error states, aborting requests
- Pagination UX with `PagedResult<T>` (page state in the URL)
- SignalR JavaScript client: connection lifecycle, reconnect, subscribing to groups
- Stripe Elements / Payment Element integration in React
- Countdown timers in React (TTL display): intervals, cleanup, drift
- i18next: namespaces, interpolation, plurals — keys always added to `en` and `uk` together

### Tasks

- [ ] **6.1 YARP Gateway**
    - Project in `src/ApiGateway` (folder exists, empty), routes to all services
    - JWT passthrough, CORS policy for the SPA
- [ ] **6.2 SPA: events list**
    - Real Catalog data through the Gateway, `PagedResult<T>` pagination
    - Event cards per the design system (glassmorphism, JetBrains Mono for dates/prices)
- [ ] **6.3 SPA: event page + seat map**
    - Session selection → seat map → SignalR subscription (live seat statuses)
- [ ] **6.4 SPA: booking and payment flow**
    - Reservation → on-screen TTL timer → Stripe Elements → confirmation
    - Error handling: seat already taken, payment declined, reservation expired
- [ ] **6.5 SPA: My Tickets**
    - Ticket list with QR codes (replace the `MyTicketsPage.tsx` placeholder)
- [ ] **6.6 Localization**
    - All new screens: keys added to `en/common.json` and `uk/common.json` at the same time
- [ ] **6.7 Structured validation-error contract (metadata)**
    - When the SPA starts consuming Identity/Catalog validation endpoints, upgrade `ValidationBehavior` to carry
      `f.FormattedMessagePlaceholderValues` into `Error.Validation(..., metadata)`
    - API returns stable code + parameters (e.g. `{ maxLength: 255 }`); each client localizes itself
      (Razor via `Captions`, SPA via i18next interpolation `{{maxLength}}`)
    - Deferred from Phase 1: validation messages currently drop placeholders because errors render under each
      field, so field/number context was redundant for the Razor-only flow

---

## Phase 7 — Quality and wrap-up 🟢

**📚 Topics to know:**

- Architecture testing with NetArchTest: asserting dependency rules in CI
- Integration testing strategy: what to cover end-to-end vs at unit level, avoiding fragile tests
- Docker Compose: services, networks, volumes, healthchecks
- GitHub Actions: workflow triggers, jobs, caching NuGet/npm, running Testcontainers in CI
- Writing a portfolio README: architecture diagram, run instructions, screenshots

### Tasks

- [ ] **7.1 Architecture tests**
    - `Eventify.ArchitectureTests` project (declared in CLAUDE.md, not created yet)
    - NetArchTest: Domain has no dependencies, Application → Domain only, etc.
- [ ] **7.2 Integration tests for critical flows**
    - Saga happy path end-to-end; compensation on payment failure
- [ ] **7.3 docker-compose for infrastructure**
    - Postgres, RabbitMQ, Redis, MailHog — one `docker compose up` for the dev environment
- [ ] **7.4 CI (GitHub Actions)**
    - build + unit tests + architecture tests on every PR
- [ ] **7.5 Portfolio README**
    - Architecture overview with a diagram, run instructions, SPA and Razor Pages screenshots
    - Links to ADRs and `ARCHITECTURE.md`

---

## Decision log

| Date | Item | Decision |
|------|------|----------|
| _(entry)_ | 1.3 | _(Reset Password: now / Phase 5)_ |
