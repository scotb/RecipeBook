# RecipeBook

A multi-stack recipe management and meal planning application built as a professional portfolio showcase. One shared .NET 10 REST API backend consumed by independently built UI projects — each demonstrating the same feature set across different technology stacks.

> Inspired by [AsteroidsWasm](https://github.com/aesalazar/AsteroidsWasm) — a single core library demonstrated across WPF, WinForms, Blazor, WASM, MAUI, and Electron.

**Live Demo:** _coming soon_

---

## What It Does

Registered users can:
- Browse a curated recipe catalog
- Build and manage their own private recipe collection
- Import recipes from any URL that publishes `schema.org/Recipe` structured data
- Fork catalog recipes and customise them personally
- Plan meals week-by-week with a calendar-based meal planner

---

## Architecture

```
RecipeBook (monorepo)
│
├── RecipeBook.Domain              .NET 10 class library — entities, enums, domain rules (zero dependencies)
├── RecipeBook.Application         .NET 10 class library — use cases, repository interfaces, DTOs
├── RecipeBook.Infrastructure      .NET 10 class library — EF Core, PostgreSQL, OAuth adapters
├── RecipeBook.Api                 ASP.NET Core 10 Web API — REST endpoints, JWT auth
│
├── RecipeBook.Blazor.Server       Blazor Server UI          ← v1 fully implemented
├── RecipeBook.Angular             Angular SPA               ← stub (v2)
├── RecipeBook.React               React SPA                 ← stub (v2)
├── RecipeBook.Wpf                 WPF desktop client        ← stub (v2)
│
├── RecipeBook.Domain.Tests
├── RecipeBook.Application.Tests
├── RecipeBook.Infrastructure.Tests
├── RecipeBook.Api.Tests
└── RecipeBook.Blazor.Server.Tests
```

The architecture follows **Clean Architecture** (Onion pattern):

```
Presentation  →  Infrastructure  →  Application  →  Domain
(no reverse dependencies — Domain has zero external references)
```

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 LTS |
| API | ASP.NET Core 10 Web API (REST) |
| Authentication | ASP.NET Core Identity + Google/Facebook OAuth, JWT |
| ORM | Entity Framework Core 10 (Code First, migrations) |
| Database | PostgreSQL 17 (swappable via EF Core provider model) |
| Blazor UI | Blazor Server (.NET 10) |
| Testing | xUnit + FluentAssertions + Moq + bUnit |
| CI/CD | GitHub Actions |
| Hosting | Azure App Service + Azure Database for PostgreSQL |
| Local Dev | Docker Compose |

---

## Engineering Practices

- **SOLID principles** enforced at every layer
- **Clean Architecture** — business logic has no dependency on infrastructure or UI
- **Repository Pattern** — all data access abstracted behind interfaces in the Application layer
- **TDD** — tests are written and approved before any production code is written
- **Agentic paired programming** — features are built through a structured human-agent TDD workflow (see [docs/prd/06-tdd-agentic-workflow.md](docs/prd/06-tdd-agentic-workflow.md))
- **Central Package Management** — all NuGet versions managed in `Directory.Packages.props`
- **Swappable data store** — EF Core provider is selected by configuration; switching from PostgreSQL to MySQL or SQL Server requires no code changes

---

## Getting Started (Local Development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`10.0.108` or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL + full stack)
- A Google and/or Facebook developer app (for OAuth — see setup below)

> **Note:** Unit tests (Domain, Application, Infrastructure) run without Docker. Docker is only needed to run the full application stack.

### 1. Clone and configure secrets

```bash
git clone https://github.com/YOUR_USERNAME/RecipeBook.git
cd RecipeBook
cp .env.example .env
# Edit .env and fill in your OAuth credentials and JWT secret
```

### 2. Start with Docker Compose

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| REST API | http://localhost:7100 |
| Blazor UI | http://localhost:7200 |
| PostgreSQL | localhost:5432 |

### 3. Run without Docker (API only)

```bash
# Start PostgreSQL container only
docker compose up db -d

# Apply EF Core migrations
dotnet ef database update --project RecipeBook.Infrastructure --startup-project RecipeBook.Api

# Run the API
dotnet run --project RecipeBook.Api
```

### 4. Running Tests

```bash
# All tests — no Docker needed
dotnet test RecipeBook.slnx

# Specific layers
dotnet test RecipeBook.Domain.Tests
dotnet test RecipeBook.Application.Tests
dotnet test RecipeBook.Infrastructure.Tests

# Blazor component tests
dotnet test RecipeBook.Blazor.Server.Tests
```

### OAuth Setup

1. **Google**: Create a project at [console.cloud.google.com](https://console.cloud.google.com), enable Google+ API, create OAuth 2.0 credentials. Set redirect URI to `https://localhost:7100/api/v1/auth/callback`.
2. **Facebook**: Create an app at [developers.facebook.com](https://developers.facebook.com), add Facebook Login product. Set redirect URI similarly.
3. Add credentials to your `.env` file.

---

## Project Documentation

Full design documentation lives in [`docs/prd/`](docs/prd/):

| Document | Contents |
|---|---|
| [01 — Product Overview](docs/prd/01-product-overview.md) | Vision, feature scope, v2 backlog |
| [02 — Domain Model](docs/prd/02-domain-model.md) | Entities, relationships, EF Core design |
| [03 — API Specification](docs/prd/03-api-specification.md) | All REST endpoints, DTOs, error format |
| [04 — Auth & Authorization](docs/prd/04-authentication-authorization.md) | OAuth flow, JWT, roles, policies |
| [05 — Blazor UI/UX](docs/prd/05-ui-ux-blazor.md) | All screens, components, interactions |
| [06 — TDD & Agentic Workflow](docs/prd/06-tdd-agentic-workflow.md) | Test-first process, agent prompting guide |
| [07 — Deployment](docs/prd/07-deployment-infrastructure.md) | Docker Compose, Azure resources, CI/CD |

---

## V2 Roadmap

- Shopping list generation from weekly meal plan
- Angular, React, and WPF UI implementations
- GraphQL endpoint (Hot Chocolate)
- Nutrition auto-calculation (USDA FoodData Central API)
- Alternate data store implementations (MySQL, SQL Server)
- Playwright E2E tests

---

## License

[MIT](LICENSE)
