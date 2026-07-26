# RecipeBook — Implementation Handoff for Coder

**Handoff date:** 2026-07-18  
**From:** Lead (conversation analysis)  
**To:** Coder (implementation)  
**Project:** RecipeBook (Blazor Server UI against existing .NET 10 API)  
**Orchestrator:** User (local agentic coding, TDD workflow)

---

## Quick Start for Coders

1. Read this entire document to understand the project context and remaining work.
2. Read `docs/prd/06-tdd-agentic-workflow.md` for the TDD process and conventions.
3. Start with **Unit 0** (marked "REQUIRED PREREQUISITE" — nothing else works without it).
4. For each unit: propose the interface + behavior list → wait for human approval → work through tests one at a time.
5. When done with a unit, report status and stop. The orchestrator will assign the next unit.
6. Never start a new unit until the previous one is approved and merged.

---

## How to Use This Document

This is a **master map** of remaining work. Each "Unit" below is a self-contained piece of work that should be tackled independently using the TDD workflow from `docs/prd/06-tdd-agentic-workflow.md`.

**TDD process per unit:**
1. Read the unit's description
2. Propose public interface + prioritized behavior list → wait for human approval
3. RED → GREEN cycle: one test → minimal impl → next test → minimal impl
4. Refactor when all GREEN → human reviews → approve/merge
5. Move to next unit

**Never tackle two units at once.** Each unit is designed to be completed in a single focused session.

---

## Current State Summary

| Layer | Status | Tests | Notes |
|---|---|---|---|
| Domain (entities, aggregates) | ✅ Complete | 131 pass | Recipe + MealPlan |
| Application (services, DTOs) | ✅ Complete | 61 pass | Full logic |
| Infrastructure (EF Core, repos) | ✅ Complete | 24 pass | SQLite + PostgreSQL |
| API (controllers, auth) | ✅ Complete | 137 pass | Wired up, seed data added |
| Blazor (API clients) | ✅ Complete | 26 pass | All methods implemented |
| Blazor (UI pages) | 🔄 In progress | — | Catalog + Recipe Detail built |
| Blazor (shared components) | ✅ Complete | — | RecipeCard, Search, Category, Pagination |

**Total:** 379+ tests, 0 failures. Build succeeds.

---

## EF Provider Status

Both SQLite and PostgreSQL providers are already referenced in `RecipeBook.Infrastructure.csproj`:
- `Microsoft.EntityFrameworkCore.Sqlite` (v10.0.9) ✅
- `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.3) ✅

No package changes needed for C2.

---

## Critical Issues to Fix Before Building Pages

### C1. API Never Calls `AddInfrastructure()`

**File:** `RecipeBook.Api/Program.cs`

**Problem:** The API registers `AddAuthServices()`, `AddApplicationServices()`, authorization, and exception handling — but never calls `services.AddInfrastructure(...)`. This means `RecipeBookDbContext`, `IRecipeRepository`, `IMealPlanRepository`, and Identity stores are never registered. The API will crash at runtime.

**Fix:** Add before `app.Run()`:
```csharp
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Default connection string required.");
builder.Services.AddInfrastructure(connectionString);
```

**Context:** `AddInfrastructure()` is defined in `RecipeBook.Infrastructure/InfrastructureServiceExtensions.cs` and accepts a `string connectionString`.

---

### C2. Infrastructure Only Supports PostgreSQL, Not SQLite

**File:** `RecipeBook.Infrastructure/InfrastructureServiceExtensions.cs` line 17

**Problem:** Uses `options.UseNpgsql(connectionString)` unconditionally. For local dev with SQLite, this will fail.

**Fix:** Make it conditional by accepting `IConfiguration` instead of raw connection string:

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
{
    var connectionString = cfg.GetConnectionString("Default") ?? "";
    var provider = cfg["Database__Provider"] ?? "PostgreSQL";

    if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
    {
        services.AddDbContext<RecipeBookDbContext>(options =>
            options.UseSqlite(connectionString));
    }
    else
    {
        services.AddDbContext<RecipeBookDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
    // ... rest unchanged
}
```

**Then update API's `appsettings.Development.json`** to include:
```json
{
  "ConnectionStrings": {
    "Default": "DataSource=Data/app.db;Cache=Shared"
  },
  "Database__Provider": "SQLite",
  ...
}
```

