# Prompt 4: Extract Query Builder from Controllers

## Context
`RecipeController.BuildQuery` contains complex string parsing, enum validation, and query construction logic. This is business logic that belongs outside the controller.

## Fixes
- **SOLID-002**: Controller doing more than orchestration
- **CORRECT-003**: Brittle manual parsing logic in controller

## Scope

### New File
- `RecipeBook.Application/Models/RecipeQueryBuilder.cs` — a static helper class that:
  - Accepts raw parameters (`string? search`, `string? category`, `string? tags`, `int page`, `int pageSize`)
  - Validates inputs
  - Returns a `RecipeQuery` object or an error result
  - Handles: whitespace-only strings → null, invalid category → error, page < 1 → error, pageSize < 1 → error, pageSize > 100 → error, tag parsing from comma-separated strings

### Modified Files
- `RecipeBook.Api/Controllers/RecipeController.cs` — replace inline `BuildQuery` calls with `RecipeQueryBuilder.Create(...)`
- `RecipeBook.Api/Controllers/AdminController.cs` — if it has similar query building logic, replace it too
- **Do NOT** create a separate DTO for the input — accept the raw parameters that the controller already receives

## TDD Execution Order

1. Write failing test in `RecipeBook.Application.Tests/Models/RecipeQueryBuilderTests.cs`:
   - `Create_WhenPageIsZero_ReturnsError`
2. Minimal implementation: return error for page < 1
3. Write failing test: `Create_WhenPageSizeIsZero_ReturnsError`
4. Minimal implementation: return error for pageSize < 1
5. Write failing test: `Create_WhenPageSizeExceeds100_ReturnsError`
6. Minimal implementation: return error for pageSize > 100
7. Write failing test: `Create_WhenSearchIsWhitespaceOnly_ReturnsNullSearch`
8. Minimal implementation: trim and null-check search
9. Write failing test: `Create_WhenCategoryIsInvalid_ReturnsError`
10. Minimal implementation: validate category enum
11. Write failing test: `Create_WhenCategoryIsValid_ReturnsValidQuery`
12. Minimal implementation: accept valid category
13. Write failing test: `Create_WhenTagsAreCommaSeparated_ParsesCorrectly`
14. Minimal implementation: split tags on comma
15. Write failing test: `Create_WhenAllValidParameters_ProducesValidQuery`
16. Implement full query construction
17. Verify all tests pass
18. Update `RecipeController` to use `RecipeQueryBuilder.Create()`
19. Verify all controller tests pass
20. Check `AdminController` for similar query building — if present, apply same pattern

## Key Details
- The `RecipeQueryBuilder` should return a discriminated union or result type: `(Error? error, RecipeQuery? query)` — match the existing return type of `BuildQuery`
- The validation rules must match exactly what `BuildQuery` currently does:
  - `page < 1` → 400 error
  - `pageSize < 1` or `pageSize > 100` → 400 error
  - Whitespace-only search → null
  - Whitespace-only category → null
  - Invalid category string → 400 error
  - Tags: comma-separated string → `string[]`
- `RecipeQueryBuilder` lives in `RecipeBook.Application.Models` (application layer)
- `RecipeQueryBuilder` is a `static` class — no DI needed

## Verification
- `dotnet test` passes all tests
- `RecipeController.BuildQuery()` is removed
- `AdminController` has no similar query building logic (or it uses `RecipeQueryBuilder` too)
- `RecipeQueryBuilderTests` has comprehensive coverage of input validation

## Reminders
- **lean-ctx**: `str`/`ret` are display-only. Use full words `string`/`return` in source code
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- The `RecipeQueryBuilder` should be `public static class` with a single `Create` method
- Keep the method simple — validation logic only, no business rules
