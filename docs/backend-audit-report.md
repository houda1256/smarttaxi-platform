# SmartTaxi Backend — Audit Report

**Date:** 2026-07-29 (updated — supersedes the initial audit produced earlier this session)
**Branch:** `feature/identity-authentication`
**Format:** Master Prompt Part 4, 25-point structure
**Method:** Real commands executed in this session (output quoted below), plus direct source inspection. Nothing in this report is inferred from documentation alone.

## Commands executed and raw results

```
dotnet --info            → SDK 10.0.302, host 10.0.10, win-x64. (SDKs 8.0.319/10.0.301/10.0.302 also installed.)
dotnet restore SmartTaxiBackend.slnx → "All projects are up-to-date for restore."
dotnet build SmartTaxiBackend.slnx   → Build succeeded. 0 Warning(s). 0 Error(s).
dotnet test SmartTaxiBackend.slnx --no-build →
  SmartTaxi.Application.Tests: Passed! Failed: 0, Passed: 8,  Skipped: 0, Total: 8
  SmartTaxi.Domain.Tests:      Passed! Failed: 0, Passed: 18, Skipped: 0, Total: 18
dotnet list <each csproj> package → see §2 table (exact resolved versions)
dotnet ef migrations list --project src/SmartTaxi.Infrastructure --startup-project src/SmartTaxi.API →
  20260721101836_InitialCreate
  (no PostgreSQL server reachable at localhost:5432 in this environment — expected, no local DB running;
   EF Core still successfully resolved the DbContext and read migration metadata from source)
```

---

## 1. Repository structure

```
backend/
  SmartTaxiBackend.slnx
  src/
    SmartTaxi.API/            (Endpoints/, ErrorHandling/, SecurityDemo/, Contracts/, Properties/)
    SmartTaxi.Application/    (Common/, Identity/)
    SmartTaxi.Domain/         (Common/, Identity/)
    SmartTaxi.Infrastructure/ (Identity/, Persistence/)
  tests/
    SmartTaxi.Domain.Tests/
    SmartTaxi.Application.Tests/
docs/
  backend-architecture.md, backend-modules.md, devsecops-strategy.md,
  backend-audit-report.md (this file), business-functional-specification.md,
  codeql-diagnosis-report.md
.github/workflows/codeql.yml   (out of scope — not touched)
```
No `frontend/`, `figma/`, or `devsecops/` directories exist in this repository yet.

## 2. Existing projects