---

### C3. Blazor `appsettings.json` Points to Wrong Port

**File:** `RecipeBook.Blazor.Server/appsettings.json`

**Current:**
```json
"ApiBaseUrl": "https://localhost:7182"
```

**Fix — change to:**
```json
"ApiBaseUrl": "https://localhost:7100"
```

---

### C4. Seed Data Needed

**Problem:** Fresh SQLite database has zero recipes. The Blazor catalog will be blank.

**Fix:** Add a seed method in `RecipeBook.Api/Program.cs` that runs only in Development:
```csharp
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RecipeBook.Infrastructure.Persistence.RecipeBookDbContext>();
        await DbSeeder.SeedAsync(db);
    }
}
```

**`DbSeeder` should create 4-6 sample recipes** with varied categories (Breakfast, Lunch, Dinner, Snack, Dessert), some public, some private, with realistic ingredients and steps. Use `DbContext.Recipes.Add()` then `SaveChangesAsync()`.

---

## TDD Unit Breakdown — Execute in Order

### Unit 0: Add Demo Auth Endpoint (REQUIRED PREREQUISITE)

**Type:** Single API endpoint  
**PRD reference:** PRD 03 §2 (Authentication Endpoints), PRD 04 §3.4  
**Estimated effort:** 10 minutes

**Why this first:** Before any Blazor page works, we need a way to obtain a valid JWT. Rather than hardcoding a token (which expires), we add an API endpoint that generates a fresh token on demand using the existing `TokenService`. This guarantees tokens always validate and never expire prematurely.

**Context:** The API's existing `TokenService` (`RecipeBook.Infrastructure/Identity/TokenService.cs`) already knows how to sign JWTs using the configured secret key. The dev config in `RecipeBook.Api/appsettings.json`:
```json
"Jwt": {
  "Issuer": "RecipeBook",
  "Audience": "RecipeBookAPI",
  "SecretKey": "dev-jwt-secret-key-change-me-12345",
  "TokenLifetimeMinutes": 60
}
```

**Test-first proposal (submit to human for approval):**

**Public interface:**
- `POST /api/v1/auth/demo` — returns `{"token": "eyJ..."}`
- Endpoint only active in Development environment (returns 404 otherwise)
- Uses existing `TokenService.GenerateToken()`

**Behaviors to test:**
1. `DemoEndpoint_WhenCalledInDev_Returns200WithToken`
2. `DemoToken_WhenDecoded_ContainsDemoUserIdAndAdminRole`
3. `DemoEndpoint_WhenCalledInProduction_Returns404`
4. `DemoToken_IsValid_AcceptedByJwtBearerValidation`

**Implementation spec:**
- Add `Demo()` method to `AuthController` (`RecipeBook.Api/Controllers/AuthController.cs`)
- Call `_tokenService.GenerateToken("demo-user-001", "demo@localhost", "Demo User", null, new[] { "User", "Admin" })`
- Return `Ok(new { token })`
- Guard with `if (!hostingEnvironment.IsDevelopment()) return NotFound();`

**Acceptance criteria:**
- `curl -X POST https://localhost:7100/api/v1/auth/demo` returns 200 with a JWT
- JWT validates against the configured secret key
- JWT contains roles `User` and `Admin`
- Returns 404 when app is not in Development mode
- All existing tests still pass

---

### Unit 1: Fix the Foundation (C1 + C2 + C3 + C4)

**Type:** Configuration / Wiring (not traditional TDD)  
**Estimated effort:** 30-45 minutes  
**Files changed:** `Program.cs` (Api), `InfrastructureServiceExtensions.cs`, `appsettings.Development.json` (Api), `appsettings.json` (Blazor), new `DbSeeder.cs`

**Tests (manual verification):**
1. `dotnet run --project RecipeBook.Api` starts without `InvalidOperationException`
2. `curl https://localhost:7100/api/v1/recipes` returns `200 OK` with seed data (with valid JWT from demo endpoint)
3. `dotnet test RecipeBook.slnx` — all 379 existing tests still pass

**Acceptance criteria:**
- API starts and responds to requests using SQLite
- Seed data is loaded on first startup
- Blazor can connect to the API (correct URL)
- All existing tests still pass

