# RecipeBook — Agent Instructions

## TDD is Non-Negotiable

This project uses strict vertical-slice TDD. Violating this process is the most critical mistake an agent can make here. It has happened multiple times and must not happen again.

### The Only Permitted Sequence

```
1. Write ONE test → it must fail to compile or fail at runtime (RED)
2. Show the RED output — stop here, do not proceed
3. Write ONLY the minimum production code to make that one test pass (GREEN)
4. Run tests → confirm GREEN
5. Go to step 1 for the next behavior
```

### Hard Rules

- **Never write production code before a test exists for it.** If you find yourself writing a class, method, or configuration without a failing test that requires it, stop immediately.
- **One test at a time.** Writing multiple tests then multiple implementations is explicitly forbidden.
- **"Setup" is not an exception.** There is no such thing as infrastructure that must be written before tests. The first test defines what minimum setup is needed.
- **After writing one test, stop and confirm RED before writing any production code.**
- **Refactor only when all behaviors are GREEN.** Never while RED.
- **Do not modify a test to make it pass.** Flag it for human review instead.

### What "Minimum Production Code" Means

Only write what is needed to make the current failing test pass. If test #1 tests `AddAsync` on `RecipeRepository`, write only:
- The `RecipeBookDbContext` with a single `DbSet<Recipe>` 
- A `RecipeConfiguration` with enough config for the test to run
- `RecipeRepository` with only `AddAsync`
- A migration for just that table

Do NOT write `MealPlanRepository`, `UserLookupService`, or configs for other entities until a test requires them.

### Self-Check Before Writing Any Production Code

Ask yourself: "Does a currently-failing test require this code?" If the answer is no, do not write it.

---

## Project Reference

- **PRD docs:** `docs/prd/` — design decisions are locked, do not re-litigate without explicit instruction
- **TDD workflow:** `docs/prd/06-tdd-agentic-workflow.md` — read this before every implementation session
- **Handoff doc:** `docs/handoff.md` — current project state and completed work

## Key Conventions

| Convention | Detail |
|---|---|
| File-scoped namespaces | `namespace RecipeBook.Domain;` not `namespace RecipeBook.Domain { }` |
| Private fields | `_camelCase` prefix |
| Central Package Management | Never add `Version=` to `<PackageReference>` |
| No PropertyGroup duplication | `TargetFramework`, `Nullable`, `ImplicitUsings` are in `Directory.Build.props` |
| Test naming | `MethodOrScenario_StateUnderTest_ExpectedBehavior` |
| Clean Architecture | Domain → Application → Infrastructure → Presentation; no layer skipping |

## Build & Test Commands

```bash
dotnet build RecipeBook.slnx
dotnet test RecipeBook.slnx
dotnet test RecipeBook.Infrastructure.Tests/RecipeBook.Infrastructure.Tests.csproj
```
