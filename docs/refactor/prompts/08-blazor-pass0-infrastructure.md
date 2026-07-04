# Blazor Server — Pass 0: Foundation Infrastructure

## Goal
Replace the default ASP.NET Core Identity scaffold in `RecipeBook.Blazor.Server` with the foundational plumbing for a JWT-authenticated Blazor app that calls the RecipeBook API.

This pass produces a compilable project with:
- MudBlazor installed and wired
- `AuthStateService` (JWT auth state per circuit)
- `JwtAuthorizationMessageHandler` (injects Bearer token into outgoing requests)
- `RecipeApiService` + `MealPlanApiService` (thin API wrappers)
- `ApiClientServiceBase` (common HTTP error handling)
- DI wiring, updated `Routes.razor`, `App.razor`, `MainLayout.razor`, `NavMenu.razor`

**No pages yet.** Only infrastructure and layout shell.

## API Contract (for service implementations)
- Base URL (dev): `https://localhost:7182` (from `appsettings.json` key `ApiBaseUrl`)
- All endpoints require `Authorization: Bearer <JWT>` except callback
- All errors return RFC 7807 ProblemDetails

### Key DTOs (from `RecipeBook.Application.DTOs` — add project reference)
`RecipeDto`, `RecipeSummaryDto`, `IngredientDto`, `RecipeStepDto`, `MealPlanDto`, `MealPlanSummaryDto`, `MealEntryDto`, `CreateRecipeRequest`, `UpdateRecipeRequest`, `CreateMealPlanRequest`, `UpdateMealPlanRequest`, `SetMealEntryRequest`, `ImportRecipeRequest`, `ImportRecipeResult`

### Enums (from `RecipeBook.Domain.Enums`)
`RecipeCategory`: Breakfast, Lunch, Dinner, Dessert, Snack, Appetizer, Drink
`RecipeVisibility`: Private, PendingReview, Public

---

## WORKFLOW — One test → one minimal fix → pass → next test

**Do NOT batch-write all code then all tests.** Work in tight RED→GREEN cycles.

### Phase A: Services (unit tests via Moq + HttpMessageHandler)

#### A1. `PagedResult<T>` record
- Simple record, no tests needed.

#### A2. `Services/IRecipeApiService.cs` — interface definition
- No test, just define the interface:

```csharp
public interface IRecipeApiService
{
    Task<PagedResult<RecipeSummaryDto>> GetPublicRecipesAsync(string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<RecipeSummaryDto>> GetMyRecipesAsync(string? search, string? category, string? tags, int page, int pageSize, CancellationToken ct = default);
    Task<RecipeDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RecipeDto> CreateAsync(CreateRecipeRequest request, CancellationToken ct = default);
    Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<RecipeDto> ForkAsync(Guid id, CancellationToken ct = default);
    Task<ImportRecipeRequest> ImportAsync(string url, CancellationToken ct = default);
}
```

#### A3. `Services/IMealPlanApiService.cs` — interface definition
- No test, just define the interface:

```csharp
public interface IMealPlanApiService
{
    Task<IEnumerable<MealPlanSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<MealPlanDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MealPlanDto> CreateAsync(CreateMealPlanRequest request, CancellationToken ct = default);
    Task<MealPlanDto> UpdateAsync(Guid id, UpdateMealPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MealEntryDto> SetEntryAsync(Guid id, SetMealEntryRequest request, CancellationToken ct = default);
    Task DeleteEntryAsync(Guid id, DayOfWeek dayOfWeek, MealSlot mealSlot, CancellationToken ct = default);
}
```

#### A4. `Services/ApiClientServiceBase.cs` — base class for API services
This is a protected base class that API services inherit. It handles common HTTP error patterns from ProblemDetails responses.

- **Test 1:** `SendAsync_WhenResponseIs401_CallsAuthStateServiceClearAndRedirects` — verify that a 401 response causes the base class to call `AuthStateService.ClearUser()` and set a redirect URI on the result.
- **Test 2:** `SendAsync_WhenResponseIs403_CallsAuthStateServiceClearAndSetsForbiddenRedirect` — verify 403 maps to a `/forbidden` redirect.
- **Test 3:** `SendAsync_WhenResponseIs200_DeserializesJsonCorrectly` — verify happy-path JSON deserialization works.
- **Test 4:** `SendAsync_WhenResponseHasProblemDetails_ErrorContainsDetailMessage` — verify that a 422 response deserializes the ProblemDetails `detail` field into an exception message.

