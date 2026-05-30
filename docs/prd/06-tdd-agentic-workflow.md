# PRD 06 — TDD & Agentic Workflow Guide

## 1. Philosophy

RecipeBook is built using **Test-Driven Development (TDD)** with **agentic paired programming**. The core principles are:

> **Tests verify behavior through public interfaces, not implementation details.**  
> **One test. One implementation. Repeat.**

Tests describe *what* the system does, not *how*. A good test reads like a specification and survives internal refactors. Code can change entirely; tests should not need to change unless observable behavior changes.

**The anti-pattern to avoid — horizontal slicing:**  
Writing all tests for a feature upfront, then writing all the implementation, produces tests that verify the *shape* of imagined code rather than real behavior. They become insensitive to real changes and break on refactors that don't affect behavior. This approach is explicitly avoided.

**The correct approach — vertical slices via tracer bullets:**  
One test → minimal implementation → next test → minimal implementation → repeat. Each test responds to what was learned from the previous cycle.

```
WRONG (horizontal):
  RED:   test1, test2, test3, test4, test5
  GREEN: impl1, impl2, impl3, impl4, impl5

RIGHT (vertical):
  RED → GREEN: test1 → impl1
  RED → GREEN: test2 → impl2
  RED → GREEN: test3 → impl3
  ...
```

---

## 2. Build Order

The stack is built layer by layer. Each layer's public interface is agreed before any code is written. No layer is started until the layer below it is complete and tests pass.

```
1. Domain        (RecipeBook.Domain + RecipeBook.Domain.Tests)
2. Application   (RecipeBook.Application + RecipeBook.Application.Tests)
3. Infrastructure(RecipeBook.Infrastructure + RecipeBook.Infrastructure.Tests)
4. API           (RecipeBook.Api + RecipeBook.Api.Tests)
5. Blazor UI     (RecipeBook.Blazor.Server + RecipeBook.Blazor.Server.Tests)
```

---

## 3. The TDD Workflow (Per Feature / Behavior Group)

Every unit of work — one entity, one use case, one endpoint, one component — follows this sequence:

```
┌─────────────────────────────────────────────────────┐
│  STEP 1: Plan — Interface + Behavior List            │
│  Agent reads the relevant PRD section and proposes:  │
│  - The public interface (constructor signature,      │
│    method names, parameters, return types)           │
│  - A prioritized list of behaviors to test           │
│  Human engineer reviews and approves (or adjusts)    │
│  the interface and behavior list before any code     │
│  is written.                                         │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 2: Tracer Bullet                               │
│  Agent writes ONE test for the first / most          │
│  important behavior. Test fails to compile or        │
│  fails at runtime — that is expected (RED).          │
│  Agent writes the minimum production code to make    │
│  it pass (GREEN).                                    │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 3: Incremental Loop                            │
│  For each remaining behavior in the approved list:   │
│    RED:   Write next test → fails                    │
│    GREEN: Minimal code to pass → passes              │
│  Rules:                                              │
│  - One test at a time                                │
│  - Only enough code to pass the current test         │
│  - Do not anticipate future tests                    │
│  - Never refactor while RED                          │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 4: Refactor                                    │
│  All behaviors are GREEN. Now look for:              │
│  - Duplicated logic to extract                       │
│  - Modules to deepen (simplify interface,            │
│    move complexity inward)                           │
│  - SOLID principles to apply naturally               │
│  Run tests after every refactor step.                │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 5: Human Reviews Implementation                │
│  Engineer reviews the production code for:           │
│  - Correct layer placement (no cross-layer leaks)    │
│  - SOLID compliance                                  │
│  - Security concerns                                 │
│  - Code clarity and naming                           │
│  Engineer approves (merge) or requests changes.      │
└─────────────────────────────────────────────────────┘
```

**The human's two review gates:**
1. **Before coding** — approve the interface design and behavior list (Step 1)
2. **After all behaviors are green** — approve the final implementation (Step 5)

The agent works autonomously through Steps 2–4.

---

## 4. Testing Stack

| Library | Role |
|---|---|
| `xUnit` | Test runner and assertion framework |
| `FluentAssertions` | Expressive assertion syntax (`.Should().Be(...)`) |
| `Moq` | Mock/stub creation for interfaces |
| `bUnit` | Blazor component testing |
| `Microsoft.AspNetCore.Mvc.Testing` | API integration tests via `WebApplicationFactory` |
| `Testcontainers.PostgreSql` | Spins up a real PostgreSQL container for Infrastructure integration tests |

---

## 5. Test Project Layout

```
RecipeBook.Domain.Tests/            # Unit tests for Domain entities and logic
RecipeBook.Application.Tests/       # Unit tests for Application use-case handlers
RecipeBook.Infrastructure.Tests/    # Integration tests (EF Core + real PostgreSQL via Testcontainers)
RecipeBook.Api.Tests/               # Integration tests (WebApplicationFactory + in-memory or test DB)
RecipeBook.Blazor.Server.Tests/     # Blazor component tests (bUnit)
```

