# PromptStash

A modern prompt-sharing platform for developers — built as a portfolio-grade reference architecture.

- **Backend:** .NET 8 Web API with a **flat, layered folder structure** (Controllers / Features / Services / Data / Common). Single deployable, MediatR + CQRS retained.
- **Frontend:** **Angular 18** with a flat **4-NgModule** layout (Core / Auth / Dashboard / App). Lazy-loaded feature chunks. Separate `.ts/.html/.scss` per component.
- **Database:** PostgreSQL + EF Core
- **Messaging:** Azure Service Bus (production) or in-memory bus (local dev) — same handlers, swapped at startup.
- **Cross-cutting:** MediatR + CQRS, FluentValidation, JWT auth, Serilog, Swagger, Docker, GitHub Actions.

## Backend layout (5 top-level folders)

The whole API project lives in one `.csproj` with **only five top-level folders**. Files of the same kind sit together; MediatR + CQRS use-cases are grouped by feature inside `Features/`.

```
backend/src/PromptStash.Api/
├── Controllers/        # all controllers, flat
│   ├── AuthController.cs
│   ├── PromptsController.cs
│   └── UsersController.cs
├── Features/           # MediatR commands/queries/validators/handlers, grouped per use-case
│   ├── Auth/
│   │   ├── Login/
│   │   ├── Register/
│   │   └── GetCurrentUser/
│   ├── Prompts/
│   │   ├── CreatePrompt/  UpdatePrompt/  DeletePrompt/
│   │   ├── ToggleLike/    TrackCopy/
│   │   ├── GetPublicFeed/ GetMyPrompts/  GetPromptById/
│   └── Users/
│       ├── ToggleFollow/  GetUserProfile/
├── Services/           # all services + repositories + integration handlers + bus consumers
│   ├── CurrentUserService.cs    DateTimeProvider.cs
│   ├── PasswordHasher.cs        JwtTokenService.cs
│   ├── UserRepository.cs        PromptRepository.cs   FollowRepository.cs
│   ├── EmailService.cs
│   ├── ServiceBusPublisher.cs   InMemoryBusConsumer.cs   AzureServiceBusConsumer.cs
│   ├── EventDispatcher.cs       IntegrationEventHandlers.cs
├── Data/               # EF Core: DbContext + Entities + Configurations + Interceptor + Seed
│   ├── AppDbContext.cs
│   ├── AuditableEntityInterceptor.cs
│   ├── DbInitializer.cs
│   ├── Entities/       # AppUser, Prompt, PromptLike, Follow, ProcessedMessage, BaseEntity, PromptVisibility
│   └── Configurations/ # IEntityTypeConfiguration<> mappings (auto-discovered)
└── Common/             # cross-cutting types: DTOs / Models / Settings / Middleware / Behaviors / Exceptions / Events / Extensions
    ├── DTOs/           # AuthDtos, PromptDto, UserDtos
    ├── Models/         # PaginatedList<T>
    ├── Settings/       # Jwt, ServiceBus, Email, Worker options
    ├── Middleware/     # CorrelationId, ExceptionHandling
    ├── Behaviors/      # MediatR LoggingBehavior + ValidationBehavior
    ├── Exceptions/     # NotFound, Conflict, ForbiddenAccess
    ├── Events/         # IntegrationEvent base + concrete events
    └── Extensions/     # ServiceCollectionExtensions.AddPromptStash() — single composition root
+ Program.cs            # ~50 LOC: Serilog, AddPromptStash(), middleware pipeline, Run
+ appsettings*.json     # Jwt / ServiceBus / Email / Worker / Cors / Serilog
+ Dockerfile            # multi-stage publish for the single API image
+ Properties/launchSettings.json
```

That's it — five logical buckets. No per-feature module folders, no extra projects.

## Repository layout