---

### Unit 2: Fake Authentication

**Type:** Simple Blazor feature  
**PRD reference:** PRD 04 §3.4 (Blazor Server auth), PRD 05 §3.2 (Auth Callback)  
**Estimated effort:** 15-20 minutes

**Context:** We're bypassing OAuth entirely for dev. The fake login button calls `POST /api/v1/auth/demo` to get a fresh JWT, then calls `AuthStateService.SetUser()`. The `JwtAuthorizationMessageHandler` already exists and will pick up the token automatically.

**Test-first proposal (submit to human for approval):**

**Public interface:**
- `Home.razor` — add "Login as Demo User" button
- `HttpClient` calls `POST /api/v1/auth/demo`
- `AuthStateService.SetUser()` called with returned JWT

**Behaviors to test:**
1. Home page shows "Login as Demo User" button (dev only)
2. Clicking the button calls the demo endpoint
3. Button sets auth state to authenticated with returned token
4. Auth state includes User + Admin roles
5. User is redirected to `/catalog` after login
6. `AuthStateService.GetJwt()` returns the token string
7. Outgoing API requests include the Bearer token

**Acceptance criteria:**
- Clicking the button logs the user in
- NavMenu changes to show logged-in state with logout button
- All API-calling pages work immediately after login
- Code is gated behind `#if DEBUG`

---

### Unit 3: Wire Navigation

**Type:** Simple Blazor UI update  
**PRD reference:** PRD 05 §2 (Navigation Structure)  
**Estimated effort:** 10 minutes

**Test-first proposal:**

**Public interface:**
- `NavMenu.razor` — update links, add logout button

**Behaviors to test:**
1. NavMenu shows Catalog, My Recipes, Meal Plans links
2. NavMenu shows Admin link only when user has Admin role
3. Clicking a link navigates to the correct route
4. Sign out button calls `AuthStateService.ClearUser()`
5. After sign out, user is redirected to `/`
6. All routes require authentication (unauthenticated → redirect to `/`)

**Acceptance criteria:**
- NavMenu shows correct routes
- Admin link is conditional on role
- Logout clears auth state and redirects

---

### Unit 4a: Implement Stub API Methods

**Type:** Thin API client implementation  
**PRD reference:** PRD 03 (API Specification)  
**Estimated effort:** 30 minutes

**Context:** 6 API client methods still throw `NotImplementedException`. These are thin wrappers around `HttpClient`. Implement them using the pattern already established in `RecipeApiService.CreateAsync()` as reference.

**Test-first proposal:**

**Files to implement:**
- `RecipeApiService.GetMyRecipesAsync()` — maps to `GET /api/v1/recipes/my`
- `RecipeApiService.GetByIdAsync()` — maps to `GET /api/v1/recipes/{id}`
- `RecipeApiService.UpdateAsync()` — maps to `PUT /api/v1/recipes/{id}`
- `MealPlanApiService.GetByIdAsync()` — maps to `GET /api/v1/mealplans/{id}`
- `MealPlanApiService.UpdateAsync()` — maps to `PUT /api/v1/mealplans/{id}`
- `MealPlanApiService.DeleteAsync()` — maps to `DELETE /api/v1/mealplans/{id}`

**Behaviors to test (one per method):**
1. `GetMyRecipesAsync_WhenApiReturns200_ReturnsPagedResult`
2. `GetByIdAsync_WhenApiReturns200_ReturnsRecipeDto`
3. `UpdateAsync_WhenApiReturns200_ReturnsUpdatedRecipeDto`
4. `GetByIdAsync_WhenMealPlanNotFound_ThrowsApiException`
5. `UpdateAsync_WhenApiReturns200_ReturnsUpdatedMealPlanDto`
6. `DeleteAsync_WhenApiReturns204_DoesNotThrow`

**Acceptance criteria:**
- All 6 methods implemented
- All 6 new unit tests pass
- No changes to `ApiClientServiceBase` needed
- Error handling uses existing `ApiException` pattern

---

### Unit 4b: Catalog Page

**Type:** Blazor page + shared components  
**PRD reference:** PRD 05 §3.3  
**Estimated effort:** 1-2 hours

**Test-first proposal:**

