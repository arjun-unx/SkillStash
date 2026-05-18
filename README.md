# SkillStash

Discover, save, and publish **agent skills** (`SKILL.md`-style instructions) for Claude, ChatGPT, Gemini, Cursor, and more.

## Stack

| Layer | Tech |
| --- | --- |
| API | .NET 8, MediatR, EF Core, PostgreSQL |
| Web | Angular 18, Tailwind CSS |
| Trending | GitHub API — aggregates public `SKILL.md` repos |
| Messaging | In-memory bus (local) or Azure Service Bus (production) |

## Features

- **Discover** — browse public skills from the community
- **My skills** — create and manage your own `SKILL.md` content
- **Library** — bookmarks and collections
- **Trending** — curated skills synced from official/community GitHub repos
- **Auth** — JWT login and registration

## Quick start

### Docker Compose

```bash
docker compose up --build
```

| Service | URL |
| --- | --- |
| Web | http://localhost:4200 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |

### Local development

**Database** (PostgreSQL):

```bash
docker run --name skillstash-pg -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=promptstash -p 5432:5432 -d postgres:16-alpine
```

**API:**

```bash
cd backend
dotnet run --project src/PromptStash.Api
```

**Web:**

```bash
cd frontend
npm install
npm start
```

## GitHub token (trending sync)

Trending needs a GitHub personal access token (read access to public repos). Without it, sync hits the unauthenticated rate limit (~60 requests/hour).

1. Create a token: https://github.com/settings/tokens
2. Copy `backend/src/PromptStash.Api/appsettings.Secrets.json.example` → `appsettings.Secrets.json`
3. Set `Trending:GitHubToken` to your `ghp_...` value
4. Restart the API and use **Trending → Refresh catalog** in the UI

Or use user secrets:

```bash
dotnet user-secrets set "Trending:GitHubToken" "ghp_YOUR_TOKEN" --project backend/src/PromptStash.Api
```

## Project layout

```
backend/
  src/PromptStash.Api/     # API (Controllers, Features, Services, Data)
  tests/PromptStash.Tests/
frontend/
  src/app/                 # Angular (core, auth, dashboard)
docs/DESIGN.md             # Architecture notes
docker-compose.yml
.github/workflows/ci.yml
```

## Tests

```bash
dotnet test backend/PromptStash.sln
cd frontend && npm test
```

## License

MIT (add a `LICENSE` file if you publish under MIT.)