#### A5. `Services/AuthStateService.cs` — implements `AuthenticationStateProvider`
Scoped service. Stores JWT string and parsed `ClaimsPrincipal` in server-side circuit memory (never in browser storage).

- **Test 1:** `SetUser_WhenCalled_StoresClaimsAndRaisesEvent` — set a valid JWT, verify `AuthenticationStateChanged` was raised, verify the returned `AuthenticationState.User.Identity.IsAuthenticated` is true and claims are present.
- **Test 2:** `SetUser_WhenCalled_ExtractsSubAndEmailClaims` — verify `Sub` claim maps to `NameIdentifier` and `email` claim is accessible.
- **Test 3:** `ClearUser_WhenCalled_ResetsToAnonymousPrincipal` — after `ClearUser()`, `AuthenticationState.User.Identity.IsAuthenticated` should be false.
- **Test 4:** `GetJwt_WhenCalledAfterSetUser_ReturnsRawTokenString` — verify the raw JWT is retrievable.
- **Test 5:** `GetAuthenticationStateAsync_WhenNoUser_ReturnsAnonymousState` — default state should be anonymous.

#### A6. `Services/JwtAuthorizationMessageHandler.cs` — DelegatingHandler
Injects the JWT from `AuthStateService` into outgoing `HttpClient` requests.

- **Test 1:** `SendAsync_WhenUserIsAuthenticated_AddsBearerAuthorizationHeader` — mock `AuthStateService` returning a token, send a request, verify the `Authorization` header is set to `Bearer <token>`.
- **Test 2:** `SendAsync_WhenUserIsNotAuthenticated_DoesNotAddAuthorizationHeader` — `GetJwt()` returns null/empty, verify no `Authorization` header.
- **Test 3:** `SendAsync_PassesRequestThroughToNextHandler` — verify the inner handler still receives the original request (not modified beyond the auth header).

#### A7. `Services/RecipeApiService.cs` — implements `IRecipeApiService`
Uses `ApiClientServiceBase`. Each method calls the appropriate API endpoint.

- **Test 1:** `GetPublicRecipesAsync_WhenApiReturns200_ReturnsPagedResult` — mock `HttpMessageHandler` to return a JSON `RecipeSummaryDto[]`, verify the result maps correctly.
- **Test 2:** `CreateAsync_WhenApiReturns201_ReturnsRecipeDto` — mock POST response, verify the returned DTO.
- **Test 3:** `DeleteAsync_WhenApiReturns204_DoesNotThrow` — mock 204 response, verify no exception.
- **Test 4:** `ForkAsync_WhenApiReturns409_ThrowsApiException` — mock 409 response, verify exception.
- **Test 5:** `ImportAsync_WhenApiReturns422_ThrowsApiExceptionWithProblemDetailsMessage` — mock 422 with ProblemDetails body, verify exception message contains the `detail` field.

#### A8. `Services/MealPlanApiService.cs` — implements `IMealPlanApiService`
Same pattern as A7.

- **Test 1:** `GetAllAsync_WhenApiReturns200_ReturnsMealPlanSummaries` — mock GET, verify mapping.
- **Test 2:** `CreateAsync_WhenApiReturns201_ReturnsMealPlanDto` — mock POST, verify mapping.
- **Test 3:** `SetEntryAsync_WhenApiReturns200_ReturnsMealEntryDto` — mock PUT, verify mapping.

### Phase B: DI Wiring and Project Structure

#### B1. Install MudBlazor NuGet package
- Add `MudBlazor` to `.csproj`
- No test — just verify build succeeds

#### B2. Add project reference to `RecipeBook.Application`
- Add `<ProjectReference Include="..\RecipeBook.Application\RecipeBook.Application.csproj" />` to the Blazor `.csproj`
- This gives direct access to all DTOs, enums, and interfaces — no DTO duplication needed.