**Public interface:**
- `Catalog.razor` — `@page "/catalog"`
- `RecipeCard.razor` — shared component
- `RecipeSearchBar.razor` — shared component
- `CategoryFilter.razor` — shared component
- `PaginationBar.razor` — shared component

**Behaviors to test (prioritized):**
1. Renders loading spinner while fetching
2. Shows error when API call fails
3. Shows empty state when no results
4. Renders recipe cards for each result
5. Calls API with current filter state on load
6. Search input updates filter with debounce
7. Category chip selection updates filter
8. "View Recipe" navigates to `/recipes/{id}`
9. Pagination renders page numbers and prev/next
10. Pagination navigates on page click

**Acceptance criteria:**
- Page loads at `/catalog`
- Cards render with title, category, time, tags
- Search and category filter work
- Pagination works
- Loading/error/empty states all render

---

### Unit 4c: Recipe Detail Page

**Type:** Blazor page  
**PRD reference:** PRD 05 §3.5  
**Estimated effort:** 1-1.5 hours

**Behaviors to test:**
1. Renders recipe title, image, metadata
2. Shows two-column layout on desktop
3. Serving size scaler recalculates ingredient quantities
4. Displays ingredients with scaled quantities
5. Displays numbered steps
6. Shows "Fork this Recipe" button for non-owner public recipes
7. Shows edit/delete buttons for owner/admin
8. Shows "Private" badge for private recipes (owner only)
9. Displays owner display name and source recipe link

---

### Unit 4d: Recipe Form

**Type:** Blazor page + sub-components  
**PRD reference:** PRD 05 §3.6  
**Estimated effort:** 2-3 hours

**Prerequisite fix (do this first):**
In `RecipeDetail.razor`, fix the serving count initialization bug described above (move `_servingCount` assignment into `OnParametersSetAsync`, remove sync `OnParametersSet`).

**Behaviors to test:**
1. Create mode — all fields blank by default
2. Edit mode — fields pre-populated from API
3. Validates required fields (title, serving size, category)
4. Validates minimum 1 ingredient and 1 step
5. Dynamic ingredient rows — add, edit, remove, reorder
6. Dynamic step rows — add, edit, remove, reorder
7. Tag chips — type + Enter to add, click × to remove
8. Nutrition section is collapsible/optional
9. Saves via `CreateAsync` or `UpdateAsync`
10. Cancel navigates back to previous page
11. Shows validation errors on blur

---

### Unit 4e: My Recipes Page

**Type:** Blazor page + component extensions  
**PRD reference:** PRD 05 §3.4  
**Estimated effort:** 1-1.5 hours

**Behaviors to test:**
1. Renders recipe cards from `GetMyRecipesAsync()`
2. Shows "Add Recipe" and "Import from URL" buttons
3. Visibility filter: All / Private / Public
4. Card action menu with Edit, Delete, visibility toggle
5. Inline delete confirmation
6. Fork button for catalog recipes
7. Same card rendering as catalog (with personal actions)

---

### Unit 4f: Meal Plan Pages

**Type:** Multiple Blazor pages + modal component  
**PRD reference:** PRD 05 §3.8 through §3.10  
**Estimated effort:** 2-3 hours

**Pages:**
- `/mealplans` — list of plan cards
- `/mealplans/new` — create form
- `/mealplans/{id}` — 7-column weekly grid

**Behaviors to test:**
1. Lists meal plans sorted by date descending
2. Shows "Create New Meal Plan" button
3. Create form validates Monday date picker
4. Weekly grid shows 7 days × 4 slots
5. Empty cells show "+ Add Recipe" button
6. Filled cells show recipe image, title, remove button
7. `RecipePickerModal` for selecting recipes
8. Set/clear entries call correct API methods
9. Serving count defaults to recipe's base serving size

---

### Unit 4g: Remaining Pages (Lower Priority)

**Type:** Simple pages  
**PRD reference:** PRD 05 §3.1, §3.11  
**Estimated effort:** 1 hour

- `/` Landing page — app description + demo login button (replace placeholder)
- `/admin/recipes` — admin recipe management table (uses `GetAllRecipesAsync`)
- `/recipes/import` — URL import flow (two-step: enter URL → review → save)
- `/auth/callback` — OAuth redirect stub (can use for real OAuth later)

