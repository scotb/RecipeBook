# PRD 02 — Domain Model & Data Design

## 1. Principles

- The Domain layer has **zero external dependencies**. No EF Core, no NuGet packages beyond the .NET BCL.
- Entities are defined as C# classes with encapsulated behavior. Anemic models are avoided.
- EF Core is an **infrastructure concern**. The Domain defines the shape; Infrastructure configures persistence.
- All repository interfaces are defined in the **Application layer**, not Domain.
- Aggregate roots control access to their child collections.

---

## 2. Enumerations

### `RecipeCategory`
```csharp
// Namespace: RecipeBook.Domain.Enums
public enum RecipeCategory
{
    Breakfast,
    Lunch,
    Dinner,
    Dessert,
    Snack,
    Appetizer,
    Soup,
    Salad,
    Side,
    Drink,
    Other
}
```

### `RecipeVisibility`
```csharp
// Namespace: RecipeBook.Domain.Enums
public enum RecipeVisibility
{
    Private,       // Visible only to the owner
    PendingReview, // Submitted by the owner; awaiting admin approval
    Public         // Approved by an admin; visible to all authenticated users
}
```

> **Approval workflow:** Any user may call `SubmitForReview()` to move a recipe from `Private` or `Public` to `PendingReview`. Only an admin may call `Approve()` (→ `Public`) or `Reject(reason)` (→ `Private`). Rejection stores the reason in `RejectionReason`.

### `MealSlot`
```csharp
// Namespace: RecipeBook.Domain.Enums
public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snack
}
```

---

## 3. Entities

### 3.1 `Recipe` _(Aggregate Root)_

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK, generated on creation |
| `Title` | `string` | Yes | Max 200 chars |
| `Description` | `string?` | No | Max 2000 chars |
| `ImageUrl` | `string?` | No | Must be a valid URL if present |
| `PrepTimeMinutes` | `int?` | No | Positive integer |
| `CookTimeMinutes` | `int?` | No | Positive integer |
| `ServingSize` | `int` | Yes | Base serving count; min 1 |
| `Category` | `RecipeCategory` | Yes | Single predefined category |
| `Visibility` | `RecipeVisibility` | Yes | Default: `Private` |
| `OwnerId` | `string` | Yes | FK → ASP.NET Core Identity `ApplicationUser.Id` |
| `SourceRecipeId` | `Guid?` | No | FK → `Recipe.Id`; non-null if this was forked |
| `CaloriesPerServing` | `decimal?` | No | Optional; positive |
| `ProteinGrams` | `decimal?` | No | Optional; positive |
| `CarbsGrams` | `decimal?` | No | Optional; positive |
| `FatGrams` | `decimal?` | No | Optional; positive |
| `CreatedAt` | `DateTimeOffset` | Yes | Set on creation, UTC |
| `UpdatedAt` | `DateTimeOffset` | Yes | Updated on every mutating call, UTC; guaranteed monotonically increasing |
| `RejectionReason` | `string?` | No | Set by `Reject(reason)`; cleared by `Approve()` or `SubmitForReview()` |
| `Ingredients` | `IReadOnlyList<Ingredient>` | Yes | Child collection; empty by default |
| `Steps` | `IReadOnlyList<RecipeStep>` | Yes | Child collection; empty by default |
| `Tags` | `IReadOnlyList<RecipeTag>` | Yes | Child collection; empty by default |

**Domain methods:**
- `AddIngredient(...)` — validates and adds an ingredient, enforcing sort order
- `RemoveIngredient(Guid ingredientId)` — removes ingredient and re-sequences sort order
- `AddStep(...)` — validates and appends a step
- `ReorderSteps(IEnumerable<Guid> orderedStepIds)` — re-sequences step sort order; throws if IDs don't exactly match the current step set
- `AddTag(string name)` — adds a tag (normalized to lowercase/trimmed; case-insensitive deduplication)
- `RemoveTag(string name)` — removes a tag by name (no-op if not found)
- `SubmitForReview()` — transitions `Private` or `Public` → `PendingReview`; no-op if already `PendingReview`; clears `RejectionReason`
- `Approve()` — transitions `PendingReview` → `Public`; throws if not pending; clears `RejectionReason`
- `Reject(string reason)` — transitions `PendingReview` → `Private`; throws if not pending; sets `RejectionReason`
- `MakePrivate()` — transitions any visibility → `Private`; no-op if already `Private`
- `Fork(string newOwnerId)` — returns a new `Recipe` with `SourceRecipeId` set, `Visibility = Private`, deep-copied child collections

---

