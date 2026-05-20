# PromptStash — Architecture & Implementation Guide

## 1. Product overview

**PromptStash** is a developer-focused prompt-sharing platform. Users:

- Save and version their LLM prompts.
- Publish prompts publicly, unlisted, or keep them private.
- Follow other authors and get email notifications when they ship a new public prompt.
- Copy / like / browse prompts via a clean dashboard UI.

The codebase is intentionally a **portfolio reference architecture** — every piece is chosen to demonstrate a real-world enterprise pattern (MediatR mediator pipeline, integration events, idempotent consumers, JWT, layered project layout) without over-engineering.

## 2. High-level architecture

```
                ┌─────────────────────────┐
                │  Angular SPA (nginx)    │
                └────────────┬────────────┘
                             │ JSON / HTTPS
                             ▼
                ┌─────────────────────────┐
                │  PromptStash.Api (.NET) │   single deployable
                │  ┌──────────────────┐   │
                │  │ Controllers      │   │
                │  │ Features (MediatR)│   │
                │  │ Services         │   │
                │  │ Data (EF Core)   │   │
                │  │ Common (cross-   │   │
                │  │   cutting)       │   │
                │  │ BackgroundSvc:   │   │
                │  │   ServiceBus     │   │
                │  │   consumer       │   │
                │  └──────────────────┘   │
                └────┬────────────┬───────┘
                     │            │
                     ▼            ▼
              ┌──────────┐  ┌───────────────┐
              │ Postgres │  │ Azure Service │
              │   (EF)   │  │ Bus (or in-   │
              └──────────┘  │ memory bus    │
                            │ in dev)       │
                            └───────────────┘
```

The integration-event handler chain (`PromptPublishedHandler`, `UserRegisteredHandler`) is the **same code path** for both publishers — in-memory bus locally, Azure Service Bus in production. Hosting is decided once in `Common/Extensions/ServiceCollectionExtensions.AddAppServices(...)`.

## 3. Backend — flat 5-folder layout

### 3.1 Why a flat layout (not modules per feature)?

For a small-to-mid sized service, having **everything in five well-named buckets** wins on three axes:

- **Discoverability.** New contributors learn the project in minutes — controllers live in `Controllers/`, services in `Services/`, etc. There's nothing to "find" inside a feature module.
- **Refactorability.** Renames or interface extractions don't have to ripple across module boundaries.
- **Speed.** Adding a use-case is one folder under `Features/<Area>/<UseCase>/` plus a controller method — no module bootstrap files.

We use **MediatR only** (not CQRS): every API operation is a single `*Request` message with a matching `*Handler` and optional `*RequestValidator`. Reads and writes share the same pattern — no separate command/query split. The only thing we dropped is the per-feature module folder tree.

### 3.2 Folder responsibilities

```
backend/src/PromptStash.Api/
├── Controllers/   # ASP.NET Core controllers — thin, only call ISender.Send(...)
├── Features/      # MediatR requests + handlers, grouped by area
├── Services/      # All app services + repositories + integration handlers + bus consumers
├── Data/          # EF Core: DbContext, entities, IEntityTypeConfiguration<>, interceptor, seed
└── Common/        # cross-cutting: DTOs, Models, Settings, Middleware, Behaviors, Exceptions, Events, Extensions
```

### 3.3 Controllers/

- One file per controller (`AuthController`, `PromptsController`, `UsersController`).
- Use primary constructors to inject `ISender`.
- Methods do nothing but `Ok(await sender.Send(request, ct))`. No business logic.
- `[Authorize]` is applied per-method where required, plus `[EnableRateLimiting("auth")]` on the auth endpoints.

### 3.4 Features/

One folder per API area; each area has a single handlers file (all requests together — no per-use-case subfolders):

```
Features/
├── Auth/AuthHandlers.cs
├── Skills/SkillHandlers.cs
├── Library/LibraryHandlers.cs
├── Trending/TrendingHandlers.cs
└── Users/UserHandlers.cs
```

Namespaces: `PromptStash.Api.Features.{Auth|Skills|Library|Trending|Users}`. Controllers resolve MediatR types via `GlobalUsings.cs`.

Each handler file follows the same pattern: `*Request` **record**, optional `*RequestValidator`, and `*Handler` implementing `IRequestHandler<,>`.

### 3.5 Services/

Grouped by role; interfaces stay next to implementations:

```
Services/
├── Repositories/     # IUserRepository, ISkillRepository, IFollowRepository, ISkillBookmarkRepository
├── Auth/             # ICurrentUserService, IJwtTokenService, IPasswordHasher
├── Messaging/        # IServiceBusPublisher, EventDispatcher, consumers, integration handlers
├── Trending/         # GitHub fetcher, sync service, trending repository
├── EmailService.cs   # IEmailService (root — cross-cutting)
└── DateTimeProvider.cs
```