#### B3. Update `Program.cs`
Replace the entire `Program.cs`:
- **Remove** all Identity-related registrations (`AddIdentityCore`, `AddDbContext<ApplicationDbContext>`, `AddSignInManager`, `AddDefaultTokenProviders`, `IdentityRevalidatingAuthenticationStateProvider`)
- Keep `AddRazorComponents().AddInteractiveServerComponents()`
- Register `AddCascadingAuthenticationState()`
- Register `AddScoped<AuthenticationStateProvider, AuthStateService>()`
- Register `AddMudServices()`
- Register `IRecipeApiService` and `IMealPlanApiService` with `HttpClient` pointing at `ApiBaseUrl` from `appsettings.json`
- Register `JwtAuthorizationMessageHandler` in the `HttpClient` pipeline
- Keep `UseHttpsRedirection()`, `UseStaticAssets()`, `UseAntiforgery()`
- Map `Routes.razor`
- **Remove** `app.MapAdditionalIdentityEndpoints()`

- **Test 1:** `Program_DependencyGraph_Compiles` — just build the project. If it compiles, all service registrations are type-safe.
- **Test 2:** `Program_ApiClientBaseAddress_IsFromConfig` — verify `appsettings.json` has `"ApiBaseUrl": "https://localhost:7182"` and the services resolve.

#### B4. Update `appsettings.json`
Add:
```json
"ApiBaseUrl": "https://localhost:7182"
```

### Phase C: Layout and Routing

#### C1. Update `App.razor`
- Replace Bootstrap link with MudBlazor CSS
- Add MudBlazor JS before `</body>`
- Remove `PasskeySubmit.razor.js` script reference
- Keep `HeadOutlet`, `Routes`, `ReconnectModal`

#### C2. Update `Routes.razor`
- Wrap the `Router` in `CascadingAuthenticationState` so `AuthStateService` propagates to all components.

#### C3. Update `MainLayout.razor`
- Switch from Bootstrap grid to `MudLayout` → `MudAppBar` + `MudDrawer` + `MudContent`
- Keep the general idea: sidebar nav + main content area
- Use MudBlazor components

#### C4. Update `NavMenu.razor`
- Replace hardcoded Counter/Weather/Home links with:
  - `Catalog` → `/catalog`
  - `My Recipes` → `/recipes`
  - `Meal Plans` → `/mealplans`
  - `Admin` (Admin role only) → `/admin/recipes`
  - Login button (not authorized)
- Use `MudListItem` and `MudNavLink` components
- Show user's display name when authorized

---

## Constraints
- **No pages yet.** Only infrastructure: services, auth plumbing, layout.
- **No JavaScript interop.** URL fragment parsing comes in Pass 1.
- **No API error handling logic.** Just the structure.
- The project **must compile** after this pass.
- The project **must run** (even if pages 404).
- Follow existing code style: full words (`string`, `return`), PascalCase for public members, XML docs for public APIs.
- Work **incrementally**: one test → minimal impl → pass → next test. Do NOT write all code then all tests.
- Run `dotnet build` after each GREEN step to confirm nothing has regressed.

## Files to create
1. `Services/PagedResult.cs`
2. `Services/IRecipeApiService.cs`
3. `Services/IMealPlanApiService.cs`
4. `Services/ApiClientServiceBase.cs`
5. `Services/AuthStateService.cs`
6. `Services/JwtAuthorizationMessageHandler.cs`
7. `Services/RecipeApiService.cs`
8. `Services/MealPlanApiService.cs`

## Files to modify
1. `RecipeBook.Blazor.Server.csproj` — add MudBlazor, project reference
2. `Program.cs` — complete rewrite of DI pipeline
3. `App.razor` — replace Bootstrap with MudBlazor
4. `Routes.razor` — add `CascadingAuthenticationState`
5. `MainLayout.razor` — switch to `MudLayout`
6. `NavMenu.razor` — replace nav links
7. `appsettings.json` — add `ApiBaseUrl`

## Verification
- `dotnet build` succeeds with 0 warnings and 0 errors
- `dotnet run` starts without errors on `https://localhost:7205`
- All unit tests pass