### 3.2 `Ingredient`

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK |
| `RecipeId` | `Guid` | Yes | FK → `Recipe.Id` |
| `SortOrder` | `int` | Yes | 0-based; maintained by parent aggregate |
| `Quantity` | `decimal?` | No | e.g., `2.5` |
| `Unit` | `string?` | No | e.g., `"cups"`, `"tbsp"` — max 50 chars |
| `Name` | `string` | Yes | e.g., `"all-purpose flour"` — max 200 chars |
| `Notes` | `string?` | No | e.g., `"sifted"`, `"room temperature"` — max 500 chars |

No entity-level behavior beyond construction validation.

---

### 3.3 `RecipeStep`

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK |
| `RecipeId` | `Guid` | Yes | FK → `Recipe.Id` |
| `SortOrder` | `int` | Yes | 0-based; maintained by parent aggregate |
| `Title` | `string?` | No | Optional step heading — max 200 chars |
| `Body` | `string` | Yes | Instruction text — max 5000 chars |

---

### 3.4 `RecipeTag`

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK |
| `RecipeId` | `Guid` | Yes | FK → `Recipe.Id` |
| `Name` | `string` | Yes | Free-form; max 100 chars; stored normalized (trimmed, lowercase) |

Unique constraint: `(RecipeId, Name)` — no duplicate tags on the same recipe.

---

### 3.5 `MealPlan` _(Aggregate Root)_

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK |
| `UserId` | `string` | Yes | FK → `ApplicationUser.Id` |
| `WeekStartDate` | `DateOnly` | Yes | Must be a Monday |
| `Name` | `string?` | No | Optional label — max 100 chars |
| `CreatedAt` | `DateTimeOffset` | Yes | UTC |
| `Entries` | `IReadOnlyList<MealEntry>` | Yes | 28 possible slots (7 days × 4 slots); sparse |