---

## 6. Test Naming Convention

All test methods follow this pattern:

```
MethodOrScenario_StateUnderTest_ExpectedBehavior
```

Examples:
```csharp
AddIngredient_WithValidIngredient_AppendsToIngredientsCollection()
AddIngredient_WithEmptyName_ThrowsArgumentException()
AddTag_WithDuplicateNameDifferentCase_DoesNotAddDuplicate()
Fork_WhenCalledOnPublicRecipe_ReturnsNewRecipeWithSourceRecipeIdSet()
Fork_WhenCalledOnPrivateRecipe_ThrowsInvalidOperationException()
GetPublicAsync_WithCategoryFilter_ReturnsOnlyMatchingRecipes()
CreateRecipe_WithValidRequest_Returns201Created()
CreateRecipe_WithMissingTitle_Returns422UnprocessableEntity()
```

---

## 7. Test Coverage by Layer

### 7.1 Domain Tests (`RecipeBook.Domain.Tests`)

Tests for entity behavior and domain rules. **No mocks, no I/O, no DI.** Pure C# objects.

**What to test:**
- All entity constructor validation (required fields, range checks)
- All domain method behaviors (AddIngredient, Fork, SetEntry, etc.)
- Edge cases for domain rules (duplicate tag detection, Monday validation for MealPlan, etc.)
- Aggregate invariant enforcement

**What NOT to test:**
- Persistence
- HTTP
- Any infrastructure concern

**Example:**
```csharp
public class RecipeTests
{
    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        var act = () => new Recipe(
            title: "",
            ownerId: "user-1",
            category: RecipeCategory.Dinner,
            servingSize: 4
        );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*title*");
    }

    [Fact]
    public void AddTag_WithDuplicateNameDifferentCase_DoesNotAddDuplicate()
    {
        var recipe = RecipeFactory.CreateValid();
        recipe.AddTag("Quick");
        recipe.AddTag("quick");

        recipe.Tags.Should().HaveCount(1);
        recipe.Tags.Single().Name.Should().Be("quick");
    }
}
```

---

### 7.2 Application Tests (`RecipeBook.Application.Tests`)

Tests for use-case handlers (CQRS commands/queries or service methods). **Mocked repository interfaces.** No database, no HTTP.

**What to test:**
- Handler logic: correct repository methods are called with correct arguments
- Return value mapping: domain entities are correctly mapped to DTOs
- Error paths: not-found, forbidden, business rule violations
- Orchestration: multiple repository calls in correct order

**Example:**
```csharp
public class ForkRecipeHandlerTests
{
    private readonly Mock<IRecipeRepository> _recipeRepo = new();
    private readonly ForkRecipeHandler _handler;

    public ForkRecipeHandlerTests()
    {
        _handler = new ForkRecipeHandler(_recipeRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenSourceRecipeIsPublic_CreatesForkedRecipeAndPersists()
    {
        var source = RecipeFactory.CreatePublic(id: Guid.NewGuid());
        _recipeRepo.Setup(r => r.GetByIdAsync(source.Id, default)).ReturnsAsync(source);
        _recipeRepo.Setup(r => r.AddAsync(It.IsAny<Recipe>(), default))
                   .ReturnsAsync((Recipe r, CancellationToken _) => r);

        var result = await _handler.Handle(new ForkRecipeCommand(source.Id, "user-2"), default);

        result.SourceRecipeId.Should().Be(source.Id);
        result.OwnerId.Should().Be("user-2");
        result.Visibility.Should().Be(RecipeVisibility.Private);
        _recipeRepo.Verify(r => r.AddAsync(It.IsAny<Recipe>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSourceRecipeIsPrivate_ThrowsForbiddenException()
    {
        var source = RecipeFactory.CreatePrivate(id: Guid.NewGuid());
        _recipeRepo.Setup(r => r.GetByIdAsync(source.Id, default)).ReturnsAsync(source);

        var act = async () => await _handler.Handle(new ForkRecipeCommand(source.Id, "user-2"), default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
```

---

### 7.3 Infrastructure Tests (`RecipeBook.Infrastructure.Tests`)

Integration tests against a real PostgreSQL database spun up in a Docker container via Testcontainers.

**What to test:**
- EF Core repository implementations produce correct SQL results
- Unique constraints are enforced by the database
- Pagination produces correct results
- Complex queries (filter + sort + page) return correct data

**Setup pattern:**
```csharp
public class RecipeRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private RecipeBookDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Apply migrations against the test container
        var options = new DbContextOptionsBuilder<RecipeBookDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _context = new RecipeBookDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task GetPublicAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        // Arrange: seed dinner and breakfast recipes
        // Act: query with Category = Dinner
        // Assert: only dinner recipes returned
    }
}
```

