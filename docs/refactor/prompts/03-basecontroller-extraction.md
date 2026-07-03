# Prompt 3: Extract `BaseController` and Remove Duplicated Controller Code

## Context
Three controllers (`RecipeController`, `MealPlanController`, `AdminController`) each define their own `GetUserId()` and `Problem(int, string)` methods. `AuthController` is different — it's public-facing and doesn't need these helpers.

## Fixes
- **DRY-001**: Duplicated `GetUserId()` / `User.FindFirstValue` pattern across controllers
- **DRY-002**: Duplicated `Problem(int, string)` helper across controllers
- **CORRECT-002**: Remove redundant `ModelState.IsValid` checks (covered by `[ApiController]`)

## Scope

### New File
- `RecipeBook.Api/Controllers/BaseController.cs` — inherits from `ControllerBase`, provides:
  - `protected string GetUserId()` — extracts `User.FindFirstValue(ClaimTypes.NameIdentifier)`, throws `InvalidOperationException` if missing
  - `protected IActionResult Problem(int statusCode, string detail)` — returns `ObjectResult` with `ProblemDetails`

### Modified Files
- `RecipeBook.Api/Controllers/RecipeController.cs` — inherit from `BaseController`, remove `GetUserId()`, `Problem()`, and redundant `ModelState.IsValid` checks
- `RecipeBook.Api/Controllers/MealPlanController.cs` — inherit from `BaseController`, remove `GetUserId()`, `Problem()`, and redundant `ModelState.IsValid` checks
- `RecipeBook.Api/Controllers/AdminController.cs` — inherit from `BaseController`, remove `GetUserId()`, `Problem()`, and redundant `ModelState.IsValid` checks
- `RecipeBook.Api/Controllers/AuthController.cs` — **no changes** (public controller, doesn't need these helpers)

### Test Files
- No test files need changes — `BaseController` is a protected base class, tests interact via public controller interfaces

## TDD Execution Order

1. Create `BaseController.cs` with just `Problem(int, string)` method
2. Write a test that verifies `Problem(400, "test detail")` returns correct `ProblemDetails` JSON
3. Verify test passes
4. Add `GetUserId()` to `BaseController`
5. Write a test for `GetUserId()` — verify it extracts the claim correctly
6. Verify test passes
7. In `RecipeController`: change base class from `ControllerBase` to `BaseController`
8. Remove private `GetUserId()` and `Problem()` methods from `RecipeController`
9. Update all references to `GetUserId()` — should work automatically (same method name)
10. Update all references to `Problem()` — should work automatically
11. Verify `RecipeControllerTests` pass
12. Repeat steps 7-11 for `MealPlanController`
13. Repeat steps 7-11 for `AdminController`
14. Remove all `if (!ModelState.IsValid)` blocks from `RecipeController`, `MealPlanController`, `AdminController`
15. Verify all tests still pass — `[ApiController]` handles validation automatically

## Key Details
- `GetUserId()` must match existing behavior: throw `InvalidOperationException` with message `"User identity claim is missing."`
- `Problem()` must match existing behavior: return `ObjectResult` with `new ProblemDetails { Status = statusCode, Detail = detail }`
- `AuthController` stays as `ControllerBase` — no changes needed
- The `GetUserId()` method should be `protected` (used by controllers only)
- The `Problem()` method should be `protected`

## Verification
- `dotnet build` succeeds
- `dotnet test` passes all tests (no test file changes needed)
- `GetUserId()` and `Problem(int, string)` appear in **only one file**: `BaseController.cs`
- All controllers that need them inherit from `BaseController`
- `AuthController` is still `ControllerBase`
- No `if (!ModelState.IsValid)` blocks remain in controllers

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- This is a **pure extraction** — no behavioral changes. Existing tests should pass without modification after each controller's base class is updated.
- If a test fails, the issue is likely: method visibility changed, or a method was removed that was called directly. Verify method signatures match.