**Domain methods:**
- `SetEntry(DayOfWeek day, MealSlot slot, Guid? recipeId, int? servingCount)` — upserts a meal entry for a slot; passing `null` for `recipeId` clears the slot. When adding a **new** entry, `servingCount` is required (throws if null — the Application layer must resolve the recipe's `ServingSize` as the default). When **updating** an existing entry, `null` preserves the current `ServingCount`.
- `ClearEntry(DayOfWeek day, MealSlot slot)` — removes the entry if present
- Validation: `WeekStartDate` must be a Monday (`DayOfWeek.Monday`)

---

### 3.6 `MealEntry`

**Namespace:** `RecipeBook.Domain.Entities`

| Property | Type | Required | Notes |
|---|---|---|---|
| `Id` | `Guid` | Yes | PK |
| `MealPlanId` | `Guid` | Yes | FK → `MealPlan.Id` |
| `DayOfWeek` | `DayOfWeek` | Yes | Monday–Sunday |
| `MealSlot` | `MealSlot` | Yes | Breakfast, Lunch, Dinner, or Snack |
| `RecipeId` | `Guid?` | No | FK → `Recipe.Id`; null = empty slot |
| `ServingCount` | `int` | Yes | Min 1; required when adding a new entry (Domain throws if omitted — Application layer resolves the recipe's `ServingSize` as the default); optional on update (null preserves the existing value) |

**Unique constraint:** `(MealPlanId, DayOfWeek, MealSlot)` — one entry per slot per day per plan.

---

### 3.7 `ApplicationUser` _(Identity)_

**Namespace:** `RecipeBook.Infrastructure.Identity`

Extends `IdentityUser` from ASP.NET Core Identity.

| Property | Type | Notes |
|---|---|---|
| `DisplayName` | `string?` | Populated from OAuth provider profile on first login |
| `AvatarUrl` | `string?` | Profile picture URL from OAuth provider |
| `CreatedAt` | `DateTimeOffset` | Timestamp of first registration |

Note: `ApplicationUser` lives in the Infrastructure layer because it depends on `Microsoft.AspNetCore.Identity`. The Domain references users only by their `string` ID.

---

## 4. Repository Interfaces

Defined in `RecipeBook.Application.Interfaces`. Implemented in `RecipeBook.Infrastructure.Repositories`.

### `IRecipeRepository`
```csharp
Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<PagedResult<Recipe>> GetPublicAsync(RecipeQuery query, CancellationToken ct = default);
Task<PagedResult<Recipe>> GetByOwnerAsync(string ownerId, RecipeQuery query, CancellationToken ct = default);
Task<Recipe> AddAsync(Recipe recipe, CancellationToken ct = default);
Task UpdateAsync(Recipe recipe, CancellationToken ct = default);
Task DeleteAsync(Guid id, CancellationToken ct = default);
Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
```

### `IMealPlanRepository`
```csharp
Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
Task<IReadOnlyList<MealPlan>> GetByUserAsync(string userId, CancellationToken ct = default);
Task<MealPlan> AddAsync(MealPlan mealPlan, CancellationToken ct = default);
Task UpdateAsync(MealPlan mealPlan, CancellationToken ct = default);
Task DeleteAsync(Guid id, CancellationToken ct = default);
```

### Supporting types (in `RecipeBook.Application.Models`)
```csharp
public record RecipeQuery(
    string? SearchText = null,
    RecipeCategory? Category = null,
    IReadOnlyList<string>? Tags = null,
    int Page = 1,
    int PageSize = 20
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```

---

## 5. Entity Relationship Diagram

```
ApplicationUser (Identity)
    │
    ├─< Recipe (OwnerId)
    │       │
    │       ├─< Ingredient (RecipeId)
    │       ├─< RecipeStep (RecipeId)
    │       └─< RecipeTag (RecipeId)
    │
    └─< MealPlan (UserId)
            │
            └─< MealEntry (MealPlanId)
                    │
                    └── Recipe? (RecipeId, nullable)
```

`Recipe.SourceRecipeId` is a nullable self-referencing FK (fork provenance).

---

## 6. Database Design

### 6.1 EF Core Configuration

- Code First with migrations.
- All migrations live in `RecipeBook.Infrastructure/Migrations/`.
- `RecipeBookDbContext` extends `IdentityDbContext<ApplicationUser>`.
- Entity configurations use the Fluent API in separate `IEntityTypeConfiguration<T>` classes per entity, located in `RecipeBook.Infrastructure/Persistence/Configurations/`.

### 6.2 Table Naming
EF Core default table naming is overridden to use snake_case via a model convention or explicit `ToTable()` calls:

| Entity | Table Name |
|---|---|
| `Recipe` | `recipes` |
| `Ingredient` | `ingredients` |
| `RecipeStep` | `recipe_steps` |
| `RecipeTag` | `recipe_tags` |
| `MealPlan` | `meal_plans` |
| `MealEntry` | `meal_entries` |

### 6.3 Indexes

| Table | Columns | Type | Purpose |
|---|---|---|---|
| `recipes` | `(owner_id, visibility)` | Composite | Personal recipe list queries |
| `recipes` | `(visibility)` | Single | Catalog browse queries |
| `recipes` | `(source_recipe_id)` | Single | Fork lineage lookups |
| `recipe_tags` | `(recipe_id, name)` | Unique composite | Prevent duplicate tags |
| `meal_entries` | `(meal_plan_id, day_of_week, meal_slot)` | Unique composite | Enforce one entry per slot |

### 6.4 Swappable Provider Pattern

The EF Core provider is registered in `RecipeBook.Infrastructure` and resolved by `RecipeBook.Api` at startup via configuration. The connection string and provider name come from environment variables or `appsettings.json`.

```csharp
// In Infrastructure registration extension method:
services.AddDbContext<RecipeBookDbContext>(options =>
{
    var provider = configuration["Database:Provider"]; // "PostgreSQL" | "MySQL" | "SqlServer"
    var connectionString = configuration.GetConnectionString("Default");

    _ = provider switch
    {
        "PostgreSQL"  => options.UseNpgsql(connectionString),
        "MySQL"       => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
        "SqlServer"   => options.UseSqlServer(connectionString),
        _             => throw new InvalidOperationException($"Unsupported database provider: {provider}")
    };
});
```

### 6.5 Soft Delete
V1 uses **hard delete**. Records are permanently removed when deleted. Soft delete (an `IsDeleted` flag + query filter) is a V2 consideration.

---

## 7. Value Objects

### `PagedResult<T>`
Defined in Application layer. Not persisted. Used as the return type for paginated repository queries.

---

## 8. Domain Validation Rules Summary

| Rule | Where Enforced |
|---|---|
| Recipe `Title` must not be empty | Domain entity constructor / method |
| Recipe `ServingSize` must be ≥ 1 | Domain entity constructor |
| `Ingredient.Name` must not be empty | Ingredient constructor |
| `RecipeStep.Body` must not be empty | RecipeStep constructor |
| `RecipeTag.Name` is normalized to lowercase trimmed | RecipeTag constructor |
| `MealPlan.WeekStartDate` must be a Monday | MealPlan constructor / `SetEntry` |
| `MealEntry.ServingCount` must be ≥ 1 | MealEntry constructor |
| One entry per `(MealPlanId, DayOfWeek, MealSlot)` | DB unique constraint + domain `SetEntry` |
| Tag names are unique per recipe (case-insensitive) | Domain `AddTag` method |
