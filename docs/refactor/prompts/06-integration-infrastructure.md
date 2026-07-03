# Prompt 1: Integration Test Infrastructure

## Context
We need to set up the testing infrastructure to support end-to-end integration tests using `WebApplicationFactory`. This allows us to test the full pipeline (Middleware → Controller → Service → Repository → Database) with real HTTP requests, rather than mocked controllers.

## Fixes
- **Infrastructure:** Implement `UserControllerContext` so the real DI container can satisfy `IUserContext` in production and test code.
- **Infrastructure:** Configure `WebApplicationFactory` to spin up the full app in-process for testing.
- **Infrastructure:** Create a JWT helper to generate valid tokens for authenticated requests.

## Scope

### New Files
1. **`RecipeBook.Api/UserContext.cs`**
   - Class `UserControllerContext` implementing `IUserContext`.
   - Uses `IHttpContextAccessor` to read the current user's identity from `HttpContext.User`.
   - `UserId`: Returns `User.FindFirstValue(ClaimTypes.NameIdentifier)`.
   - `IsInRole`: Returns `User.IsInRole(role)`.

2. **`RecipeBook.Api.Tests/Integration/IntegrationTestWebFactory.cs`**
   - Inherits from `WebApplicationFactory<Program>`.
   - Overrides `ConfigureWebHost` to:
     - Use an **in-memory SQLite** provider for the database (fast, isolated, no external DB required).
     - Provide configuration that disables real OAuth providers (Google/Facebook) or provides empty/null secrets so the app doesn't crash.
     - Ensure `IUserContext` is registered with the DI container using `UserControllerContext`.

3. **`RecipeBook.Api.Tests/Integration/IntegrationTestAuthHelper.cs`**
   - Static helper class.
   - Method `CreateToken(string userId, string email = "test@example.com", string[] roles = null)`.
   - Generates a real JWT token using the same signing key and issuer configured in `appsettings.json` (read via `WebApplicationFactory`'s config).
   - Returns a string that can be used in the `Authorization: Bearer` header.

### Modified Files
1. **`RecipeBook.Api/Program.cs`**
   - Register `IHttpContextAccessor` in DI (add `AddHttpContextAccessor()`).
   - Register `IUserContext` to be implemented by `UserControllerContext` (scoped).
   - Ensure `AddInfrastructure` is called with a connection string (can read from config or environment variable; for tests, the factory will override this).

### Test Files
- No existing tests need changes.

## TDD Execution Order

1. **Implement `UserControllerContext`**
   - Write failing test in `RecipeBook.Api.Tests/Integration/UserContextTests.cs`:
     - `GetUserId_WhenUserHasClaim_ReturnsCorrectId`
   - Implement `UserControllerContext` with `IHttpContextAccessor`.
   - Verify test passes.

2. **Register `IUserContext` in DI**
   - Add registration in `Program.cs`.
   - Write failing test in `IntegrationTestWebFactoryTests.cs`:
     - `GetUserContext_WhenResolvedFromFactory_IsNotDefault`
   - Verify test passes.

3. **Create `IntegrationTestAuthHelper`**
   - Write failing test:
     - `CreateToken_ReturnsValidJwt`
   - Implement using the app's signing key and issuer.
   - Verify token can be decoded and contains the correct claims.

4. **Configure `IntegrationTestWebFactory`**
   - Create the factory class.
   - Write failing test:
     - `Factory_CanCreateClient`
   - Implement `ConfigureWebHost` to use in-memory SQLite.
   - Verify factory starts up without errors and `GetService<T>` works.

5. **Verify App Startup**
   - Write failing test:
     - `Factory_WhenSendingRequest_ReturnsOk` (e.g., `GET /health` or any public endpoint).
   - Ensure the app starts and responds to requests.
   - Verify all existing tests still pass.

## Key Details
- **Database:** Use `Microsoft.EntityFrameworkCore.Sqlite` in-memory provider for integration tests. Do not require a running Postgres container for the test suite.
- **Security:** The JWT helper must generate tokens that pass the app's JWT validation (issuer, audience, signing key).
- **Naming:** `MethodName_Scenario_ExpectedBehavior`
- **Dependencies:** Ensure `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite` are available in the test project.

## Verification
- `dotnet build` succeeds.
- `dotnet test` passes all existing tests.
- New integration tests pass.
- `UserControllerContext` is used by the DI container.
- `IntegrationTestAuthHelper` produces valid tokens.
- `IntegrationTestWebFactory` can send HTTP requests to the app.

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code.
- **Naming**: `MethodName_Scenario_ExpectedBehavior`.
- Test against the **interface** `IUserContext`, not the concrete implementation.
- The `IntegrationTestWebFactory` must isolate the test database so each test starts with a clean slate (consider recreating the DB or using separate in-memory instances per test class).
