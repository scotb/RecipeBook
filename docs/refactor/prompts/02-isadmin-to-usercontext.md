# Prompt 2: Replace `isAdmin` Boolean with `IUserContext`

## Context
Service methods in `IRecipeService` accept `bool isAdmin` from the controller caller. This means the service trusts its caller to correctly determine admin status. If any caller passes `isAdmin: true`, all ownership checks are bypassed. This is the core architectural issue.

## Fixes
- **SEC-003**: Remove `isAdmin: bool` parameter from `IRecipeService` methods
- **SOLID-001**: Remove `isAdmin` from interface (interface segregation)
- **SOLID-003**: Replace controller-level claim extraction with scoped `IUserContext`

## Scope

### New Files
- `RecipeBook.Application/Interfaces/IUserContext.cs` — interface with `string UserId { get; }` and `bool IsInRole(string role)`

### Modified Files
- `RecipeBook.Application/Interfaces/IRecipeService.cs` — remove `isAdmin` param from `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`
- `RecipeBook.Application/Services/RecipeService.cs` — inject `IUserContext`, use `IsInRole("Admin")` instead of `isAdmin` parameter
- `RecipeBook.Api/Controllers/RecipeController.cs` — remove `User.IsInRole("Admin")`, remove manual `GetUserId()` inline logic
- `RecipeBook.Api/Controllers/AdminController.cs` — remove `isAdmin: true` argument, remove manual `GetUserId()` inline logic
- `RecipeBook.Api/Controllers/AuthController.cs` — add `IUserContext` if needed (may already have one)

### Test Files
- `RecipeBook.Application.Tests/Services/RecipeServiceTests.cs` — update all tests that pass `isAdmin`
- `RecipeBook.Api.Tests/RecipeControllerTests.cs` — update controller tests
- `RecipeBook.Api.Tests/AdminControllerTests.cs` — update admin controller tests

## TDD Execution Order

### Phase A: Create `IUserContext`
1. Write `IUserContext` interface in Application layer (no tests — it's an interface)
2. Register it in `ApplicationServiceExtensions.cs` with a scoped implementation that reads from `HttpContext.User`
   - Actually: defer implementation. Just define interface and DI registration for now.
   - **Better approach**: Define the interface, update service tests to use mock `IUserContext`

### Phase B: Update `IRecipeService` Interface
3. Remove `isAdmin` from `GetByIdAsync`, `UpdateAsync`, `DeleteAsync` signatures
4. Write failing test in `RecipeServiceTests`: any test that passes `isAdmin: true` must fail to compile
5. Minimal fix: update service method signatures to remove the param
6. Write failing test: `GetByIdAsync_WhenRecipeIsPrivate_ThrowsForbiddenException` (non-admin user)
7. Implement `IUserContext.IsInRole("Admin")` check in `RecipeService.GetByIdAsync`
8. Verify existing happy-path tests still compile and pass

### Phase C: Update `RecipeService`
9. Write failing test: `UpdateAsync_WhenUserIsNotOwnerAndNotAdmin_ThrowsForbiddenException`
10. Implement `IsInRole("Admin")` check in `RecipeService.UpdateAsync`
11. Write failing test: `DeleteAsync_WhenUserIsNotOwnerAndNotAdmin_ThrowsForbiddenException`
12. Implement `IsInRole("Admin")` check in `RecipeService.DeleteAsync`
13. Verify all existing `RecipeServiceTests` pass

### Phase D: Update Controllers
14. In `RecipeController`: add `IUserContext` to constructor (inject)
15. Write failing test: `GetById_WhenUserIsAdmin_ReturnsPrivateRecipe`
16. Replace `User.IsInRole("Admin")` with `_userContext.IsInRole("Admin")`
17. Replace inline `User.FindFirstValue(ClaimTypes.NameIdentifier)` with `_userContext.UserId`
18. Repeat steps 15-17 for `UpdateAsync` and `DeleteAsync` endpoints
19. Verify all `RecipeControllerTests` pass

### Phase E: Update `AdminController`
20. `AdminController` always passes `isAdmin: true`. Update to inject `IUserContext` instead.
21. The admin controller's delete endpoint: the service now calls `IsInRole("Admin")` internally. The controller doesn't need to pass anything.
22. Write failing test: `Delete_WhenNotAdmin_ReturnsForbidden`
23. Implement and verify

### Phase F: DI Registration
24. Add scoped registration for `IUserContext` in `ApplicationServiceExtensions.cs`
25. Implement a `UserControllerContext` (or similar) in `Api` layer that wraps `HttpContext.User`
26. Verify all controller tests pass (they use mock services — the `IUserContext` mock is per-test)

## Key Details
- `IUserContext` goes in `RecipeBook.Application.Interfaces` (application layer, not infrastructure)
- The actual implementation lives in `RecipeBook.Api` (e.g., `UserControllerContext : IUserContext`)
- For `AdminController`: admin endpoints should also check `IUserContext.IsInRole("Admin")` at the service level
- **Do not** add `[Authorize(Roles = "Admin")]` at the controller level yet — that's for a later prompt if needed
- All ownership checks must still work for non-admin users

## Verification
- `dotnet test` must pass for all 3 test projects
- No `isAdmin` parameter anywhere in production code (search for `isAdmin` — should find nothing)
- `IUserContext` must be injected into `RecipeService`
- Controllers must use `IUserContext` instead of `User.IsInRole` and `User.FindFirstValue`

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- Test against interfaces, not concrete implementations
- `ForbiddenException` for auth failures (existing pattern)
- This is the **most disruptive** prompt — expect many test signature changes. Write tests that fail to compile first, then make them pass.