---

## Stub Methods Reference

All API client stubs have been implemented (see Units 4a and 4c). Remaining methods added in 4c:

| Service | Method | API Endpoint | Added in |
|---|---|---|---|
| `RecipeApiService` | `DeleteAsync` | `DELETE /api/v1/recipes/{id}` | Unit 4c |
| `RecipeApiService` | `ForkAsync` | `POST /api/v1/recipes/{id}/fork` | Unit 4c |

---

## Key Files Reference

### API
| File | Action |
|---|---|
| `RecipeBook.Api/Program.cs` | ✅ Infrastructure + seed data added |
| `RecipeBook.Api/Controllers/AuthController.cs` | ✅ `Demo()` endpoint added |
| `RecipeBook.Api/appsettings.Development.json` | ✅ Connection string + provider added |

### Infrastructure
| File | Action |
|---|---|
| `RecipeBook.Infrastructure/InfrastructureServiceExtensions.cs` | ✅ Conditional SQLite/PostgreSQL |

### Blazor
| File | Action |
|---|---|
| `RecipeBook.Blazor.Server/appsettings.json` | ✅ API URL fixed |
| `RecipeBook.Blazor.Server/Components/Pages/Home.razor` | ✅ Demo login button added |
| `RecipeBook.Blazor.Server/Components/Layout/NavMenu.razor` | ✅ Real routes + sign-out added |
| `RecipeBook.Blazor.Server/Services/RecipeApiService.cs` | ✅ All stubs + Fork/Delete |
| `RecipeBook.Blazor.Server/Services/MealPlanApiService.cs` | ✅ All stubs |
| `RecipeBook.Blazor.Server/Services/IAuthStateService.cs` | ✅ `GetUserId()` added |
| `RecipeBook.Blazor.Server/Components/Pages/Catalog.razor` | ✅ Built — Unit 4b |
| `RecipeBook.Blazor.Server/Components/Pages/RecipeDetail.razor` | ✅ Built — Unit 4c |
| `RecipeBook.Blazor.Server/Components/Pages/RecipeForm.razor` | **Create** — new page |
| `RecipeBook.Blazor.Server/Components/Pages/MyRecipes.razor` | **Create** — new page |
| `RecipeBook.Blazor.Server/Components/Pages/MealPlans.razor` | **Create** — new page |
| `RecipeBook.Blazor.Server/Components/Pages/MealPlanDetail.razor` | **Create** — new page |
| `RecipeBook.Blazor.Server/Components/Pages/MealPlanCreate.razor` | **Create** — new page |
| `RecipeBook.Blazor.Server/Components/Shared/RecipeCard.razor` | ✅ Built — Unit 4b (updated in 4c) |
| `RecipeBook.Blazor.Server/Components/Shared/RecipeSearchBar.razor` | ✅ Built — Unit 4b |
| `RecipeBook.Blazor.Server/Components/Shared/CategoryFilter.razor` | ✅ Built — Unit 4b |
| `RecipeBook.Blazor.Server/Components/Shared/PaginationBar.razor` | ✅ Built — Unit 4b |

### PRDs (DO NOT MODIFY)
| File | Purpose |
|---|---|
| `docs/prd/05-ui-ux-blazor.md` | Complete UI spec for all pages |
| `docs/prd/03-api-specification.md` | Complete API endpoint docs |
| `docs/prd/04-authentication-authorization.md` | Auth flow spec |
| `docs/prd/06-tdd-agentic-workflow.md` | TDD process |

---

## Conventions to Enforce

| Convention | Detail |
|---|---|
| **File-scoped namespaces** | `namespace RecipeBook.Blazor.Server;` not `{` on next line |
| **Private fields** | `_camelCase` prefix |
| **Central Package Management** | No `Version=` in `<PackageReference>` — versions in `Directory.Packages.props` |
| **No PropertyGroup in .csproj** | Framework/Nullable/ImplicitUsings in `Directory.Build.props` |
| **MudBlazor components** | Use `Mud*` components, not HTML primitives |
| **API calls** | Via typed `HttpClient` services, NOT direct `HttpClient` injection in components |
| **Error handling** | Catch `ApiException` in components, display via `ErrorMessage` component |
| **Auth** | Blazor circuit-scoped `AuthStateService` — never use `AuthenticationStateProvider` directly in components, use `CascadingAuthenticationState` + `AuthorizeView` |
| **No auth bypass in production** | Any fake auth code must be `#if DEBUG` gated |
| **Test naming** | `Method_WhenState_ExpectedBehavior` (e.g., `GetPublicRecipesAsync_WhenApiReturns200_ReturnsPagedResult`) |

