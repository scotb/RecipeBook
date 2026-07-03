# Prompt 5: Hardening Pass — Exception Safety and Cleanup

## Context
After the major refactors, two remaining issues need addressing:
1. `GlobalExceptionHandler` could leak internal information via exception messages
2. `AuthController.GetMe` has a redundant authentication check

## Fixes
- **SEC-004**: `GlobalExceptionHandler` returns `ex.Message` directly in ProblemDetails — sanitize for 500 errors
- **CORRECT-001**: `AuthController.GetMe` checks `!User.Identity?.IsAuthenticated` despite `[Authorize]` attribute

## Scope

### Modified Files
- `RecipeBook.Api/GlobalExceptionHandler.cs` — for 500 errors, return generic detail message. Log the full exception.
- `RecipeBook.Api/Controllers/AuthController.cs` — remove the `!User.Identity?.IsAuthenticated` check in `GetMe`

## TDD Execution Order

### Part A: Sanitize Exception Messages
1. Write failing test in `RecipeBook.Api.Tests/GlobalExceptionHandlingTests.cs`:
   - `HandleAsync_WhenExceptionIsInternalServerError_ReturnsGenericMessage`
   - The test should throw an `InvalidOperationException` (or similar) and verify the response detail is generic
2. Implement: in the catch-all 500 handler, use a generic message instead of `ex.Message`
3. Verify test passes
4. Write failing test:
   - `HandleAsync_WhenExceptionIsNotFound_ReturnsOriginalMessage`
   - Verify that 404-level errors still include the specific message (useful for client behavior)
5. Implement: only sanitize for 500 errors; preserve messages for client-level errors (400, 404, 403)
6. Verify all tests pass

### Part B: Remove Redundant Auth Check
7. Write failing test in `AuthControllerTests.cs`:
   - `GetMe_Returns200WhenUserIsAuthenticated` (this should already exist and pass)
8. Remove the `if (!User.Identity?.IsAuthenticated == true)` check from `GetMe`
9. Verify the existing test still passes
10. If no existing test covers this, write one first, then remove the check

## Key Details
- **Generic message for 500 errors**: `"An unexpected error occurred. Please try again later."`
- **Log the exception**: `logger.LogError(ex, "Unhandled exception")` — this is where the real error info goes
- **Preserve specific messages** for 400, 403, 404 errors — these are client-facing and expected
- **For `AuthController.GetMe`**: the `[Authorize]` attribute already ensures the user is authenticated. The extra check is dead code.

## Verification
- `dotnet test` passes all tests
- `GlobalExceptionHandler` returns generic message for 500 errors (not `ex.Message`)
- `GlobalExceptionHandler` preserves `ex.Message` for 400, 403, 404 errors
- `AuthController.GetMe` has no `IsAuthenticated` check
- All existing tests still pass

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- This is a cleanup pass — minimal behavioral changes
- The `GlobalExceptionHandler` test may need to verify the `ProblemDetails.Detail` field specifically
- If `GlobalExceptionHandlerTests` doesn't exist yet, create it with the structure matching existing controller test files
