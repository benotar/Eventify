# Eventify — Roadmap & Backlog

> As of 2026-07-27. Derived from `ARCHITECTURE.md` (source of truth for *what* to build) — this file tracks
> *how the work is sequenced and broken down*.
>
> **Structure:**
> - **Roadmap** — the delivery timeline (Phases, one per branch/milestone). High-level: goal, epics touched, demo.
> - **Backlog** — every Epic broken into **User Stories** (persona + Given/When/Then acceptance criteria) broken
>   into **implementation Tasks** (checkboxes — mark `[x]` as you go).
>
> An Epic = one bounded context / capability area (mostly 1:1 with the services in `ARCHITECTURE.md` §4). A Phase
> in the Roadmap pulls in one or more Epics in delivery order — Epics don't ship standalone, Phases do.

## Priority legend

- 🔴 — blocks other phases
- 🟡 — required for the end-to-end flow
- 🟢 — quality / polish, order is flexible

---

## Roadmap

| Phase | Branch/Milestone | Priority | Epics | Goal | Demo |
|---|---|---|---|---|---|
| 0 — Foundation | `main` | 🔴 | [E0](#epic-0--foundation--buildingblocks) | Solution skeleton + shared building blocks | ✅ Done |
| 1 — Identity | `identity` | 🔴 | [E1](#epic-1--identity-service) | Full OAuth2/OIDC flow, SPA ⇄ Identity Server | Login/register/logout round-trips with a real token |
| 2 — Catalog | `catalog` | 🔴 | [E2](#epic-2--catalog-service) | Full catalog domain + first integration events + gRPC server | Browse events/sessions from seeded data via REST |
| 3 — Booking | `booking` | 🔴 | [E3](#epic-3--booking-service) | Seat reservation w/ TTL, saga orchestration, live seat map | Reserve seats, see them go red on another tab in real time |
| 4 — Payment | `payment` | 🟡 | [E4](#epic-4--payment-service) | Real (test-mode) Stripe payment wired into the saga | Pay with a Stripe test card, booking confirms |
| 5 — Ticket & Notification | `ticket-notification` | 🟡 | [E5](#epic-5--ticket-service), [E6](#epic-6--notification-service) | Booking → ticket → email chain complete | MailHog shows a QR-coded ticket email after payment |
| 6 — Gateway & SPA | `gateway-spa` | 🟡 | [E7](#epic-7--api-gateway), [E8](#epic-8--spa--frontend) | Single entry point; full user flow in the browser | Browse → reserve → pay → view ticket, all through the Gateway |
| 7 — Observability | `observability` | 🟢 | [E9](#epic-9--observability--telemetry) | Centralized logs, tracing, metrics | One trace in Jaeger spanning 6 services + 8 message hops |
| 8 — Deployment & Quality | `deploy-quality` | 🟢 | [E10](#epic-10--devops-testing--deployment) | Containerized system, CI green, architecture tests enforced, K8s optional | `docker compose up` runs the whole system; Grafana shows live metrics |

Phase order matters — each phase builds on the previous one. Within a phase, stories can reorder freely unless a
task explicitly depends on another (noted inline).

---

## Backlog

### Epic 0 — Foundation / BuildingBlocks

**Status:** ✅ Done. Kept here for traceability since the rest of the backlog depends on it.

- [x] Solution + projects + `Directory.Build.props` + `Directory.Packages.props` + `global.json`
- [x] `Eventify.SharedKernel` — `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `DomainEvent`, `IAuditable`,
      `IClearableAggregate`, `BaseDbContext`, `UpdateAuditableInterceptor`, `PublishDomainEventsInterceptor`,
      `ICommand`/`IQuery<T>`, `LoggingBehavior`, `ValidationBehavior`
- [x] `Eventify.IntegrationEvents` — `IntegrationEvent` abstract record (UUIDv7), no deps

---

### Epic 1 — Identity Service

**Branch:** `identity` · **Depends on:** E0 · **ARCHITECTURE ref:** §4.1, §8.1

**📚 Topics to know:** OAuth2 Authorization Code flow + PKCE; OIDC `id_token` vs `access_token`, scopes, claims,
discovery document; Duende IdentityServer concepts (Client, ApiScope, IdentityResource, interaction service);
Razor Pages (`PageModel`, `OnGet`/`OnPost`, `[BindProperty]`); ASP.NET Core localization (`IStringLocalizer`,
`.resx`, culture providers, culture cookie); cookie vs token authentication; CORS preflight for the token endpoint.

#### US-1.1 — Localized registration

*As a Customer, I want the Register page fully localized in EN/UK so the account-creation flow feels native in
my language.*

```gherkin
Given the browser's preferred language is Ukrainian
When I open the Register page
Then all labels, validation messages, and the language switcher pill read "UK" (never "UA") in Ukrainian
And switching to EN re-renders the same page in English without losing entered form values
```

- [ ] Add missing keys to `Captions.resx` + `Captions.uk-UA.resx` (files already touched — finish the job)
- [ ] Verify the language switcher on the Register page (EN / UK pill, matches SPA design system)

#### US-1.2 — End-to-end OIDC round-trip

*As a Customer, I want to log in through Identity Server from the SPA and come back with a valid session so I can
access protected areas of the app.*

```gherkin
Given I am signed out in the SPA
When I click "Login" and complete the Duende IdentityServer login form
Then I am redirected back to the SPA callback route with a valid id_token and access_token
And the token's scopes/claims match what the SPA client requests

Given I am signed in
When I click "Logout"
Then both the SPA session and the Identity Server cookie session are destroyed
```

- [ ] SPA → Login button → redirect to `https://localhost:5001` → login → callback → SPA holds a token
- [ ] Verify client redirect URIs, CORS policy, and scopes against `ARCHITECTURE.md` §8.1
- [ ] Inspect `id_token`/`access_token` contents (claims, expiry) match expectations
- [ ] Logout: SPA → Identity → back to SPA, session destroyed on both sides
- [ ] Register → confirm chosen behavior (auto-login vs redirect to Login) works end-to-end

#### US-1.3 — Forgot/Reset Password decision

*As a Customer who forgot their password, I want a way back into my account — OR the team explicitly defers this
to when email infrastructure exists.*

```gherkin
Given the decision below is "Option A"
When a user requests a password reset
Then a reset email is sent (requires MailHog, i.e. depends on Epic 6) and the flow completes

Given the decision is "Option B"
Then no reset UI ships in Phase 1, and this story is re-opened in Epic 6 (Notification)
```

- [ ] Decide: **Option A** (implement now, needs an email channel — blocked on Notification) vs **Option B**
      (defer to Epic 6, record decision in the Decision log below)
- [ ] If Option A: implement the reset flow; if Option B: link this story from Epic 6 instead

#### US-1.4 — Production-safe styling

*As a maintainer, I want the Identity Razor Pages free of the Tailwind CDN so nothing points at an external CDN
in production.*

```gherkin
Given a production build of Identity Server
When `_Layout.cshtml` is rendered
Then no request is made to cdn.tailwindcss.com
And the previously CDN-served utility classes (w-full, max-w-md, mb-8, space-y-5, text-center) still render
    correctly from auth.css
```

- [ ] Move the used utility classes into `auth.css`
- [ ] Remove the `<script src="https://cdn.tailwindcss.com">` tag from `_Layout.cshtml`

#### US-1.5 — Ship the branch

*As a maintainer, I want the finished Identity work merged into `main` so downstream phases can build on it.*

```gherkin
Given US-1.1 through US-1.4 are complete (or explicitly deferred per US-1.3)
When the identity branch is opened as a PR against main
Then CI is green and the PR is merged
```

- [ ] Commit the Register pages and localization
- [ ] Open PR `identity` → `main`, merge

---

### Epic 2 — Catalog Service

**Branch:** `catalog` · **Depends on:** E0 · **ARCHITECTURE ref:** §4.2, §5.1

**📚 Topics to know:** DDD tactical patterns (Aggregate, Entity vs Value Object, invariants, consistency
boundary); choosing aggregate boundaries; domain-modeled state machines (Draft → Published → Cancelled); domain
events vs integration events; EF Core owned entities (`OwnsOne`), value conversions, migrations, indexes; the
Transactional Outbox pattern and the dual-write problem it solves; RabbitMQ basics (exchanges, queues, bindings);
gRPC (`.proto`, unary calls, HTTP/2, gRPC vs REST); CQRS with MediatR; FluentValidation.

*Reference implementation: the existing `Artists/` slice (Domain → Application → Endpoints) is the template for
every story below.*

#### US-2.1 — Manage venues

*As an Admin, I want to create and maintain Venues with an address and capacity so Sessions can be scheduled at
real locations.*

```gherkin
Given I am authenticated as Admin
When I create a Venue with a valid address and capacity
Then the Venue is persisted with a UUIDv7 id and CreatedAt is stamped by the audit interceptor

Given an existing Venue
When I submit an update with an invalid address (e.g. missing country)
Then the request fails validation and no partial write occurs
```

- [ ] Domain: `Venue`, `VenueId`, address as a Value Object, capacity; Created/Updated/Deleted domain events
- [ ] Application: CRUD commands + queries with validators (follow the Artist pattern)
- [ ] Api: `VenueModule` (Carter), request/response types

#### US-2.2 — Manage events and their lifecycle

*As an Admin, I want to create Events linked to an Artist and move them through Draft → Published → Cancelled so
only vetted Events are visible to Customers.*

```gherkin
Given a Draft Event
When I call Publish
Then the Event transitions to Published and a PublishedDomainEvent is raised

Given a Published Event
When I call Publish again
Then the transition is rejected (invalid state transition) via a typed ErrorOr error, not an exception

Given a Cancelled Event
When a Customer requests it via GET /events/{id}
Then it is excluded from the public catalog (or returned with Cancelled status, per the chosen visibility rule)
```

- [ ] Domain: `Event`, link to `ArtistId`, statuses (Draft → Published → Cancelled) with transition invariants
- [ ] Application: Create/Update/Publish/Cancel commands + queries
- [ ] Api: `EventModule`

#### US-2.3 — Sessions and price tiers

*As an Admin, I want to schedule Sessions for an Event at a Venue with per-section pricing so Customers can pick
a date/time and see the price for each seat category.*

```gherkin
Given an Event and a Venue with a published SeatLayout
When I create a Session with a start time and a PriceTier per SectionCategory
Then every SectionCategory in the layout has a matching PriceTier (no gaps)

Given a Session start time in the past
When I try to create it
Then the request is rejected by a domain invariant
```

- [ ] `Session`: date/time, Event + Venue link, seat count
- [ ] `PriceTier`: name, `Money` VO (never raw decimal!), seat count per tier
- [ ] Decide: is PriceTier a child entity of Session or a separate aggregate — justify via consistency boundary,
      record the reasoning (candidate ADR)

#### US-2.4 — Persistence mapping

*As a developer, I want Money and Address correctly mapped by EF Core so domain Value Objects round-trip to
Postgres without leaking primitives.*

```gherkin
Given a Session with a PriceTier of Money(200, "USD")
When it is saved and reloaded
Then the price_amount and price_currency columns round-trip to the same Money value

Given a query filtering Sessions by EventId
Then the query uses the FK index (verified via EXPLAIN)
```

- [ ] `OwnsOne` for `Money` (`*_amount` + `*_currency` columns) and for the address
- [ ] Indexes: FK columns, list-filtering fields
- [ ] EF Core migrations checked in and applied via `db.Database.Migrate()` on startup

#### US-2.5 — Publish catalog integration events

*As the Booking service (consumer), I want to be notified when a Session is published, cancelled, or repriced so
I can react without polling Catalog.*

```gherkin
Given a Session is published
When the transaction commits
Then a SessionPublishedIntegrationEvent row exists in the outbox table in the same transaction
And it is visible on the RabbitMQ Management UI shortly after (delivered at-least-once)
```

- [ ] Contracts in `Eventify.IntegrationEvents`: `EventPublishedIntegrationEvent`,
      `SessionPublishedIntegrationEvent`, `SessionCancelledIntegrationEvent`, `PricesUpdatedIntegrationEvent`
- [ ] MassTransit Transactional Outbox in Catalog (domain event → handler → outbox row, same transaction)
- [ ] Verify delivery via the RabbitMQ Management UI

#### US-2.6 — Serve session/seat data over gRPC

*As the Booking and Ticket services, I want a fast internal lookup for session details and seat validation so I
don't pay REST/JSON overhead for internal calls.*

```gherkin
Given a valid SessionId
When Booking calls GetSessionDetails via gRPC
Then it receives session, venue, and price-tier data in under the configured deadline

Given a SeatId that does not belong to the Session's layout
When ValidateSeats is called
Then it returns a failure result, not a thrown exception
```

- [ ] `.proto`: `GetSessionInfo`, `GetPriceTiers`, `ValidateSeats` (consumers: Booking, Ticket in later phases)
- [ ] `Grpc.AspNetCore` server, dedicated port/protocol in `launchSettings.json`

#### US-2.7 — Catalog test coverage

*As a maintainer, I want Catalog's invariants and handlers covered by tests so regressions are caught before
they reach Booking.*

```gherkin
Given an Event in Published status
When Cancel() is called on the aggregate directly (unit test)
Then it transitions correctly and raises the expected domain event

Given a CreateVenueCommandHandler with a mocked repository
When an invalid address is submitted
Then the handler returns Error.Validation without touching the mock's persistence method
```

- [ ] Aggregate invariants (Event status transitions, Money/address validation)
- [ ] Handlers with mocked dependencies (`Eventify.Catalog.UnitTests` already exists)

---

### Epic 3 — Booking Service

**Branch:** `booking` · **Depends on:** E2 · **ARCHITECTURE ref:** §4.3, §5.2, §7 (re-read the saga state diagram
before starting)

**📚 Topics to know:** Distributed locking (why single-node isn't enough, RedLock, lock TTL, fencing); Redis
fundamentals (key expiration, atomic ops, `SETNX`); Saga pattern (orchestration vs choreography, compensating
actions, why 2PC doesn't fit long-running transactions); MassTransit Automatonymous (states, events, transitions,
EF Core saga repository); message delivery guarantees (at-least-once, idempotent consumers, retries/error
queues); race conditions, optimistic vs pessimistic concurrency; SignalR (hubs, groups, connection lifecycle,
Redis backplane); typed gRPC clients (deadlines, error handling); Testcontainers.

#### US-3.1 — Service skeleton

*As a developer, I want the Booking service scaffolded like Catalog so the team has one consistent 4-project
Clean Architecture layout across services.*

```gherkin
Given the Catalog service's project layout
When Booking is scaffolded
Then it has Domain/Application/Infrastructure/Api projects, is added to Eventify.slnx, and reuses
    SharedKernel's BaseDbContext + interceptors without overriding SaveChangesAsync
```

- [ ] 4 Clean Architecture projects following the Catalog layout, added to `Eventify.slnx`
- [ ] Postgres DB (`eventify_booking`), `BaseDbContext`, SharedKernel interceptors wired in

#### US-3.2 — Reservation and Booking aggregates

*As a Customer, I want my seat selection held as a Reservation with a deadline, and confirmed into a Booking once
paid, so the lifecycle in the saga diagram (§7) is enforced by the domain, not by application code.*

```gherkin
Given a Reservation in Active status past its TTL deadline
When any command tries to confirm it
Then the domain rejects the transition (Active → Confirmed is invalid once Expired)

Given a Confirmed Reservation
Then exactly one Booking exists for it, never more than one
```

- [ ] `Reservation`: seat list, TTL deadline, statuses (Active → Confirmed / Expired)
- [ ] `Booking`: statuses per the saga diagram, transition invariants

#### US-3.3 — Distributed seat locking

*As a Customer racing another Customer for the same seat, I want only one of us to win the reservation so
double-booking never happens, even across multiple Booking instances.*

```gherkin
Given two concurrent requests for the same (SessionId, SeatId)
When both try to acquire the reservation lock at nearly the same time
Then exactly one succeeds and the other receives a "seat unavailable" error

Given a Booking instance crashes mid-reservation
Then the Redis lock's TTL expires and the seat becomes reservable again without manual intervention
```

- [ ] Theory first: why a distributed lock, why TTL, what happens when an instance dies
- [ ] Redis RedLock: keys like `seat:{sessionId}:{seatId}`, atomic acquisition of multiple seats

#### US-3.4 — Validate seats and lock price at reservation time

*As a Customer, I want the price I see at reservation time to be the price I pay, even if an Admin changes prices
afterward.*

```gherkin
Given a Session with current PriceTier $80 for "Standard"
When I reserve a Standard seat
Then the Reservation stores $80 as the locked price

Given the Admin changes the price to $100 after my reservation
Then my pending Booking still totals based on $80
```

- [ ] gRPC client to Catalog: validate session + fetch current prices when creating a reservation
- [ ] Persist the locked price on the Reservation/ReservedSeat, not a live reference to Catalog's price

#### US-3.5 — Booking saga

*As the platform, I want the Reserve → Pay → Confirm/Compensate flow orchestrated centrally so the full booking
lifecycle is debuggable from one place, per the ADR-0003 rationale in ARCHITECTURE.md.*

```gherkin
Given a Reservation is created
When the saga starts
Then it schedules a timeout at the Reservation's ExpiresAt and transitions to Pending

Given the saga is in AwaitingPayment and receives PaymentFailed
Then it transitions to Failed and no Booking is confirmed

Given the saga's timeout elapses with no payment
Then it transitions to Expired and releases the held seats
```

- [ ] MassTransit Automatonymous StateMachine — states and transitions strictly per the §7 diagram
- [ ] Saga commands → Payment service (`ProcessPaymentCommand`); compensations on timeout/payment failure
      (release seats)
- [ ] Saga state persistence in Postgres via EF Core saga repository

#### US-3.6 — Live seat map

*As a Customer viewing a session's seat map, I want seat availability to update in real time as other users
reserve or release seats, without refreshing the page.*

```gherkin
Given two browser tabs subscribed to the same session's seat map
When tab A reserves a seat
Then tab B sees that seat turn unavailable within ~1 second, without polling

Given two Booking service instances behind a load balancer
Then a seat-status event raised on instance A still reaches a client connected to instance B
```

- [ ] SignalR hub `/hubs/seats/{sessionId}`: `SeatHeld`, `SeatReleased`, `SeatBooked` server-to-client events
- [ ] Redis backplane so the hub scales across multiple Booking instances

#### US-3.7 — Integration test coverage

*As a maintainer, I want the reservation race condition and TTL expiry covered by real infrastructure tests, not
mocks, since this is the highest-risk service in the system.*

```gherkin
Given a real Postgres + RabbitMQ + Redis via Testcontainers
When two simulated users request the same seat concurrently
Then exactly one Reservation succeeds

Given a Reservation with a short TTL for test purposes
When the TTL elapses without payment
Then the saga transitions to Expired and the seat lock is released
```

- [ ] Testcontainers: Postgres + RabbitMQ + Redis
- [ ] Scenarios: happy-path reservation; two users competing for one seat; TTL expiration

---

### Epic 4 — Payment Service

**Branch:** `payment` · **Depends on:** E3 · **ARCHITECTURE ref:** §4.4, §5.3

**📚 Topics to know:** Stripe payment model (PaymentIntent lifecycle, client secret, test mode/test cards);
webhooks (why polling doesn't work, `Stripe-Signature` verification, replay attacks); idempotency keys and
duplicate webhook handling; secret management (user-secrets, env vars, never in git); the Outbox pattern applied
to webhook-driven publishing.

#### US-4.1 — Service skeleton

*As a developer, I want Payment scaffolded like the other Clean Architecture services with Outbox wired in from
day one, since every event it publishes must be transactional.*

- [ ] 4-project Clean Architecture skeleton, Postgres DB, Transactional Outbox configured

#### US-4.2 — Create a Stripe PaymentIntent

*As a Customer, I want a real Stripe PaymentIntent created for my booking total so I can pay with a test card
through Stripe Elements.*

```gherkin
Given a saga ProcessPaymentCommand with a booking total of $80
When Payment creates a Stripe PaymentIntent in test mode
Then it returns a client_secret to the caller and persists a Payment row in Pending status
```

- [ ] Stripe keys via user-secrets (never in git)
- [ ] Create a PaymentIntent on a saga command, return the `client_secret`

#### US-4.3 — Handle Stripe webhooks idempotently

*As the platform, I want Stripe's webhook events processed exactly once even under Stripe's at-least-once
delivery, so a booking is never double-confirmed or double-refunded.*

```gherkin
Given a payment_intent.succeeded webhook with an unrecognized signature
When it hits POST /webhooks/stripe
Then it is rejected with 400 and no event is processed

Given the same valid webhook event.id delivered twice
When both requests are processed
Then only one PaymentSucceededIntegrationEvent is published
```

- [ ] `Stripe-Signature` verification on `POST /webhooks/stripe`
- [ ] Idempotent processing (dedup table on Stripe `event.id`)
- [ ] `payment_intent.succeeded` / `payment_intent.payment_failed` → integration events via Outbox

#### US-4.4 — Saga command consumers

*As the Booking saga, I want Payment to consume my ProcessPayment command and report back success/failure so I
can advance the saga state.*

```gherkin
Given a ProcessPaymentCommand
When Payment processes it and Stripe confirms success
Then a PaymentSucceededIntegrationEvent is published for the saga to consume

Given Stripe declines the card
Then a PaymentFailedIntegrationEvent is published instead
```

- [ ] Handle `ProcessPayment` command
- [ ] Publish `PaymentSucceeded` / `PaymentFailed`

---

### Epic 5 — Ticket Service

**Branch:** `ticket-notification` · **Depends on:** E3 · **ARCHITECTURE ref:** §4.5, §5.4

**📚 Topics to know:** Vertical Slice Architecture vs Clean Architecture; QR code payload design and HMAC
signing to prevent forgery; ticket validation semantics (one-time use, Issued → Validated, race on double scan);
consumer-driven design (reacting to integration events instead of being called directly).

#### US-5.1 — Issue tickets on booking confirmation

*As a Customer whose booking just confirmed, I want a QR-coded ticket generated per seat so I have something to
present at the venue.*

```gherkin
Given a BookingConfirmedIntegrationEvent for a booking with 2 seats
When the Ticket consumer processes it
Then 2 Ticket rows are created, each with a distinct HMAC-signed QR payload {ticketId, sessionId, seatId}
And a TicketIssuedIntegrationEvent is published per ticket
```

- [ ] VSA single-project skeleton
- [ ] Consumer of `BookingConfirmed` → create a ticket with a QR code (payload: ticketId + signature)
- [ ] gRPC client to Catalog (event/session name for the ticket)

#### US-5.2 — Validate a ticket at the door

*As a Validator, I want to scan a QR code once and have it marked used, and reject any repeat scan, so a ticket
can't be reused.*

```gherkin
Given an Issued ticket
When it is scanned via POST /tickets/validate
Then its status becomes Validated and the response indicates success

Given a Validated ticket
When it is scanned again
Then the response indicates AlreadyValidated, and a ValidationAttempt is logged either way
```

- [ ] QR validation endpoint (API-key auth), one-time use: status Issued → Validated
- [ ] Log every attempt (including duplicates) to `ValidationAttempt`

---

### Epic 6 — Notification Service

**Branch:** `ticket-notification` · **Depends on:** E3 (and E1 if US-1.3 chose Option B) · **ARCHITECTURE ref:**
§4.6, §5.5

**📚 Topics to know:** SMTP basics; MailHog as a dev catch-all vs SendGrid for later; Outbox-driven consumer
design; templated transactional email.

#### US-6.1 — Email on booking lifecycle events

*As a Customer, I want an email when my booking is confirmed, a ticket is issued, or a refund completes, so I
have a paper trail without checking the app.*

```gherkin
Given a BookingConfirmedIntegrationEvent and its paired TicketIssuedIntegrationEvent
When both are consumed
Then a single confirmation email with the QR ticket is sent (visible in MailHog in dev)

Given a RefundCompletedIntegrationEvent
Then a refund-confirmation email is sent
```

- [ ] VSA skeleton
- [ ] Consumers: `BookingConfirmed`, `TicketIssued`, `RefundCompleted`, `UserRegistered`, `ReservationExpired`
- [ ] `IEmailSender` abstraction; MailHog in dev, SendGrid-ready for later
- [ ] Outbox pattern for outbound `EmailSentIntegrationEvent` (debug signal)

#### US-6.2 — Password reset email (conditional)

*As a Customer who deferred password reset in US-1.3 (Option B), I want it implemented here now that the email
channel exists.*

```gherkin
Given US-1.3 recorded "Option B" in the Decision log
When a Customer requests a password reset
Then Notification sends the reset email using the same MailHog/SendGrid channel as booking emails
```

- [ ] Only in scope if US-1.3 chose Option B — check the Decision log before starting
- [ ] Implement the deferred reset-password email flow

---

### Epic 7 — API Gateway

**Branch:** `gateway-spa` · **Depends on:** E1–E6 (fronts every service) · **ARCHITECTURE ref:** §4, §9, §13

**📚 Topics to know:** Reverse proxy concept; YARP routes/clusters/transforms; JWT validation at the gateway vs
per-service (passthrough vs termination trade-offs).

#### US-7.1 — Single entry point for the SPA

*As the SPA, I want one base URL for all REST calls so I don't hardcode six different service ports.*

```gherkin
Given the Gateway is running on port 5000
When the SPA calls /api/catalog/events through the Gateway
Then it is routed to the Catalog service and the response is returned unmodified

Given a request without a valid JWT to a protected route
When it passes through the Gateway
Then it is rejected before reaching the upstream service (or passed through for the service to reject —
    per the passthrough decision recorded for this project)
```

- [ ] YARP project in `src/ApiGateway` (folder exists, empty) — routes to all 6 services
- [ ] JWT passthrough, CORS policy scoped to the SPA origin

---

### Epic 8 — SPA / Frontend

**Branch:** `gateway-spa` · **Depends on:** E7 · **ARCHITECTURE ref:** §10

**Already done (context, not backlog):** routing/provider skeleton, OIDC auth provider, theme provider (dark
default), require-auth guard, login/register pages (alpha), go-home/cancel URLs.

**📚 Topics to know:** React data fetching (effects vs data-fetching libs, loading/error states, request
aborting); pagination UX with `PagedResult<T>`; SignalR JS client (connection lifecycle, reconnect, groups);
Stripe Elements/Payment Element in React; countdown timers (intervals, cleanup, drift); i18next (namespaces,
interpolation, plurals — `en`/`uk` keys always added together).

#### US-8.1 — Browse events

*As a Customer, I want to browse a paginated list of events fetched from Catalog through the Gateway so I can
find something to attend.*

```gherkin
Given the Catalog has 45 published events
When I open the events list page
Then I see page 1 of 20 events with pagination controls, and the page number is reflected in the URL

Given I am on page 2
When I refresh the browser
Then I land back on page 2, not page 1
```

- [ ] Real Catalog data through the Gateway, `PagedResult<T>` pagination with page state in the URL
- [ ] Event cards per the design system (glassmorphism, JetBrains Mono for dates/prices)

#### US-8.2 — Session and live seat map

*As a Customer, I want to pick a session and see a live seat map so I know what's actually available right now.*

```gherkin
Given I open a session's seat map
When another Customer reserves a seat in another tab
Then my seat map updates that seat to unavailable without a page refresh (via SignalR)
```

- [ ] Session selection UI → seat map → SignalR subscription for live seat statuses

#### US-8.3 — Reservation and payment flow

*As a Customer, I want to reserve seats, see a countdown to my reservation deadline, and pay with a test card, so
I can complete a booking end-to-end.*

```gherkin
Given I reserve 2 seats
When the confirmation screen loads
Then I see a countdown timer counting down to the reservation's ExpiresAt

Given my card is declined by Stripe
Then I see a clear error and my seats remain held until the TTL (not immediately released)

Given my reservation's TTL elapses before I pay
Then I see a "reservation expired" message and the seats are released
```

- [ ] Reservation → on-screen TTL countdown timer → Stripe Elements → confirmation
- [ ] Error handling: seat already taken, payment declined, reservation expired

#### US-8.4 — My Tickets

*As a Customer, I want to see my tickets with their QR codes so I can present them at the venue.*

```gherkin
Given I have a confirmed booking with 2 tickets
When I open "My Tickets"
Then I see both tickets with their QR codes, replacing the current placeholder page
```

- [ ] Replace `MyTicketsPage.tsx` placeholder with real ticket list + QR codes

#### US-8.5 — Localization parity

*As a Ukrainian-speaking Customer, I want every new screen fully translated, not just the ones from Phase 1.*

```gherkin
Given a new screen ships with English strings
Then the corresponding Ukrainian keys are added to uk/common.json in the same PR, never as follow-up debt
```

- [ ] All new screens: keys added to `en/common.json` and `uk/common.json` at the same time

#### US-8.6 — Structured validation-error contract

*As a frontend developer, I want validation errors to carry machine-readable metadata (not just a pre-formatted
message) so the SPA can localize them itself instead of displaying server-rendered English text.*

```gherkin
Given a field fails FluentValidation's MaxLength(255) rule
When the API returns the error
Then the response includes a stable code and parameters (e.g. { maxLength: 255 }), not just a formatted string

Given the SPA receives that structured error
Then it renders the message via i18next interpolation ("{{maxLength}} characters max") in the active locale
```

- [ ] Upgrade `ValidationBehavior` to carry `f.FormattedMessagePlaceholderValues` into
      `Error.Validation(..., metadata)`
- [ ] SPA consumes the code + parameters and localizes via i18next interpolation
- [ ] *Deferred from Phase 1*: validation messages there render under each field, so placeholder context was
      redundant for the Razor-only flow — revisit now that the SPA needs to localize independently

---

### Epic 9 — Observability & Telemetry

**Branch:** `observability` · **Depends on:** E1–E8 (nothing to observe before the flow exists) ·
**ARCHITECTURE ref:** §8.2, Iteration 3

**📚 Topics to know:** Structured logging vs plain-text logs; centralized log aggregation; distributed tracing
concepts (spans, trace context propagation across HTTP/gRPC/message boundaries); metrics vs logs vs traces
("three pillars"); health checks as a deployment readiness signal.

#### US-9.1 — Centralized structured logs

*As a developer debugging a production-shaped local run, I want all 6 services' logs in one searchable place
instead of 6 terminal windows.*

- [ ] Serilog → console (baseline, already present per-service)
- [ ] Serilog → Seq (centralized), `docker-compose.observability.yml` entry

#### US-9.2 — Distributed tracing across the saga

*As a developer, I want one trace to show a booking's full journey through HTTP, gRPC, and RabbitMQ hops so I
can debug the saga without stitching logs together manually.*

```gherkin
Given a Customer completes a booking end-to-end
When I open Jaeger and search for that request's trace
Then I see a single trace spanning the SPA request, Booking, Catalog (gRPC), Payment, Ticket, and Notification,
    connected across the RabbitMQ message hops
```

- [ ] OpenTelemetry traces → Jaeger; spans across HTTP + gRPC + RabbitMQ
- [ ] Correlation ID propagation through the saga (MassTransit trace context)

#### US-9.3 — Health checks

*As an operator, I want a dashboard showing whether each service and its dependencies (DB, Redis, RabbitMQ) are
healthy.*

- [ ] `Microsoft.Extensions.Diagnostics.HealthChecks` per service + HC UI dashboard

#### US-9.4 — Metrics dashboard

*As an operator, I want request rate, latency, and error rate per service visible on a dashboard so I can spot
regressions without reading logs.*

- [ ] OpenTelemetry metrics → Prometheus → Grafana with starter dashboards (request rate, p95, error rate)

---

### Epic 10 — DevOps, Testing & Deployment

**Branch:** `deploy-quality` · **Depends on:** everything above (wraps the whole system) · **ARCHITECTURE ref:**
§11, §13, §14

**📚 Topics to know:** NetArchTest for enforcing dependency rules in CI; integration-testing strategy (what to
cover end-to-end vs at unit level, avoiding fragile tests); Docker Compose (services, networks, volumes,
healthchecks); multi-stage Dockerfiles; GitHub Actions (triggers, jobs, caching, Testcontainers in CI);
Kustomize for Kubernetes manifest layering; writing a portfolio README.

#### US-10.1 — Enforce architecture rules in CI

*As a maintainer, I want a build to fail if someone adds a Domain → Infrastructure reference, so layering
violations are caught mechanically, not in code review.*

```gherkin
Given a hypothetical change that makes Domain depend on Infrastructure
When the Eventify.ArchitectureTests suite runs
Then the build fails with a clear NetArchTest assertion message
```

- [ ] Create `Eventify.ArchitectureTests` project (declared in CLAUDE.md, not yet created)
- [ ] NetArchTest rules: Domain has zero deps, Application → Domain only, Infrastructure → Domain+Application,
      Api → all three

#### US-10.2 — Integration tests for critical flows

*As a maintainer, I want the saga's happy path and its compensation path both covered end-to-end against real
infrastructure, since this is the highest-value regression surface.*

- [ ] Saga happy path end-to-end (reserve → pay → confirm → ticket → email)
- [ ] Compensation path on payment failure (release seats, no ticket issued)

#### US-10.3 — One-command local infrastructure

*As a developer, I want `docker compose up` to bring up Postgres, RabbitMQ, Redis, and MailHog so I can start
coding without manually installing anything.*

- [ ] `docker-compose.yml` (base infra) + `docker-compose.app.yml` (all .NET services) +
      `docker-compose.observability.yml`, layered per §13

#### US-10.4 — CI pipeline

*As a maintainer, I want every PR to run build + unit tests + architecture tests automatically, and integration
tests to run on PRs into main, so broken code can't merge.*

- [ ] `build.yml` — restore + build + unit tests + lint + arch tests, every push
- [ ] `integration-tests.yml` — Testcontainers-based, every PR to `main`
- [ ] `docker-publish.yml` — build + push images to GHCR on tag
- [ ] `frontend.yml` — lint + typecheck + build for the SPA
- [ ] Branch protection on `main`: require all checks green

#### US-10.5 — Containerize every service

*As a maintainer, I want each service buildable as a container image so the system can run identically in CI,
locally, and eventually in Kubernetes.*

- [ ] Multi-stage Dockerfile per service under `deploy/docker/`

#### US-10.6 — Kubernetes manifests (stretch)

*As a maintainer, I want to demonstrate the system running on Kubernetes locally, since it's part of the
portfolio's DevOps-maturity story.*

- [ ] Kustomize manifests for local minikube/k3d under `deploy/k8s/`
- [ ] Migrations as `Job` resources (init-container pattern)
- [ ] Optional: Helm chart; optional: GitHub Action to deploy on tag

#### US-10.7 — Portfolio polish

*As the project owner, I want a README that lets a stranger understand and run Eventify in five minutes.*

- [ ] Architecture overview with a diagram, run instructions, SPA + Razor Pages screenshots
- [ ] Links to ADRs and `ARCHITECTURE.md`
- [ ] Optional: Playwright E2E test for browse → reserve → pay → ticket (stretch, pairs with US-10.6)

---

## Decision log

| Date | Item | Decision |
|------|------|----------|
| _(entry)_ | US-1.3 | _(Reset Password: Option A now / Option B → US-6.2)_ |
| _(entry)_ | US-2.3 | _(PriceTier: child entity of Session, or separate aggregate — record the consistency-boundary reasoning)_ |