### 3.6 Data/

- `AppDbContext.cs` — applies all `IEntityTypeConfiguration<>` from the assembly via `ApplyConfigurationsFromAssembly`.
- `Entities/` — `Core.cs` (base types + `SkillVisibility`), `Users.cs`, `Skills.cs`, `Library.cs`, `Trending.cs`.
- `Configurations/` — `UserConfigurations`, `SkillConfigurations`, `LibraryConfigurations`, `TrendingConfigurations`. EF auto-discovery means **no manual registration** in `OnModelCreating`.
- `AuditableEntityInterceptor.cs` — sets `CreatedAt/UpdatedAt` and the audit user; registered as **scoped** because it depends on the scoped `ICurrentUserService`.
- `DbInitializer.cs` — applies migrations if any exist, otherwise calls `EnsureCreatedAsync`, then seeds a demo user and two prompts.

### 3.7 Common/

Holds everything that crosses concerns and isn't a service:

- `Dtos/` — `AuthDtos`, `SkillDtos`, `BookmarkDtos`, `TrendingDtos`, `UserDtos` (API contracts for the SPA).
- `Models/` — generic helpers like `PaginatedList<T>` (with `CreateAsync(IQueryable<T>, page, pageSize)`).
- `Settings/` — strongly-typed options (`JwtOptions`, `ServiceBusOptions`, `EmailOptions`, `WorkerOptions`).
- `Middleware/` — `CorrelationIdMiddleware` (per-request correlation id pushed into Serilog `LogContext`) and `ExceptionHandlingMiddleware` (maps to ProblemDetails JSON).
- `Behaviors/` — MediatR pipeline behaviors: `LoggingBehavior` and `ValidationBehavior`.
- `Exceptions/` — `NotFoundException`, `ConflictException`, `ForbiddenAccessException`.
- `Events/` — `IntegrationEvent` base + concrete `UserRegisteredIntegrationEvent`, `PromptPublishedIntegrationEvent`.
- `Extensions/ServiceCollectionExtensions.cs` — **the** composition root: `AddPromptStash(IConfiguration)` and `MapPromptStashHealth(WebApplication)`.

### 3.8 Composition root

`Program.cs` stays around 50 lines:

```csharp
builder.Host.UseSerilog(...);
builder.Services.AddPromptStash(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapPromptStashHealth();
await DbInitializer.InitializeAsync(app.Services, app.Logger);
app.Run();
```

`AddPromptStash` chains five private methods: `AddDatabase`, `AddMediatRPipeline`, `AddJwtAuth`, `AddAppServices`, `AddApiInfrastructure`. Each is short, scoped to one concern, and easy to swap in tests.

## 4. Request flow (CreateSkill example)

1. `POST /api/skills` hits `SkillsController.Create(CreateSkillRequest)`.
2. The controller calls `sender.Send(request, ct)`.
3. `LoggingBehavior` logs entry + duration; `ValidationBehavior` runs `CreateSkillRequestValidator` and throws `ValidationException` on failure.
4. `CreateSkillHandler` reads `currentUser.UserId`, loads the author from `IUserRepository`, creates a `Skill`, persists via `ISkillRepository`, and — if the visibility is `Public` — publishes a `SkillPublishedIntegrationEvent` via `IServiceBusPublisher`.
5. The hosted `InMemoryBusConsumer` (or `AzureServiceBusConsumer`) dequeues the envelope, hands it to `EventDispatcher`, which:
   - skips duplicates by `MessageId` via `ProcessedMessage`,
   - routes to every `IIntegrationEventHandler` whose `EventName` matches,
   - records the message id on success.
6. `PromptPublishedHandler` emails the author and fans out to followers (`EmailNotificationsEnabled = true`).
7. Any exception bubbles to `ExceptionHandlingMiddleware`, which serializes a ProblemDetails payload with the `traceId` matching `X-Correlation-Id`.

## 5. Auth

- BCrypt password hashing.
- JWT access tokens issued by `JwtTokenService` with `sub`, `email`, `preferred_username`, `display_name`, and a `jti`.
- `IHttpContextAccessor` + `CurrentUserService` to read `sub` from any layer.
- `[Authorize]` attribute per controller method.
- Rate-limited register/login via the named `"auth"` policy (10 req/min/IP).

## 6. Observability

- Serilog console sink with structured output and a `CorrelationId` enricher.
- Per-request correlation id is set by `CorrelationIdMiddleware` and pushed into `LogContext` so every downstream log line has it.
- Health: `/health` and `/health/live`, with an `Npgsql` check.