---

### 7.4 API Tests (`RecipeBook.Api.Tests`)

Integration tests using `WebApplicationFactory<Program>`. Tests the full HTTP request/response pipeline including routing, model binding, authorization, and response serialization.

**What to test:**
- Correct HTTP status codes for happy paths
- Correct HTTP status codes for error paths (404, 403, 422, etc.)
- Request body validation
- Authorization (unauthenticated requests return 401, wrong-owner requests return 403)
- Response body shape matches expected DTO

**Auth in tests:** A `TestAuthHandler` is registered in the test `WebApplicationFactory` to inject a configurable `ClaimsPrincipal` without OAuth:

```csharp
public class RecipesControllerTests : IClassFixture<RecipeBookApiFactory>
{
    [Fact]
    public async Task GetRecipe_WhenRecipeIsPrivateAndCallerIsNotOwner_Returns403()
    {
        var client = _factory.CreateClientWithUser("other-user-id");
        var privateRecipeId = await _factory.SeedPrivateRecipeForUser("owner-user-id");

        var response = await client.GetAsync($"/api/v1/recipes/{privateRecipeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

---

### 7.5 Blazor Component Tests (`RecipeBook.Blazor.Server.Tests`)

Tests using bUnit. Test component rendering and interactive behavior.

**What to test:**
- Component renders correctly given props/parameters
- User interactions (button clicks, form input) trigger correct state changes
- Correct API calls are made (via mocked `HttpClient` or mocked service)
- Loading states and error states render correctly
- Serving size scaler correctly recalculates ingredient quantities

**Example:**
```csharp
public class ServingSizeScalerTests : TestContext
{
    [Fact]
    public void WhenServingCountIsDoubled_IngredientQuantitiesDouble()
    {
        var recipe = RecipeFactory.CreateWithIngredient(quantity: 2m, servingSize: 4);
        var cut = RenderComponent<RecipeDetail>(p => p.Add(r => r.Recipe, recipe));

        cut.Find("input[type=number]").Change(8); // double the servings

        cut.Find(".ingredient-quantity").TextContent.Should().Be("4");
    }
}
```

---

## 8. Test Factories & Helpers

A shared `RecipeBook.Tests.Shared` project (not a test project itself — a class library) provides:

- `RecipeFactory` — creates valid domain entities for use in tests (avoids test setup boilerplate)
- `MealPlanFactory` — same for meal plans
- `ClaimsPrincipalFactory` — creates test `ClaimsPrincipal` objects with configurable roles
- `RecipeBookApiFactory` — `WebApplicationFactory` subclass with seeding helpers

This project is referenced by all test projects.

---

## 9. Continuous Integration Test Execution

Tests are run in CI (GitHub Actions) on every push:
- Domain + Application tests: run directly (no containers needed)
- Infrastructure tests: run with Docker-in-Docker (Testcontainers provides the PostgreSQL container)
- API tests: run with an in-memory test database (or Testcontainers — configurable)
- Blazor component tests: run directly (no browser needed, bUnit is headless)

**All tests must pass before a PR can be merged.**

---

## 10. What Agents Are Responsible For

When working with an agent on a feature, the agent is expected to:

1. **Read the relevant PRD section** before proposing any interface or behavior list
2. **Propose a public interface + prioritized behavior list** and wait for human approval before writing code
3. **Work through behaviors one at a time** — one test → one implementation → next test
4. **Write minimum production code** per cycle — no speculation, no extra features
5. **Run tests after each GREEN step** to confirm nothing has regressed
6. **Refactor only when all behaviors are GREEN** — never while RED
7. **Not modify a test to make it pass** — if a test is wrong, flag it for human review
8. **Respect layer boundaries** — Domain has no dependencies, Application does not reference Infrastructure, etc.
9. **Follow SOLID principles** in all production code
10. **Ask before making architectural decisions** not covered by the PRD

---

## 11. Prompting the Agent

**Step 1 — Planning prompt (start of a new unit of work):**

```
Feature: [feature name]
PRD reference: [section in the relevant PRD doc]
Layer: [Domain | Application | Infrastructure | API | Blazor]

Read the PRD section and propose:
1. The public interface (class/method signatures, constructor parameters, return types)
2. A prioritized list of behaviors to test

Do not write any code yet. Wait for approval.
```

**Step 2 — Execution prompt (after human approves the interface + behavior list):**

```
The interface and behavior list are approved.
Start TDD: tracer bullet first, then work through the behavior list one test at a time.
One test → minimal implementation → next test.
Refactor after all behaviors are green. Show me the final result when done.
```

**If a behavior is unclear during the loop:**
```
Pause. I'm unsure how to test [specific behavior] — the PRD says [quote].
How should this behave when [edge case]?
```