```
backend/
├── PromptStash.sln
├── Directory.Build.props
├── src/PromptStash.Api/                # the 5-folder API above
└── tests/PromptStash.Tests/            # xUnit + FluentAssertions + NSubstitute

frontend/
├── package.json
├── angular.json
└── src/
    ├── main.ts                         # platformBrowserDynamic().bootstrapModule(AppModule)
    ├── styles.scss
    └── app/                            # 3 top-level folders, 4 NgModules total
        ├── app.module.ts               # AppModule (root) — imports CoreModule.forRoot() + AppRoutingModule
        ├── app-routing.module.ts       # 2 lazy chunks: /auth → AuthModule, /** → DashboardModule
        ├── app.component.{ts,html,scss}
        ├── core/                       # CoreModule = singletons + layout shell + reusable UI + pipes
        │   ├── components/             # auth-layout, empty-state, loading-spinner, prompt-card
        │   ├── layout/                 # main-layout, header, sidebar
        │   ├── services/  guards/  interceptors/  models/  pipes/
        │   └── core.module.ts
        ├── auth/                       # AuthModule (lazy) — login + register
        │   ├── login/    {ts,html,scss}
        │   ├── register/ {ts,html,scss}
        │   └── auth.module.ts          # routes co-located in the module
        └── dashboard/                  # DashboardModule (lazy) — every authenticated screen
            ├── feed/             {ts,html,scss}
            ├── my-prompts/       {ts,html,scss}
            ├── prompt-detail/    {ts,html,scss}
            ├── prompt-edit/      {ts,html,scss}
            ├── profile/          {ts,html,scss}
            └── dashboard.module.ts     # routes co-located in the module

docs/DESIGN.md                          # detailed architecture & implementation guide
.github/workflows/ci.yml                # backend test + frontend build + docker images
docker-compose.yml                      # postgres + api + web (single-deployable demo)
```

## Quick start (local dev)

### Option A — Docker Compose (recommended)

```bash
docker compose up --build
```

- API: <http://localhost:5080> · Swagger UI at `/swagger`
- Web: <http://localhost:4200>
- DB:  Postgres on `localhost:5432` (user/pass `promptstash`/`promptstash`)
- Seeded user: `demo@promptstash.io` / `Demo!2345`

### Option B — Run each piece directly

```bash
# 1) Database
docker run --name promptstash-pg -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=promptstash -p 5432:5432 -d postgres:16-alpine

# 2) Backend
cd backend
dotnet run --project src/PromptStash.Api

# 3) Frontend
cd frontend
npm install
npm start    # http://localhost:4200
```

## Testing

```bash
dotnet test backend/PromptStash.sln       # xUnit + FluentAssertions + NSubstitute
cd frontend && npm test                   # karma + jasmine (no specs included yet)
```

## How a request flows

1. `Controllers/<X>Controller` receives the HTTP request and calls `ISender.Send(...)`.
2. MediatR runs the request through `LoggingBehavior` → `ValidationBehavior` (FluentValidation) → handler.
3. The handler (in `Features/<Area>/<UseCase>/`) calls services/repositories from `Services/` and entities from `Data/Entities/`.
4. On a public state change (e.g. a published prompt) the handler publishes an `IntegrationEvent` via `IServiceBusPublisher`.
5. `InMemoryBusConsumer` (local) or `AzureServiceBusConsumer` (cloud) picks it up and routes it to the matching `IIntegrationEventHandler`(s) via `EventDispatcher` — with idempotency tracked in `ProcessedMessage`.
6. Errors bubble up to `ExceptionHandlingMiddleware` and are returned as ProblemDetails with the correlation id.

## Composition root

Everything is wired in **one** call from `Program.cs`:

```csharp
builder.Services.AddPromptStash(builder.Configuration);
```

`AddPromptStash` (in `Common/Extensions/ServiceCollectionExtensions.cs`) registers the database, MediatR pipeline, JWT auth, services, controllers, Swagger, CORS, rate limiter, and health checks. The integration-event consumer is hosted in-process when `Worker:HostInProcess=true` (default).

## Frontend NgModule layout (4 modules total)

| Module | Folder | Role |
|---|---|---|
| `AppModule` | `src/app/` | Bootstraps the SPA; imports `CoreModule.forRoot()` and `AppRoutingModule`. |
| `CoreModule` | `src/app/core/` | Singletons + layout shell + reusable UI + pipes. `forRoot()` registers HTTP interceptors. Re-exports the Angular Material modules feature modules need. |
| `AuthModule` | `src/app/auth/` | Lazy chunk for `/auth/login` and `/auth/register`. Wraps both pages in the shared `AuthLayoutComponent`. |
| `DashboardModule` | `src/app/dashboard/` | Lazy chunk for everything else (feed, my-prompts, prompt detail/edit, profile). Wraps every authenticated screen in `MainLayoutComponent`. |

- Every component has separate `.ts`, `.html`, and `.scss` files (no inline templates/styles).
- `*-routing.module.ts` files are gone — routes live inside each module file (one file per feature).
- Path aliases: `@core/*`, `@env/*`.

## Deployment (free tier ideas)

- **Render / Railway / Fly.io** — single API container + managed Postgres.
- **Azure App Service (free F1)** — API container, Azure Postgres (B1ms), Azure Service Bus (Basic).
- **Vercel / Netlify / Cloudflare Pages** — web build (`frontend/dist/promptstash-web`).
- **Neon / Supabase / Aiven Free** — managed Postgres.

See `docs/DESIGN.md` for a deeper walkthrough.
#   S k i l l S t a s h  
 