## 7. Frontend — flat 4-NgModule layout

The frontend mirrors the backend's "fewer folders, clearer roles" philosophy:

```
src/app/
├── app.module.ts              # AppModule (root) — required by Angular for bootstrap
├── app-routing.module.ts      # 2 lazy chunks
├── app.component.{ts,html,scss}
├── core/                      # CoreModule — singletons + layout + shared UI
│   ├── components/            # auth-layout, empty-state, loading-spinner, prompt-card
│   ├── layout/                # main-layout, header, sidebar
│   ├── interceptors/          # auth.interceptor, error.interceptor
│   ├── guards/                # auth.guard
│   ├── models/                # index + auth/skill/trending models, app.constants
│   ├── services/              # auth, skill, library, trending, user, token, notification
│   ├── pipes/                 # time-ago.pipe
│   └── core.module.ts
├── auth/                      # AuthModule (lazy)
│   ├── login/    {ts,html,scss}
│   ├── register/ {ts,html,scss}
│   └── auth.module.ts         # routes inline
└── dashboard/                 # DashboardModule (lazy)
    ├── feed/  my-skills/  skill-detail/  skill-edit/  library/  profile/  trending/
    └── dashboard.module.ts    # routes inline, wrapped in MainLayoutComponent
```

### 7.1 Why only 4 NgModules?

- **`AppModule`** stays minimal — bootstrap and the two top-level lazy routes.
- **`CoreModule`** absorbs the old `SharedModule`. It declares + exports every Angular Material module and shared UI primitive (cards, spinner, empty state, time-ago pipe). It also declares the layout shell (`MainLayoutComponent`, `HeaderComponent`, `SidebarComponent`) and the `AuthLayoutComponent` poster used by `/auth`. A `forRoot()` static method registers the HTTP interceptors so they're added exactly once.
- **`AuthModule`** owns sign-in / sign-up. Routing lives inside the module file (no separate `auth-routing.module.ts`).
- **`DashboardModule`** owns every authenticated screen (feed, library, my-skills, skill edit/detail, profile, trending) and wires them under the `MainLayoutComponent` shell. Routing is inline.

All app services use `providedIn: 'root'`, so re-importing `CoreModule` from a feature module is harmless — only `forRoot()` adds providers.

### 7.2 Component conventions

- Every component is a **triplet**: `*.component.ts` + `*.component.html` + `*.component.scss`. No inline templates or styles.
- Components use `ChangeDetectionStrategy.OnPush` and call `cdr.markForCheck()` after async work.
- Selectors are prefixed with `ps-` (e.g. `ps-feed`, `ps-prompt-card`).
- Shared UI lives under `core/components/`; layout shells live under `core/layout/`. Both are declared by `CoreModule` so any feature module can drop them straight into a template.

### 7.3 Routing

`app-routing.module.ts` only knows two destinations:

```ts
{ path: 'auth', loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
{ path: '',     loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule) }
```

`DashboardModule` mounts everything inside `MainLayoutComponent` (sidebar + header) and applies `AuthGuard` on routes that need it. `AuthModule` wraps its pages in `AuthLayoutComponent` (the split-screen poster + form).

### 7.4 Path aliases

Only two are needed now:

```json
"paths": {
  "@core/*": ["src/app/core/*"],
  "@env/*":  ["src/environments/*"]
}
```

Feature modules import their own pages with relative paths (`./login/login.component`) — the only cross-cutting symbols come from `@core/...`.

## 8. Deployment

- `backend/src/PromptStash.Api/Dockerfile` produces the single API image.
- `frontend/Dockerfile` builds the Angular app and serves it with nginx.
- `docker-compose.yml` brings up Postgres + API + Web (the consumer is in-process inside the API).
- `.github/workflows/ci.yml` runs `dotnet build`, `ng build`, then builds + pushes both Docker images on `main`.

## 9. Adding a new use-case (recipe)

1. Add the command/query record, validator, and handler to the matching file under `Features/{Area}/` (e.g. `Features/Skills/SkillHandlers.cs`).
2. If new persistence fields are needed, update the relevant file under `Data/Entities/` and `Data/Configurations/`, then apply schema patches or a migration.
3. Add a controller method to the matching controller in `Controllers/`. Forward to MediatR.
4. If a new shared service is needed, add it under the appropriate `Services/{Repositories|Auth|Messaging|Trending}/` folder and register it in `ServiceCollectionExtensions.AddAppServices`.
5. If you cross a module boundary, define an `IntegrationEvent` in `Common/Events/` and a matching `IIntegrationEventHandler` in `Services/Messaging/`.
That's the loop — grouped folders, MediatR mediator pattern, no per-use-case file explosion.
