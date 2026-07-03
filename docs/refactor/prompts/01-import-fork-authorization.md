# Prompt 1: Fix Authorization in Import and Fork Endpoints

## Context
Codebase review identified two critical security issues where authenticated users can bypass recipe visibility controls.

## Fixes
- **SEC-001**: `RecipeService.ImportAsync` — any authenticated user can import recipes without visibility/ownership checks
- **SEC-002**: `RecipeService.ForkAsync` — users can fork private recipes if they know the GUID

## Scope

### Files to Touch
- `RecipeBook.Application/Services/RecipeService.cs` — add visibility/ownership checks to `ImportAsync` and `ForkAsync`

### What to Add
**`ImportAsync`**: After fetching the recipe, verify visibility is `Public` OR the user owns the recipe. Throw `ForbiddenException` if not.

**`ForkAsync`**: After fetching the source recipe, verify visibility is `Public`. Private recipes should throw `ForbiddenException`. (Forking an owner's own private recipe is also forbidden — forking implies creating a *new* independent copy.)

### What NOT to Touch
- Do NOT change any method signatures
- Do NOT touch any controllers
- Do NOT touch any tests (yet — write new tests for these checks)

## TDD Execution Order

1. Write failing test in `RecipeBook.Application.Tests/Services/RecipeServiceTests.cs`:
   - `ForkAsync_WhenRecipeIsPrivate_ThrowsForbiddenException`
2. Implement minimal check in `RecipeService.ForkAsync`
3. Write failing test:
   - `ForkAsync_WhenRecipeIsPublic_Allowed` (verify the happy path still works)
4. Implement minimal check in `RecipeService.ForkAsync`
5. Write failing test:
   - `ImportAsync_WhenSourceRecipeIsPrivate_ThrowsForbiddenException`
6. Implement minimal check in `RecipeService.ImportAsync`
7. Write failing test:
   - `ImportAsync_WhenSourceRecipeIsPublic_Allowed`
8. Implement minimal check in `RecipeService.ImportAsync`

## Verification
- Run all existing tests: must still pass
- Run new tests: must pass
- No method signatures changed
- No controller changes

## Reminders
- Use `RecipeVisibility.Public` enum for visibility checks
- Use `NotFoundException` when recipe doesn't exist (existing behavior)
- Use `ForbiddenException` for authorization failures (existing pattern)
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- Test against the **interface** `IRecipeService`, not `RecipeService` directly
