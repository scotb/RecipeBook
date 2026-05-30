# PRD 01 — Product Overview & Vision

## 1. Project Identity

| Field | Value |
|---|---|
| Project name | RecipeBook |
| Repository | `RecipeBook` (monorepo) |
| Runtime | .NET 10 LTS |
| Primary language | C# |
| Version documented | v1.0 |

---

## 2. Purpose

RecipeBook is a personal recipe management and meal planning web application with two equally important goals:

1. **User value** — A registered user can browse a curated recipe catalog, build and manage their own private recipe collection, and plan meals by week.
2. **Portfolio showcase** — The codebase demonstrates professional software engineering across multiple UI technology stacks, all sharing a single backend. It is intended to be publicly accessible on GitHub and live on the web.

---

## 3. Target Users

| Role | Description |
|---|---|
| Anonymous visitor | Can view the landing/marketing page only. Cannot access recipes or meal plans. |
| Registered user | Authenticated via Google or Facebook OAuth. Full access to catalog, personal recipes, and meal planning. |
| Admin | A registered user with the `Admin` role. Can create, edit, and delete public catalog recipes. Assigned manually via configuration seeding. |

---

## 4. V1 Feature Scope

### 4.1 Authentication
- Register and log in using Google or Facebook accounts.
- No local username/password accounts.
- Profile is created automatically on first OAuth login.

### 4.2 Recipe Catalog
- Browse a curated collection of recipes provided and maintained by admins.
- Filter by category (Breakfast, Lunch, Dinner, Dessert, Snack, Appetizer, Soup, Salad, Side, Drink, Other).
- Search by title/description text.
- Paginated results.

### 4.3 Personal Recipe Collection
- Each user has their own recipe collection.
- Create a recipe manually via a structured form.
- Import a recipe by pasting a URL pointing to a page that publishes `schema.org/Recipe` JSON-LD structured data (most major recipe sites).
- Fork any public or catalog recipe into the personal collection and modify it independently.
- Edit and delete personal recipes.

### 4.4 Recipe Structure
Each recipe contains:
- Title, description, image URL (optional)
- Category (single predefined value) and free-form tags
- Prep time and cook time (optional, in minutes)
- Base serving count (used for ingredient scaling)
- Structured ingredient list (quantity, unit, name, notes per line)
- Ordered instruction steps (optional title + body per step)
- Optional nutrition fields: calories, protein (g), carbs (g), fat (g)
- Visibility flag: Private (personal) or Public (visible to all users)
- Fork provenance: if forked, stores the source recipe ID

### 4.5 Meal Planning
- Create a week-based meal plan (anchored to a Monday).
- Each day of the week has four named slots: Breakfast, Lunch, Dinner, Snack.
- Assign any recipe (personal or catalog) to any slot.
- Each meal entry records the serving count for that slot (defaults to the recipe's base serving count).
- Multiple meal plans can exist per user.

### 4.6 Serving Size Scaling
- On the recipe detail view, a numeric input allows the user to change the displayed serving count.
- Ingredient quantities scale proportionally in real time (client-side calculation).

### 4.7 Admin
- Admin users create and manage public (catalog) recipes through the same UI used by regular users, with additional controls visible only to admins.
- Admin role is not self-assignable; it is seeded via application configuration.

---

## 5. V2 Backlog (Out of Scope for V1)

| Feature | Notes |
|---|---|
| Shopping list | Aggregate ingredients across a week's meal plan, group by ingredient, sum quantities. |
| Nutrition auto-calculation | Integrate with USDA FoodData Central or Nutritionix API to auto-populate nutrition fields from structured ingredients. |
| Recipe image file upload | Blob storage (Azure Blob Storage), CDN delivery, server-side resizing. |
| Configurable meal slot names | User-defined slot names per plan (e.g., "Pre-workout", "Post-workout"). |
| E2E tests | Playwright test suite covering critical user journeys. |
| Angular UI (full build) | Complete Angular SPA consuming the shared REST API. |
| React UI (full build) | Complete React SPA consuming the shared REST API. |
| WPF UI (full build) | Complete WPF desktop client consuming the shared REST API. |
| GraphQL layer | Add a GraphQL endpoint (Hot Chocolate) as an alternative to REST. |
| Analytics | Usage analytics (recipe popularity, meal plan trends). |
| MySQL alternate data store | Alternate EF Core provider implementation for MySQL. |
| SQL Server alternate data store | Alternate EF Core provider implementation for SQL Server. |

---

## 6. Multi-Stack Showcase Design

The repository is structured as a monorepo inspired by [AsteroidsWasm](https://github.com/aesalazar/AsteroidsWasm). The shared backend (Domain, Application, Infrastructure, API) is implemented once. Each UI stack is an independent project that consumes the REST API.

```
RecipeBook/                              # All projects flat at repo root (AsteroidsWasm-inspired)
├── RecipeBook.Domain/                   # Entities, interfaces, enums (no dependencies)
├── RecipeBook.Application/              # Use cases, repository interfaces, DTOs
├── RecipeBook.Infrastructure/           # EF Core, OAuth adapters, concrete repositories
├── RecipeBook.Api/                      # ASP.NET Core REST API
├── RecipeBook.Blazor.Server/            # Blazor Server UI (v1 fully built)
├── RecipeBook.Angular/                  # Angular SPA stub (v2)
├── RecipeBook.React/                    # React SPA stub (v2)
├── RecipeBook.Wpf/                      # WPF desktop stub (v2)
├── RecipeBook.Domain.Tests/
├── RecipeBook.Application.Tests/
├── RecipeBook.Infrastructure.Tests/
├── RecipeBook.Api.Tests/
└── RecipeBook.Blazor.Server.Tests/
```

---

## 7. Engineering Principles

- **SOLID** — All code follows Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion principles.
- **Clean Architecture** — Dependencies flow inward: Presentation → Infrastructure → Application → Domain. Domain has no external dependencies.
- **Repository Pattern** — All data access is abstracted behind interfaces defined in the Application layer. EF Core implementations live in Infrastructure.
- **TDD** — All features are test-driven. Tests are written and approved before implementation begins. See PRD 06.
- **Agentic Paired Programming** — Development follows a structured human-agent collaboration workflow. See PRD 06.

---

## 8. Non-Goals for V1

- Social features (comments, ratings, following other users)
- Recipe sharing via link
- Mobile native apps
- Offline support
- Multi-language / i18n
- Subscription or payment features
- Notifications or email
