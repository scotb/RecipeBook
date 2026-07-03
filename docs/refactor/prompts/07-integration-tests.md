# Prompt 2: Integration Tests

## Context
Infrastructure is set up (`IntegrationTestWebFactory`, `IntegrationTestAuthHelper`, in-memory SQLite). Now we write behavioral tests that exercise the full pipeline: HTTP request → Middleware → Controller → Service → Repository → Database → Response.

## Scope

### New Test Files
1. **`RecipeBook.Api.Tests/Integration/RecipeEndpointTests.cs`**
2. **`RecipeBook.Api.Tests/Integration/MealPlanEndpointTests.cs`**
3. **`RecipeBook.Api.Tests/Integration/AuthEndpointTests.cs`**

### Tests to Implement (TDD Order)

#### 1. Recipe Discovery (`GET /api/v1/recipes`)
- `GetPublicRecipes_WhenNoAuth_Returns200AndPublicRecipes`
  - Verify 200 OK.
  - Verify response contains only public recipes.
  - Verify pagination works (check `totalCount` and `items` count).

#### 2. Recipe Detail (`GET /api/v1/recipes/{id}`)
- `GetRecipe_WhenPrivateAndUnauthorized_Returns403`
  - Create a private recipe in the DB (via service or direct factory).
  - Send request without auth.
  - Verify 403 Forbidden with ProblemDetails.
- `GetRecipe_WhenPrivateAndOwner_Returns200`
  - Create a private recipe owned by user A.
  - Send request as user A.
  - Verify 200 OK.

#### 3. Recipe Creation (`POST /api/v1/recipes`)
- `CreateRecipe_WhenUnauthorized_Returns401`
  - Send POST without token.
  - Verify 401 Unauthorized.
- `CreateRecipe_WhenValid_Returns201AndCreatedLocation`
  - Send POST with valid body and auth token.
  - Verify 201 Created.
  - Verify `Location` header is set.
  - Verify recipe exists in DB with correct owner ID.

#### 4. Fork (`POST /api/v1/recipes/{id}/fork`)
- `ForkRecipe_WhenPrivate_Returns403`
  - Create a private recipe.
  - Send fork request as a *different* user.
  - Verify 403 Forbidden.
- `ForkRecipe_WhenPublic_Returns201`
  - Create a public recipe by user A.
  - Send fork request as user B.
  - Verify 201 Created.
  - Verify new recipe exists with owner = user B.

#### 5. Import (`POST /api/v1/recipes/import`)
- `ImportRecipe_WhenSourceIsPrivate_Returns403`
  - Mock or configure the `IRecipeImporter` to return a private recipe.
  - Send import request.
  - Verify 403 Forbidden.
- `ImportRecipe_WhenSourceIsPublic_Returns200`
  - Mock `IRecipeImporter` to return a public recipe.
  - Send import request.
  - Verify 200 OK.
  - Verify recipe exists in DB.

#### 6. Auth (`POST /auth/logout`)
- `Logout_WhenAuthenticated_Returns200`
  - Send POST with auth token.
  - Verify 200 OK.

## TDD Execution Order

1. **Set up test data factory**
   - In `IntegrationTestWebFactory`, provide a way to get the `RecipeService` and `MealPlanService` from the DI container so tests can create test data.
   - Write failing test: `Factory_CanResolveRecipeService`.
   - Verify test passes.

2. **Write first recipe tests**
   - `GetPublicRecipes_WhenNoAuth_Returns200AndPublicRecipes`
   - Verify test fails (no routes/endpoint).
   - Ensure the endpoint is wired (it should be from previous work).
   - Verify test passes.

3. **Write auth checks**
   - `GetRecipe_WhenPrivateAndUnauthorized_Returns403`
   - Verify test fails (returns 200 or 404).
   - Verify the `GetById` endpoint checks visibility.
   - Verify test passes.

4. **Write creation tests**
   - `CreateRecipe_WhenValid_Returns201AndCreatedLocation`
   - Verify test fails.
   - Ensure the `CreateAsync` endpoint works with real DB.
   - Verify test passes.

5. **Write fork/import tests**
   - `ForkRecipe_WhenPrivate_Returns403`
   - Verify test fails.
   - Ensure `ForkAsync` checks visibility.
   - Verify test passes.

6. **Write meal plan tests**
   - `CreateMealPlan_WhenUnauthorized_Returns401`
   - `CreateMealPlan_WhenValid_Returns201`

## Key Details
- **Database Isolation:** Each test should start with a clean database. Consider using a new `WebApplicationFactory` instance per test class or resetting the DB state before each test.
- **ProblemDetails:** Use FluentAssertions to verify the `ProblemDetails` JSON structure (status, type, title, detail) for error responses.
- **Naming:** `Method_Scenario_ExpectedBehavior`
- **Dependencies:** Ensure `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite` are available in the test project.

## Verification
- `dotnet build` succeeds.
- `dotnet test` passes all tests (existing + new).
- All new tests exercise the full pipeline (HTTP → DB → HTTP).
- No mocks for controllers or services in integration tests (only `IRecipeImporter` is mocked for import tests).
- Error responses return valid RFC 7807 ProblemDetails.

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code.
- **Naming**: `MethodName_Scenario_ExpectedBehavior`.
- Test against the **public HTTP endpoints**, not internal methods.
- The `IntegrationTestWebFactory` must be used for all tests.
- If a test requires a specific user, use `IntegrationTestAuthHelper.CreateToken` to generate the JWT.
