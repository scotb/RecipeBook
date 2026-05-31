# RecipeBook — Agent Handoff Document

**Last updated:** 2026-05-31  
**Workspace:** `/home/scot/git/personal-website`  
**SDK:** .NET 10.0.108 (installed, verified)

---

## Project Purpose

A multi-stack recipe management and meal planning application. One shared .NET 10 REST API backend consumed by independently built UIs. Intended as a professional GitHub portfolio showcase. Inspired by [AsteroidsWasm](https://github.com/aesalazar/AsteroidsWasm).

---

## Suggested Skills

Before proceeding, load the following skill files using the `read_file` tool:

- **`project-setup-info-local`** — for any new project scaffolding, framework initialisation, or adding new UI stacks (Angular/React/WPF stubs → full implementations).
- **`agent-customization`** — if adding or updating `.github/copilot-instructions.md`, `.instructions.md`, or custom prompt files to guide future agents working on this repo.

```
/home/scot/.vscode-server/bin/8761a5560cfd65fdd19ce7e2bd18dab5c0a4d84e/extensions/copilot/assets/prompts/skills/project-setup-info-local/SKILL.md
/home/scot/.vscode-server/bin/8761a5560cfd65fdd19ce7e2bd18dab5c0a4d84e/extensions/copilot/assets/prompts/skills/agent-customization/SKILL.md
```

> **Note:** The VS Code server binary path above may change after updates. If the path is stale, find the correct one with:
> `find ~/.vscode-server/bin -name "SKILL.md" | grep project-setup`

---

## Architecture

See [prd/01-product-overview.md](prd/01-product-overview.md) for full scope.

**Pattern:** Clean Architecture (Onion) — `Domain → Application → Infrastructure → Presentation`  
**Auth:** ASP.NET Core Identity + Google/Facebook OAuth only. JWT issued after callback. No local passwords.  
**Data:** EF Core 10 + PostgreSQL. Swappable via `Database__Provider` env var.  
**TDD + Agentic:** Tests written and human-approved before any production code.

---

## Monorepo Layout (AsteroidsWasm-inspired — flat at root)

```
RecipeBook.slnx                   Solution file (10 projects)
Directory.Build.props             Shared MSBuild props (TargetFramework, Nullable, LangVersion)
Directory.Packages.props          NuGet Central Package Management (all versions here)
global.json                       SDK pin: 10.0.108, latestMinor rollforward
NuGet.Config                      nuget.org source
.editorconfig                     C# code style (file-scoped namespaces, _camelCase fields, etc.)
.gitignore
.env.example                      Secrets template — safe to commit
docker-compose.yml                Local dev: PostgreSQL + API + Blazor
README.md
LICENSE                           MIT

RecipeBook.Domain/                 Class library — entities, enums, domain rules (zero deps)
RecipeBook.Application/            Class library — use cases, repository interfaces, DTOs
RecipeBook.Infrastructure/         Class library — EF Core, PostgreSQL, OAuth adapters
RecipeBook.Api/                    ASP.NET Core 10 Web API — REST, JWT
RecipeBook.Blazor.Server/          Blazor Server UI (v1 target)

RecipeBook.Domain.Tests/
RecipeBook.Application.Tests/
RecipeBook.Infrastructure.Tests/
RecipeBook.Api.Tests/
RecipeBook.Blazor.Server.Tests/

RecipeBook.Angular/                Empty stub folder (v2)
RecipeBook.React/                  Empty stub folder (v2)
RecipeBook.Wpf/                    Empty stub folder (v2)

docs/
  prd/                             7 PRD documents (see below)
  handoff.md                       This file

.github/
  workflows/
    pr.yml                         CI: build + test all layers on PR
    deploy.yml                     CD: test → ACR → Azure App Service on push to main
  dependabot.yml                   Weekly NuGet + Actions updates
RecipeBook.Api/Dockerfile          Multi-stage, non-root, EXPOSE 8080
RecipeBook.Blazor.Server/Dockerfile
```

---

## PRD Documents

All design decisions are locked in these documents. **Do not re-litigate them without explicit user instruction.**

| Path | Contents |
|---|---|
| `docs/prd/01-product-overview.md` | Vision, V1 feature scope, V2 backlog |
| `docs/prd/02-domain-model.md` | Entities, relationships, EF Core design, repository interfaces |
| `docs/prd/03-api-specification.md` | All REST endpoints `/api/v1/*`, DTOs, RFC 7807 errors |
| `docs/prd/04-authentication-authorization.md` | OAuth flow, JWT claims, Blazor cookie flow, roles/policies |
| `docs/prd/05-ui-ux-blazor.md` | All Blazor routes, pages, components, serving scaler, accessibility |
| `docs/prd/06-tdd-agentic-workflow.md` | 5-step TDD process, test naming convention, agent prompting templates |
| `docs/prd/07-deployment-infrastructure.md` | Docker Compose spec, Dockerfiles, Azure resources, GitHub Actions |

---

## Current Build / Test Status

```
dotnet build RecipeBook.slnx  →  0 Errors, 0 Warnings  ✅
dotnet test RecipeBook.slnx   →  161 / 161 pass         ✅
```

| Test project | Count | Notes |
|---|---|---|
| `RecipeBook.Domain.Tests` | 124 | Recipe aggregate (96) · MealPlan/MealEntry (28) |
| `RecipeBook.Application.Tests` | 33 | RecipeService (20) · MealPlanService (13) |
| `RecipeBook.Infrastructure.Tests` | 1 | Placeholder stub |
| `RecipeBook.Api.Tests` | 1 | Placeholder stub |
| `RecipeBook.Blazor.Server.Tests` | 1 | Placeholder stub |

---

## Key Conventions (enforce these)

| Convention | Detail |
|---|---|
| **TDD** | Tests written + human-approved before any production code |
| **File-scoped namespaces** | `namespace RecipeBook.Domain;` not `namespace RecipeBook.Domain { }` |
| **Private fields** | `_camelCase` prefix |
| **Central Package Management** | Never add `Version=` to `<PackageReference>` — versions live in `Directory.Packages.props` only |
| **No PropertyGroup duplication** | `TargetFramework`, `Nullable`, `ImplicitUsings`, `LangVersion` are in `Directory.Build.props` — do not repeat in `.csproj` files |
| **Base URL** | All API routes under `/api/v1/` |
| **Clean Architecture boundaries** | Domain has zero external dependencies. Repository interfaces live in Application, implementations in Infrastructure. |
| **SOLID** | Enforced at every layer |

---

## `.csproj` Special Cases

These properties must be preserved and must **not** be removed during any future cleanup:

| Project | Keep |
|---|---|
| `RecipeBook.Blazor.Server` | `<UserSecretsId>`, `<BlazorDisableThrowNavigationException>true` |
| `RecipeBook.Blazor.Server` | `<None Update="Data\app.db" ...>` item, `PrivateAssets="all"` on `EF.Design` ref |
| All test projects | `<IsPackable>false</IsPackable>`, `<Using Include="Xunit" />` |

---

## Completed Implementation

### Domain layer — `RecipeBook.Domain` ✅

**Entities (all in `RecipeBook.Domain.Entities`):**

- **`Recipe`** _(Aggregate Root)_ — `Id`, `Title`, `Description`, `ImageUrl`, `PrepTimeMinutes`, `CookTimeMinutes`, `ServingSize`, `Category`, `Visibility`, `OwnerId`, `SourceRecipeId`, nutrition fields, `CreatedAt`, `UpdatedAt`, `RejectionReason`, `Ingredients`, `Steps`, `Tags`
  - Methods: `AddIngredient`, `RemoveIngredient`, `AddStep`, `ReorderSteps`, `AddTag`, `RemoveTag`, `SubmitForReview`, `Approve`, `Reject`, `MakePrivate`, `Fork`, **`Update`** _(added during Application TDD)_
- **`Ingredient`** — child of Recipe; `Id`, `RecipeId`, `SortOrder`, `Quantity`, `Unit`, `Name`, `Notes`
- **`RecipeStep`** — child of Recipe; `Id`, `RecipeId`, `SortOrder`, `Title`, `Body`
- **`RecipeTag`** — child of Recipe; `Id`, `RecipeId`, `Name` (normalized lowercase)
- **`MealPlan`** _(Aggregate Root)_ — `Id`, `UserId`, `WeekStartDate` (must be Monday), `Name`, `CreatedAt`, `Entries`
  - Methods: `SetEntry`, `ClearEntry`, **`Rename`** _(added during Application TDD)_
- **`MealEntry`** — child of MealPlan; `Id`, `MealPlanId`, `DayOfWeek`, `MealSlot`, `RecipeId`, `ServingCount`

**Enums (all in `RecipeBook.Domain.Enums`):** `RecipeCategory`, `RecipeVisibility`, `MealSlot`

### Application layer — `RecipeBook.Application` ✅

**Interfaces (`RecipeBook.Application.Interfaces`):**

```
IRecipeRepository     — GetByIdAsync, GetPublicAsync, GetByOwnerAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync
IMealPlanRepository   — GetByIdAsync, GetByUserAsync, AddAsync, UpdateAsync, DeleteAsync
IUserLookupService    — GetDisplayNameAsync, GetDisplayNamesAsync(IEnumerable → IReadOnlyDictionary)
IRecipeImporter       — ImportFromUrlAsync(url) → CreateRecipeRequest
IRecipeService        — GetPublicRecipesAsync, GetMyRecipesAsync, GetAllRecipesAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, ForkAsync, ImportAsync
IMealPlanService      — GetByUserAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, SetEntryAsync, ClearEntryAsync
```

**DTOs (`RecipeBook.Application.DTOs`):**

| Type | Notes |
|---|---|
| `RecipePayload` (abstract record) | Shared init-only base for Create/Update; `Title`, `ServingSize`, `Category` required; rest optional |
| `CreateRecipeRequest : RecipePayload` | |
| `UpdateRecipeRequest : RecipePayload` | |
| `RecipeDto` | Full recipe with `OwnerDisplayName`, `SourceRecipeId`, all collections |
| `RecipeSummaryDto` | Paginated list view |
| `IngredientDto` | |
| `RecipeStepDto` | |
| `CreateIngredientRequest` | `(Quantity?, Unit?, Name, Notes?)` |
| `CreateRecipeStepRequest` | `(Title?, Body)` |
| `MealPlanDto` | With `IReadOnlyList<MealEntryDto>` |
| `MealPlanSummaryDto` | Includes `EntryCount` |
| `MealEntryDto` | With nested `MealEntryRecipeDto` |
| `MealEntryRecipeDto` | `(Id, Title, ImageUrl?, Category, ServingSize)` |
| `CreateMealPlanRequest` | `(WeekStartDate, Name?)` |
| `UpdateMealPlanRequest` | `(Name?)` |
| `SetMealEntryRequest` | `(DayOfWeek, MealSlot, RecipeId, ServingCount?)` |
| `ClearMealEntryRequest` | `(DayOfWeek, MealSlot)` |
| `ImportRecipeRequest` | `(Url)` |
| `ImportRecipeResult` | `(SourceUrl, Recipe: CreateRecipeRequest)` |

**Models (`RecipeBook.Application.Models`):** `RecipeQuery`, `AdminRecipeQuery`, `PagedResult<T>`

**Exceptions (`RecipeBook.Application.Exceptions`):** `NotFoundException`, `ForbiddenException`, `ConflictException`, `InvalidImportException`

**Service implementations (`RecipeBook.Application.Services`):**

- **`RecipeService`** — full `IRecipeService` implementation; maps via private `ToDto` / `ToSummaryDto` / `ToSummaryPageAsync` helpers; uses `IUserLookupService.GetDisplayNamesAsync` for batch owner-name resolution in list queries
- **`MealPlanService`** — full `IMealPlanService` implementation; loads recipes on-demand per-entry via `IRecipeRepository`; maps via `ToDto` / `ToSummaryDto` / `LoadRecipeMapAsync`

---

## What Has NOT Been Done (implementation backlog)

Implementation should proceed strictly in TDD order per `docs/prd/06-tdd-agentic-workflow.md`.

1. ~~**Domain layer**~~ ✅
2. ~~**Domain unit tests**~~ ✅ (124 tests)
3. ~~**Application layer**~~ ✅
4. ~~**Application unit tests**~~ ✅ (33 tests)
5. **Infrastructure layer** — EF Core `DbContext`, entity configs, repository implementations (`IRecipeRepository`, `IMealPlanRepository`), `IUserLookupService` implementation (queries `ApplicationUser.DisplayName`), migrations (`RecipeBook.Infrastructure`)
6. **Infrastructure integration tests** — Testcontainers PostgreSQL (`RecipeBook.Infrastructure.Tests`)
7. **API layer** — controllers, JWT auth, OAuth callback, OpenAPI (`RecipeBook.Api`)
8. **API integration tests** — `WebApplicationFactory` + `HttpClient` (`RecipeBook.Api.Tests`)
9. **Blazor UI** — pages, components, auth state service (`RecipeBook.Blazor.Server`)
10. **Blazor component tests** — bUnit (`RecipeBook.Blazor.Server.Tests`)

---

## Secrets / Credentials

All secrets use environment variables. **Never hardcode.**

Template lives at `.env.example`. Required variables:
- `JWT_SECRET_KEY` (min 32 chars)
- `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET`
- `FACEBOOK_APP_ID` / `FACEBOOK_APP_SECRET`
- `ADMIN_SEED_EMAILS`

GitHub Actions deployment requires these repository secrets (Azure OIDC):
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`

---

## Useful Commands

```bash
# Build
dotnet build RecipeBook.slnx

# Run all tests
dotnet test RecipeBook.slnx

# Start local stack (PostgreSQL + API + Blazor)
docker compose up --build

# PostgreSQL only (for running API outside Docker)
docker compose up db -d

# Apply EF migrations
dotnet ef database update --project RecipeBook.Infrastructure --startup-project RecipeBook.Api

# Run API directly
dotnet run --project RecipeBook.Api
```

---

## Important: What to Check Before Starting

1. Run `dotnet build RecipeBook.slnx` — must be 0 errors, 0 warnings.
2. Run `dotnet test RecipeBook.slnx` — all tests must pass.
3. Read the PRD document relevant to the layer you're implementing before writing any code.
4. Follow the TDD workflow in `docs/prd/06-tdd-agentic-workflow.md` — tests first, human approval, then production code.
5. If Domain or Application gaps are found during a later layer's TDD cycle, **stop and report** before proceeding. Don't patch forward silently.