---

## Risks & Open Questions

1. **bUnit tests for Blazor:** PRD 06 expects bUnit tests for Blazor components. The test project exists but only has stub tests. For this phase, functional testing (opening pages in browser) is the minimum. bUnit tests are recommended as follow-up but not required to mark a unit as "done."

2. **Unused Blazor EF Core references:** The Blazor `RecipeBook.Blazor.Server.csproj` references `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design`. These are harmless but unused — the Blazor app is thin (HTTP clients only). They can be removed in a cleanup pass.

3. **Docker compose not tested:** The compose stack is not tested and is out of scope. The API and Blazor are separate Docker services — this is architecturally correct but should be addressed before any cloud deployment.

---

## Unit Verification Checklist

### Unit 0 (Demo Auth Endpoint) ✅
- [x] `Demo()` method added to `AuthController`
- [x] `POST /api/v1/auth/demo` returns 200 with JWT in Development
- [x] `POST /api/v1/auth/demo` returns 404 in non-Development
- [x] JWT validates against configured secret key
- [x] JWT contains roles `User` and `Admin`
- [x] All existing tests pass

### Unit 1 (Foundation) ✅
- [x] `dotnet build RecipeBook.slnx` succeeds with 0 errors
- [x] `dotnet test RecipeBook.slnx` — all 379 existing tests pass
- [x] API starts with `dotnet run --project RecipeBook.Api` (no `InvalidOperationException`)
- [x] Seed data loads on first startup

### Unit 2 (Fake Auth) ✅
- [x] Home page shows login button (visible in dev)
- [x] Clicking button calls demo endpoint
- [x] Button sets auth state with returned token
- [x] Auth state includes User + Admin roles
- [x] Redirects to /catalog after login
- [x] Code is `#if DEBUG` gated

### Unit 3 (Navigation) ✅
- [x] NavMenu shows Catalog, My Recipes, Meal Plans
- [x] Admin link visible to demo user (has Admin role)
- [x] Logout clears auth state
- [x] Unauthenticated access redirects to `/`

### Unit 4a (Stub Methods) ✅
- [x] All 6 stub methods implemented
- [x] All 6 new unit tests pass
- [x] `dotnet test` — all tests pass

### Unit 4b (Catalog) ✅
- [x] Page loads at `/catalog`
- [x] Cards render with title, category, time, tags
- [x] Search and category filter work
- [x] Pagination works
- [x] Loading/error/empty states render

### Unit 4c (Recipe Detail) ✅
- [x] Two-column layout renders
- [x] Serving size scaler works
- [x] Ingredients/steps display correctly
- [x] Fork button visible for non-owner
- [x] Edit/delete buttons for owner/admin

**Known bug to fix in Unit 4d:** `RecipeDetail.razor` has a serving count initialization bug — `_servingCount` starts at `1` instead of the recipe's actual `ServingSize` because the sync `OnParametersSet` runs before `_recipe` is loaded in `OnParametersSetAsync`. Fix: move `_servingCount = _recipe?.ServingSize ?? 1` into `OnParametersSetAsync` after loading, remove the sync `OnParametersSet`.

### Unit 4d (Recipe Form)
- [ ] Create and edit modes work
- [ ] Validation works (required fields, min 1 ingredient/step)
- [ ] Dynamic ingredient/step rows
- [ ] Tag chips work
- [ ] Save calls correct API method
- [ ] Cancel navigates back

### Unit 4e (My Recipes)
- [ ] Cards render from personal recipes
- [ ] Visibility filter works
- [ ] Action menu renders
- [ ] Inline delete confirmation works

### Unit 4f (Meal Plans)
- [ ] List page shows plans sorted by date
- [ ] Create form validates Monday date
- [ ] Weekly grid renders 7×4
- [ ] Recipe picker modal works
- [ ] Set/clear entries call correct API methods
