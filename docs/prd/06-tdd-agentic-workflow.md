# PRD 06 — TDD & Agentic Workflow Guide

## 1. Philosophy

RecipeBook is built using **Test-Driven Development (TDD)** with **agentic paired programming**. The core principle is:

> **Tests are the specification. Code exists to make tests pass.**

No production code is written until the tests that validate it are written, reviewed, and approved by the human engineer. This creates a clear contract: the agent knows exactly what "done" looks like before writing a single line of implementation.

---

## 2. The TDD Workflow (Per Feature)

Every feature — no matter how small — follows this exact sequence:

```
┌─────────────────────────────────────────────────────┐
│  STEP 1: Define Acceptance Criteria                  │
│  Human engineer + agent discuss the feature.         │
│  Agree on: what the code must do, edge cases,        │
│  error conditions, and the public API surface.       │
│  Output: written acceptance criteria (in the         │
│  GitHub issue or task description).                  │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 2: Agent Writes Tests                          │
│  Agent generates all test code for the feature.      │
│  Tests reference the types and method signatures     │
│  that WILL exist, but do not yet. Tests will fail    │
│  to compile at this point — that is expected.        │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 3: Human Reviews and Approves Tests            │
│  Engineer reads every test. Verifies:                │
│  - Tests match the agreed acceptance criteria        │
│  - Edge cases are covered                            │
│  - Test names are clear and follow conventions       │
│  - No test is testing implementation details         │
│  Engineer approves (merge/commit tests) or requests  │
│  changes. Agent revises. Repeat until approved.      │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 4: Agent Implements Production Code            │
│  Agent writes the minimum production code needed     │
│  to make all tests pass. Follows SOLID and Clean     │
│  Architecture. Does not write code not covered       │
│  by a test.                                          │
│  Agent runs tests and iterates until all pass.       │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│  STEP 5: Human Reviews and Approves Implementation   │
│  Engineer reviews the production code for:           │
│  - SOLID compliance                                  │
│  - Correct layer placement (no cross-layer leaks)    │
│  - Security concerns                                 │
│  - Code clarity and naming                           │
│  Engineer approves (merge) or requests changes.      │
└─────────────────────────────────────────────────────┘
```

**No step is skipped.** Implementation does not begin until Step 3 is complete.

---

## 3. Testing Stack

| Library | Role |
|---|---|
| `xUnit` | Test runner and assertion framework |
| `FluentAssertions` | Expressive assertion syntax (`.Should().Be(...)`) |
| `Moq` | Mock/stub creation for interfaces |
| `bUnit` | Blazor component testing |
| `Microsoft.AspNetCore.Mvc.Testing` | API integration tests via `WebApplicationFactory` |
| `Testcontainers.PostgreSql` | Spins up a real PostgreSQL container for Infrastructure integration tests |

---

## 4. Test Project Layout

```
RecipeBook.Domain.Tests/            # Unit tests for Domain entities and logic
RecipeBook.Application.Tests/       # Unit tests for Application use-case handlers
RecipeBook.Infrastructure.Tests/    # Integration tests (EF Core + real PostgreSQL via Testcontainers)
RecipeBook.Api.Tests/               # Integration tests (WebApplicationFactory + in-memory or test DB)
RecipeBook.Blazor.Server.Tests/     # Blazor component tests (bUnit)
```

---

## 5. Test Naming Convention

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

## 6. Test Coverage by Layer

### 6.1 Domain Tests (`RecipeBook.Domain.Tests`)

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

### 6.2 Application Tests (`RecipeBook.Application.Tests`)

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

### 6.3 Infrastructure Tests (`RecipeBook.Infrastructure.Tests`)

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

### 6.4 API Tests (`RecipeBook.Api.Tests`)

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

### 6.5 Blazor Component Tests (`RecipeBook.Blazor.Server.Tests`)

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

## 7. Test Factories & Helpers

A shared `RecipeBook.Tests.Shared` project (not a test project itself — a class library) provides:

- `RecipeFactory` — creates valid domain entities for use in tests (avoids test setup boilerplate)
- `MealPlanFactory` — same for meal plans
- `ClaimsPrincipalFactory` — creates test `ClaimsPrincipal` objects with configurable roles
- `RecipeBookApiFactory` — `WebApplicationFactory` subclass with seeding helpers

This project is referenced by all test projects.

---

## 8. Continuous Integration Test Execution

Tests are run in CI (GitHub Actions) on every push:
- Domain + Application tests: run directly (no containers needed)
- Infrastructure tests: run with Docker-in-Docker (Testcontainers provides the PostgreSQL container)
- API tests: run with an in-memory test database (or Testcontainers — configurable)
- Blazor component tests: run directly (no browser needed, bUnit is headless)

**All tests must pass before a PR can be merged.**

---

## 9. What Agents Are Responsible For

When working with an agent on a feature, the agent is expected to:

1. **Understand the acceptance criteria** before writing any code
2. **Write tests first** in the correct test project, following the naming convention
3. **Not write production code** until instructed (after human approval of tests)
4. **Write minimum production code** — no gold-plating, no extra features
5. **Run the tests** and iterate until all pass
6. **Not modify tests** to make them pass — if a test is wrong, flag it for human review
7. **Respect layer boundaries** — Domain has no dependencies, Application does not reference Infrastructure, etc.
8. **Follow SOLID principles** in all production code
9. **Ask before making architectural decisions** not covered by the PRD

---

## 10. Prompting the Agent

When starting a new feature, provide the agent with:

```
Feature: [feature name]
PRD reference: [section in the relevant PRD doc]
Layer: [Domain | Application | Infrastructure | API | Blazor]
Acceptance criteria:
  - [criterion 1]
  - [criterion 2]
  - [edge case 1]

Task: Write the tests for this feature only. Do not write production code.
Follow the test naming convention from PRD 06.
Reference the test project: RecipeBook.[Layer].Tests/
```

After human approval of the tests:

```
The tests for [feature] have been approved. 
Now implement the production code to make all tests pass.
Do not add any functionality beyond what the tests require.
Reference layer: [layer].
```