| Project | TFM | Project references | Package references (resolved) |
|---|---|---|---|
| `SmartTaxi.Domain` | net10.0 | none | none |
| `SmartTaxi.Application` | net10.0 | → Domain | none |
| `SmartTaxi.Infrastructure` | net10.0 | → Domain, → Application | `Microsoft.EntityFrameworkCore.Design` 10.0.4, `Microsoft.Extensions.Identity.Core` 10.0.4, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, `System.IdentityModel.Tokens.Jwt` 8.21.0 |
| `SmartTaxi.API` | net10.0 (Web) | → Application, → Infrastructure | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0, `Microsoft.AspNetCore.OpenApi` 10.0.10, `Microsoft.Data.SqlClient` 7.0.2*, `Microsoft.EntityFrameworkCore.Design` 10.0.4, `Microsoft.OpenApi` 2.11.0, `Scalar.AspNetCore` 2.16.16 |
| `SmartTaxi.Domain.Tests` | net10.0 | → Domain | `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4 |
| `SmartTaxi.Application.Tests` | net10.0 | → Application | same test packages as above |

\* `Microsoft.Data.SqlClient` is used only by the isolated `SecurityDemo/VulnerableSqlController.cs` — the real datastore is PostgreSQL/Npgsql; this is intentional demo-only debt, not a persistence decision.

All 6 projects are referenced from `SmartTaxiBackend.slnx` under `/src/` and `/tests/` solution folders.

## 3. Existing architecture

Clean Architecture, verified (not assumed) by inspecting actual `ProjectReference`s and `using` statements:
- Domain → no outgoing references at all.
- Application → references Domain only.
- Infrastructure → references Domain + Application; implements Application's interfaces.
- API → references Application + Infrastructure; controllers/endpoints are thin.

Folder organization is module-first within each layer (an `Identity/` folder recurs in Domain, Application, Infrastructure, API), which is the correct shape for the modular monolith and scales cleanly to future modules.

## 4. Existing business modules

**Only `Identity` has any code.** Verified via full recursive listing of Domain/Application/Infrastructure/API — no trace of Fleet, Ride, Payment, Subscription, Notification, Maintenance, Roadside Assistance, Loyalty, Advertising, Support, or Administration in source.

## 5. Existing entities and value objects

| Type | File | Notes |
|---|---|---|
| `User` (sealed `AggregateRoot`) | `Domain/Identity/Entities/User.cs` | Private ctor + `Create(email, passwordHash, role)` factory; properties `Email`, `PasswordHash`, `Role`, all private-set; no invariant-enforcing behavior methods beyond construction |
| `Email` (sealed partial `ValueObject`) | `Domain/Identity/ValueObjects/Email.cs` | `Create()` trims + regex-validates (`[GeneratedRegex]`), throws `ArgumentException` on invalid input; equality is case-insensitive |
| `HashedPassword` (sealed `ValueObject`) | `Domain/Identity/ValueObjects/HashedPassword.cs` | `Create()` rejects empty/whitespace; equality on raw value |
| `UserRole` (enum) | `Domain/Identity/Enums/UserRole.cs` | `Customer`, `Driver`, `Admin` only — none of the other actor types from Part 3 exist yet |
| `Entity` / `AggregateRoot` / `ValueObject` (base types) | `Domain/Common/*.cs` | `Entity`: Id-based equality. `AggregateRoot`: extends `Entity`, currently adds nothing (no domain-event machinery). `ValueObject`: structural equality via `GetEqualityComponents()` |

No domain events exist anywhere in the codebase.

## 6. Existing commands, queries and handlers

Hand-rolled CQRS messaging (`Application/Common/Messaging/ICommand.cs`, `ICommandHandler.cs`) — **no MediatR** reference anywhere in the solution (confirmed via §2's package list).

| Command | Handler | Result |
|---|---|---|
| `RegisterUserCommand(Email, Password)` | `RegisterUserCommandHandler` | `Result<RegisterUserResult>` — validates email format, checks duplicate, enforces min password length 8, hashes password, persists |
| `LoginUserCommand(Email, Password)` | `LoginUserCommandHandler` | `Result<LoginUserResult>` — validates email format, looks up user, verifies password, issues token; returns the *same* generic "Email ou mot de passe incorrect." message for unknown-email and wrong-password (good — no user enumeration, now covered by a test) |

No Queries exist yet (no read-side operations implemented). `Result`/`Result<T>` (`Application/Common/Result.cs`) with `ErrorType` (`Validation`, `Conflict`, `Unauthorized`) is the outcome type used throughout.

## 7. Existing controllers and endpoints

| Endpoint | Method | Auth |
|---|---|---|
| `/api/auth/register` | POST (Minimal API, `Endpoints/Identity/AuthEndpoints.cs`) | Anonymous |
| `/api/auth/login` | POST (Minimal API, same file) | Anonymous |
| `/api/security-demo/vulnerable-sql` | GET (MVC controller, `SecurityDemo/VulnerableSqlController.cs`) | Anonymous — **intentionally vulnerable, isolated CodeQL demo, not a business endpoint** |
| `/weatherforecast` | GET (Minimal API, `Program.cs`) | Anonymous — leftover template scaffold |

Both real endpoints are thin (build a command, call the handler, map `Result` to `TypedResults`); no business logic inline. **No endpoint anywhere uses `[Authorize]`, a policy, or a role requirement** — `AddAuthorization()` is called with zero policies configured.

## 8. Existing repositories and persistence services

| Component | File | Notes |
|---|---|---|
| `IUserRepository` → `UserRepository` | `Infrastructure/Identity/Repositories/UserRepository.cs` | EF Core, proper async/await + `CancellationToken`; calls `SaveChangesAsync` itself (no separate Unit of Work) |
| `IPasswordHasher` → `PasswordHasher` | `Infrastructure/Identity/Services/PasswordHasher.cs` | Wraps ASP.NET Core Identity's `PasswordHasher<User>` (PBKDF2) — correct, not home-rolled; registered Singleton (safe, stateless) |
| `ITokenGenerator` → `JwtTokenGenerator` | `Infrastructure/Identity/Services/JwtTokenGenerator.cs` | HMAC-SHA256, claims `sub`/`email`/`role`/`jti`, configurable expiry; registered Singleton (safe, stateless) |

No generic repository, no Unit of Work abstraction, no other service adapters (no email/SMS/payment/file-storage/etc.) exist yet — expected at this stage.

## 9. Existing authentication and authorization

- JWT bearer auth fully wired in `Program.cs`: issuer/audience/signing-key validation, `NameClaimType="sub"`, `RoleClaimType="role"`.
- **JWT settings are read twice, two different ways**: `Program.cs` reads `Jwt:Issuer/Audience/Key` via the raw `IConfiguration` indexer; `Infrastructure/DependencyInjection.cs` binds the same `Jwt` section to `IOptions<JwtOptions>` for `JwtTokenGenerator`. Two independent sources of truth for identical settings.
- `AddAuthorization()` has zero policies; no endpoint is protected. This is correct for the two existing anonymous endpoints, but the `[Authorize]`/role pipeline has never actually been exercised end-to-end.
- No refresh tokens, no token revocation, no 2FA, no session management, no permission-based authorization yet (all specified in Part 3, Module 1 — known gap, not a bug).

## 10. Existing database configuration

- Provider: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- `ApplicationDbContext` (`Infrastructure/Persistence/ApplicationDbContext.cs`): one `DbSet<User> Users`; applies configurations via `ApplyConfigurationsFromAssembly`.
- One entity configuration: `UserConfiguration` — unique index on `Email`, value converters for `Email`/`HashedPassword`, `Role` stored as string.
- `DesignTimeDbContextFactory` reads connection string from env var `SMARTTAXI_DESIGN_TIME_CONNECTION`, falling back to a hardcoded local dev string (`Host=localhost;Database=smarttaxi;Username=postgres;Password=postgres`) — this fallback is a design-time-only convenience, not used at runtime (runtime uses `appsettings`/user-secrets `ConnectionStrings:DefaultConnection`).

## 11. Existing migrations

Exactly **one** migration, confirmed live via `dotnet ef migrations list`:

```
20260721101836_InitialCreate
```

Creates the `Users` table (`Id` uuid PK, `Email` varchar(320) unique, `PasswordHash` text, `Role` varchar(50)). `ApplicationDbContextModelSnapshot.cs` is in sync with it. No PostgreSQL server was reachable in this environment to check applied-vs-pending status — expected (no local DB running), and not required to confirm the migration itself is valid, since EF Core successfully loaded the DbContext and migration metadata from source.

## 12. Existing tests

**Added this session** (did not exist before):
- `SmartTaxi.Domain.Tests` — 18 tests: `Email` (format validation, trimming, case-insensitive equality), `HashedPassword` (validation, equality), `User.Create` (field assignment, unique Id generation).
- `SmartTaxi.Application.Tests` — 8 tests: `RegisterUserCommandHandler` (success, invalid email, duplicate, short password), `LoginUserCommandHandler` (success, unknown email, wrong password, invalid email format), using hand-written fakes (no mocking library needed for 3 small interfaces).

**Total: 26/26 passing**, confirmed by the `dotnet test` run at the top of this report. No integration tests exist yet (would need Testcontainers/PostgreSQL per Part 4's testing rules — not yet built).

## 13. Existing documentation

| File | Status |
|---|---|
| `docs/backend-architecture.md` | Up to date — describes the Clean Architecture layering accurately |
| `docs/backend-modules.md` | **Stale** — uses Part 1 module naming (Customers/Drivers/Rides/Vehicles/Payments/Rewards), superseded by Part 3's naming (Fleet/Ride/Payment.../Loyalty/etc.) recorded in `docs/business-functional-specification.md` |
| `docs/business-functional-specification.md` | Current — full Part 3 spec, authoritative |
| `docs/devsecops-strategy.md` | Out of scope for this work, not reviewed in detail |
| `docs/codeql-diagnosis-report.md` | Current — documents the intentionally vulnerable demo and its CI diagnosis |
| `docs/backend-audit-report.md` | This file |

No `README.md`, `docs/database.md`, `docs/api.md`, `docs/security.md`, `docs/development.md`, `docs/testing.md`, `docs/business-rules.md`, `docs/permissions.md`, `docs/integrations.md`, `docs/migrations.md`, `docs/demo-data.md`, or `docs/remaining-work.md` exist yet — all listed as required deliverables in Part 4, to be created as the relevant modules are built (not speculatively now).

## 14. Existing reusable components

These should be **reused, not replaced**, when building new modules:
- `Entity`, `AggregateRoot`, `ValueObject` base classes (`Domain/Common`).
- `ICommand<T>`/`ICommandHandler<TCommand,TResult>` messaging shape (`Application/Common/Messaging`) — unless/until a decision is made to adopt MediatR (Part 2 target), new commands should follow this same shape for consistency.
- `Result`/`Result<T>` + `ErrorType` outcome pattern (`Application/Common`).
- `GlobalExceptionHandler` (`API/ErrorHandling`) — single catch-all, RFC7807 `ProblemDetails`, never leaks internals; extend, don't replace.
- `PasswordHasher` (ASP.NET Core Identity-backed) and `JwtTokenGenerator` — solid, reusable as-is for any future auth work.
- Test project structure and hand-written-fake pattern just established in `tests/`.

## 15. Missing components

Full comparison against the Part 2/3 target architecture is already documented in the prior audit; unchanged since then. Headline gaps: MediatR, FluentValidation, AutoMapper, Unit of Work, generic repository, domain events (no raising machinery despite `AggregateRoot` existing), API versioning, standard response envelope, Serilog, SignalR, background processing abstraction, caching abstraction, file storage abstraction, refresh tokens/2FA/permissions, RowVersion concurrency, soft delete, idempotency infrastructure, all 10 remaining business modules, and all integration tests.

## 16. Architecture violations

**None found**, verified by direct inspection of every project's references and `using` statements (see §3). The only non-layering oddity is the `Microsoft.Data.SqlClient` package in `SmartTaxi.API` (§2), which exists solely for the isolated SecurityDemo and is not a persistence-layer decision.

## 17. Duplications

1. **JWT configuration read twice** — `Program.cs` (raw `IConfiguration` indexer) vs. `Infrastructure/DependencyInjection.cs` (`IOptions<JwtOptions>`) — same settings, two code paths (§9).
2. **Exception-driven validation duplicated across both handlers** — `RegisterUserCommandHandler` and `LoginUserCommandHandler` both wrap `Email.Create(...)` in `try/catch (ArgumentException)` purely to convert a domain validation exception into a `Result` failure — same pattern copy-pasted twice, using exceptions for expected/routine validation.

## 18. Compilation errors

**None.** `dotnet build SmartTaxiBackend.slnx` → Build succeeded, 0 Warning(s), 0 Error(s) (verified this session, output quoted at top).

## 19. Test failures

**None.** `dotnet test SmartTaxiBackend.slnx --no-build` → 26/26 passed (18 Domain + 8 Application), verified this session, output quoted at top.

## 20. Security risks

- **No endpoint enforces authorization anywhere** — harmless today (only anonymous endpoints exist) but the `[Authorize]`/role pipeline has never been proven to actually work; must be exercised before the first protected endpoint ships.
- `SecurityDemo/VulnerableSqlController.cs` — intentional, isolated, anonymous SQL-injection demo. Confirmed still isolated: not referenced by any business code, uses a separate DB driver (`Microsoft.Data.SqlClient` against a PostgreSQL app, so it cannot succeed against the real DB even if hit). Per Part 4's explicit rules: kept, not deleted, clearly documented, excluded from business flows.
- No rate limiting, no refresh-token rotation, no 2FA, no audit log yet — all expected gaps at this stage (Part 3/Part 2 targets), not defects in what exists.

## 21. Technical debt

Same items flagged in the prior audit, still open: no Unit of Work (repository calls `SaveChangesAsync` itself — fine for single-aggregate writes, insufficient once Payment/Ride-completion need multi-aggregate transactions), no domain events despite `AggregateRoot` scaffolding, JWT config duplication, exception-driven validation duplication, stale `docs/backend-modules.md` naming, leftover `/weatherforecast` scaffold, `Microsoft.Data.SqlClient` package tied to the demo.

## 22. Recommended implementation order

Per Part 3/Part 4: **Shared foundations → Identity → Fleet → Ride → Payment and Finance → Subscription → Notification → Maintenance → Roadside Assistance → Loyalty → Advertising → Support and Incidents → Administration and Analytics.**

Shared-foundations work has begun (test projects, this session). Recommended next concrete slice: complete Identity's Part 3 scope (roles/permissions, refresh tokens, documents, referral codes) before moving to Fleet, since Fleet/Ride/etc. all depend on Identity's multi-role/permission model.

## 23. Files that should be preserved

All existing Identity module files (Domain/Application/Infrastructure/API), `GlobalExceptionHandler`, `ApplicationDbContext` + its one migration, both new test projects, all `docs/*.md` files, `.github/workflows/codeql.yml` (out of scope, not to be touched), `SecurityDemo/VulnerableSqlController.cs` (must not be silently deleted per Part 4's explicit rule).

## 24. Files that may require correction (not yet actioned — awaiting direction)

- `Program.cs` — consolidate the duplicated JWT config reading onto `IOptions<JwtOptions>` only; remove the leftover `/weatherforecast` scaffold.
- `RegisterUserCommandHandler.cs` / `LoginUserCommandHandler.cs` — replace the exception-driven `Email.Create` validation pattern with a non-throwing alternative (`TryCreate` or a validator), once a validation strategy (manual vs. FluentValidation) is decided.
- `docs/backend-modules.md` — rewrite to match the Part 3 canonical module list.

## 25. Risks before implementation

| Risk | Severity |
|---|---|
| Untested authorization pipeline — first protected endpoint could ship with a silent misconfiguration | High |
| No Unit of Work before Fleet/Ride/Payment introduce multi-aggregate transactional writes | Medium |
| `docs/backend-modules.md` naming drift could confuse future contributors if not corrected before more modules are scaffolded | Medium |
| SecurityDemo/`Microsoft.Data.SqlClient` forgotten before a real deployment | Low (fully isolated, cannot reach the real Postgres DB) |
| No integration tests / no Testcontainers setup yet — unit tests alone won't catch persistence or auth wiring regressions | Medium |

---

**No code was modified to produce this report.** Per the phase-gate protocol, next step is choosing the concrete next implementation slice (see §22) — I will not proceed to implementation without your direction.
