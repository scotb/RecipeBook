# PRD 05 — UI/UX Requirements (Blazor Server v1)

## 1. Overview

| Attribute | Value |
|---|---|
| Project | `RecipeBook.Blazor.Server` |
| Framework | Blazor Web App (.NET 10) |
| Render mode | Static SSR for `/` (landing page) · Interactive Server (SignalR circuit) for all authenticated pages |
| CSS / component framework | **MudBlazor** (Material Design component library — pure Blazor, no JS dependencies, no npm build step) |
| Auth | Circuit-scoped `AuthStateService` (implements `AuthenticationStateProvider`); JWT stored in server-side circuit memory — never in browser storage |
| API calls | Typed service interfaces (`IRecipeApiService`, `IMealPlanApiService`) wrapping `HttpClient`; JWT injected via `JwtAuthorizationMessageHandler` DelegatingHandler |

---

## 2. Navigation Structure

```
/                        Landing page (unauthenticated users)
/auth/callback           OAuth callback handler (extracts token, sets circuit auth)
/catalog                 Browse public recipe catalog (authenticated)
/recipes                 My personal recipe collection (authenticated)
/recipes/new             Create new recipe form (authenticated)
/recipes/import          URL import flow (authenticated)
/recipes/{id}            Recipe detail view (authenticated)
/recipes/{id}/edit       Edit recipe form (authenticated, owner only)
/mealplans               Meal plan list (authenticated)
/mealplans/new           Create new meal plan (authenticated)
/mealplans/{id}          Meal plan week view (authenticated, owner only)
/admin/recipes           Admin recipe management (Admin role only)
```

All routes except `/` and `/auth/callback` require authentication. Unauthenticated access redirects to `/`.

---

## 3. Pages & Components

### 3.1 Landing Page (`/`)
**Purpose:** Entry point for unauthenticated users. Describes the application and provides login options.

**Content:**
- Application name and tagline
- Brief feature summary (3–4 bullet points)
- "Sign in with Google" button → `GET /api/v1/auth/login/google`
- "Sign in with Facebook" button → `GET /api/v1/auth/login/facebook`

**State:** No server-side state needed.  
**Auth:** Public (no redirect).

---

### 3.2 Auth Callback (`/auth/callback`)
**Purpose:** Handles the redirect from the API after successful OAuth. Extracts the JWT from the URL fragment and establishes the authenticated session.

**Behavior:**
1. On component mount, reads `#token=...&expires=...` from the URL fragment (JavaScript interop)
2. Stores the token in circuit-scoped state (via an injected `AuthStateService`)
3. Redirects to `/catalog`

**State:** Transient — only active during the redirect.

---

### 3.3 Recipe Catalog (`/catalog`)
**Purpose:** Browse and search public/catalog recipes.

**Layout:** Responsive card grid (3 columns desktop, 2 tablet, 1 mobile)

**Components:**
- `RecipeSearchBar` — text input + "Search" button, clears with ×; debounced (300ms)
- `CategoryFilter` — horizontal pill/chip row of all `RecipeCategory` values; "All" default; single selection
- `RecipeCard` — image (placeholder if no URL), title, category badge, prep+cook time, tag chips (max 3 shown), "View Recipe" link
- `PaginationBar` — previous/next + page numbers; shows total count

**Data flow:** On load and on any filter change, calls `GET /api/v1/recipes` with current filter state. Loading spinner shown during fetch.

**Empty state:** "No recipes found. Try a different search or category."

---

### 3.4 My Recipes (`/recipes`)
**Purpose:** View and manage the user's personal recipe collection.

**Layout:** Same responsive card grid as catalog, plus a prominent "Add Recipe" action area at the top.

**Actions bar:**
- "Add Recipe" button → `/recipes/new`
- "Import from URL" button → `/recipes/import`
- Search and category filter (same components as catalog)
- Visibility filter: "All", "Private", "Public"

**RecipeCard** (personal variant): same as catalog card but shows a "Private" badge for private recipes, plus an action menu (⋮) with: Edit, Delete, Make Public/Private toggle, Fork (if it's a catalog recipe the user forked — shows "View Original" link).

**Delete confirmation:** Inline confirmation (not a modal) — card flips/expands to show "Are you sure? This cannot be undone. [Cancel] [Delete]"

---

### 3.5 Recipe Detail (`/recipes/{id}`)
**Purpose:** Full recipe view with serving-size scaler.

**Layout:** Two-column (desktop): left column = image + metadata; right column = ingredients + steps. Single column on mobile.

**Left column:**
- Large image (or placeholder graphic)
- Title (h1)
- Category badge + tags
- Prep time / Cook time chips
- Owner display name + "Forked from: [original title]" link if applicable
- Visibility badge (shown to owner only)
- Nutrition panel (collapsible): calories/protein/carbs/fat per serving — updates when serving count changes
- Action buttons (owner/admin only): "Edit Recipe", "Delete"

**Right column:**
- **Serving size scaler:** label "Servings:", numeric `<input type="number" min="1">` pre-populated with `servingSize`. Changing the value triggers reactive ingredient quantity recalculation.
- **Ingredients list:** each item shows `[scaled quantity] [unit] [name], [notes]`. Quantity is displayed as a simplified fraction or decimal (e.g., `1 1/2 cups` or `1.5 cups` — configurable preference, V1 uses decimal).
- **Steps list:** ordered list, each step shows optional title (bold) + body text. Steps are numbered.
- "Fork this Recipe" button (shown for any public recipe the user does not own) → calls `POST /api/v1/recipes/{id}/fork`, then navigates to the new recipe's edit page.

---

### 3.6 Create / Edit Recipe Form (`/recipes/new`, `/recipes/{id}/edit`)
**Purpose:** Structured data entry form for creating or editing a recipe.

**The two routes use the same `RecipeForm` component** — in create mode, all fields are blank/default; in edit mode, fields are pre-populated.

**Form sections (accordion or tabs):**

#### Section 1: Basics
| Field | Control | Validation |
|---|---|---|
| Title | Text input | Required, max 200 |
| Description | Textarea | Optional, max 2000 |
| Image URL | Text input | Optional, must be valid URL |
| Category | Select dropdown | Required |
| Visibility | Radio: Private / Public | Required, default Private |
| Prep Time (minutes) | Number input | Optional, min 1 |
| Cook Time (minutes) | Number input | Optional, min 1 |
| Servings | Number input | Required, min 1 |

#### Section 2: Tags
- Tag input: type tag name + Enter or comma to add
- Renders as removable chips
- Tags are normalized to lowercase on save

#### Section 3: Nutrition (collapsible/optional)
| Field | Control |
|---|---|
| Calories per serving | Decimal number input |
| Protein (g) | Decimal number input |
| Carbs (g) | Decimal number input |
| Fat (g) | Decimal number input |

#### Section 4: Ingredients
- Dynamic list: each row has Quantity (number), Unit (text, with common unit suggestions), Name (text, required), Notes (text)
- "Add Ingredient" button appends a new row
- Drag handle to reorder (or up/down arrow buttons as fallback)
- "Remove" (×) button per row
- Minimum: 1 ingredient required to save

#### Section 5: Steps
- Dynamic list: each step has optional Title (text), Body (textarea, required)
- "Add Step" button appends a new step
- Drag handle to reorder (or up/down arrow buttons as fallback)
- "Remove" button per step
- Minimum: 1 step required to save

**Footer:** "Save Recipe" (primary) + "Cancel" (secondary, returns to previous page).

**Validation:** Inline field-level errors shown on blur. Full form validation on submit. Uses `EditForm` with `DataAnnotationsValidator` and `ValidationSummary`.

---

### 3.7 URL Import Flow (`/recipes/import`)
**Purpose:** Allow users to import a recipe from a URL.

**Step 1 — Enter URL:**
- Single text input for the URL
- "Import" button
- Calls `POST /api/v1/recipes/import` — shows a loading spinner
- On success: advance to Step 2
- On `422`: shows "This URL doesn't appear to contain a recipe. Please try a different link."
- On `400`: shows "Please enter a valid URL."

**Step 2 — Review & Edit:**
- The `RecipeForm` component is rendered pre-populated with the imported data
- A banner at the top reads: "Recipe imported from [domain]. Review the details below and save when ready."
- The source URL is shown (read-only) for reference
- User can edit any field before saving
- "Save Recipe" calls `POST /api/v1/recipes` as normal
- "Start Over" returns to Step 1

---

### 3.8 Meal Plan List (`/mealplans`)
**Purpose:** View and manage the user's meal plans.

**Layout:** List of meal plan cards sorted by `WeekStartDate` descending (most recent first).

Each **MealPlanCard** shows:
- Plan name (or "Week of [formatted date]" if no name)
- Date range: "Mon Jan 6 – Sun Jan 12, 2025"
- Number of meals planned out of 28 possible slots
- Action menu: View, Delete

"Create New Meal Plan" button at top.

**Delete confirmation:** Same inline confirmation pattern as recipe deletion.

---

### 3.9 Create Meal Plan (`/mealplans/new`)
**Purpose:** Create a new week-based meal plan.

**Fields:**
| Field | Control | Validation |
|---|---|---|
| Week starting | Date picker (Monday only — other days disabled) | Required |
| Plan name | Text input | Optional, max 100 chars |

"Create Plan" → calls `POST /api/v1/mealplans`, then navigates to `/mealplans/{id}`.

**Duplicate prevention:** If a plan already exists for the selected week, shows a warning: "You already have a plan for this week. [View existing plan]"

---

### 3.10 Meal Plan Week View (`/mealplans/{id}`)
**Purpose:** Assign recipes to meal slots for the week.

**Layout:** 7-column grid (Monday–Sunday), 4 rows (Breakfast, Lunch, Dinner, Snack).

Each **meal cell**:
- Empty: dashed border, "+ Add Recipe" button
- Filled: recipe image (small thumbnail), recipe title (truncated), serving count badge, remove (×) button

**"+ Add Recipe" flow:**
1. Opens a `RecipePickerModal` — a searchable list of the user's personal recipes and catalog recipes
2. User selects a recipe
3. A serving count input appears (pre-populated with the recipe's base serving size)
4. "Add to Plan" confirms and calls `PUT /api/v1/mealplans/{id}/entries`
5. Cell updates to show the selected recipe

**Remove:** clicking × on a filled cell calls `DELETE /api/v1/mealplans/{id}/entries` with the day/slot.

**Header:** Plan name (editable inline), date range, "Back to Meal Plans" link.

---

### 3.11 Admin Recipe Management (`/admin/recipes`)
**Purpose:** Admin view for managing all recipes in the system.

**Access control:** `[Authorize(Roles = "Admin")]` on the page. Non-admin users see a 403 page.

**Layout:** Table/list of all recipes (paginated), with columns: Title, Category, Owner, Visibility, Created, Actions.

**Actions per row:** View, Edit (opens the standard `RecipeForm` in edit mode), Delete.

**"New Catalog Recipe" button** → opens the standard `RecipeForm` in create mode with `Visibility = Public` pre-set and locked.

---

## 4. Shared Components

| Component | Purpose |
|---|---|
| `AppNavMenu` | Top navigation bar. Shows: Catalog, My Recipes, Meal Plans. Admin link visible to admin users only. User avatar + display name + logout in top-right. |
| `RecipeCard` | Card rendering of a recipe summary (shared between catalog and personal views with prop to show/hide action menu). |
| `RecipeSearchBar` | Search input with debounce. |
| `CategoryFilter` | Category pill filter bar. |
| `PaginationBar` | Page navigation. |
| `IngredientRow` | Single ingredient row in the recipe form. |
| `StepRow` | Single step row in the recipe form. |
| `RecipePickerModal` | Searchable recipe selection modal for meal plan assignment. |
| `ConfirmInline` | Inline expand-to-confirm pattern for destructive actions. |
| `LoadingSpinner` | Overlay spinner for async operations. |
| `ErrorMessage` | Standardized error display component. |

---

## 5. Serving Size Scaling — Component Behavior

The scaler is a controlled numeric input bound to a local `currentServings` integer. Ingredient quantities are computed as:

```
displayQuantity = (ingredient.Quantity / recipe.ServingSize) * currentServings
```

This computation is performed entirely in C# within the Blazor component — no JavaScript required. Null quantities (ingredients without a quantity) are displayed as-is without scaling.

Fractions display: V1 displays decimal values rounded to 2 decimal places (e.g., `0.67 cups`).

---

## 6. Authentication State in Blazor Server

- `AuthStateService` is registered as a **scoped service per SignalR circuit** (one instance per connected user).
- Implements `AuthenticationStateProvider` — registered as the provider in DI so `CascadingAuthenticationState` and `AuthorizeView` work automatically.
- Stores the JWT string and the parsed `ClaimsPrincipal` in server-side circuit memory. **Never stored in browser `localStorage` or `sessionStorage`** (XSS risk).
- Public interface:
  - `SetUser(string jwt)` — parses claims from the JWT, stores both, raises `AuthenticationStateChanged`
  - `ClearUser()` — resets to anonymous principal, raises `AuthenticationStateChanged`
  - `GetAuthenticationStateAsync()` — returns current `AuthenticationState` (required by the interface)
  - `GetJwt()` — returns the raw JWT string for use by `JwtAuthorizationMessageHandler`
- On login: `/auth/callback` page reads `#token=...` from the URL fragment via JS interop, then calls `AuthStateService.SetUser(token)`.
- On logout: AppNavMenu calls `AuthStateService.ClearUser()` then navigates to `/`.
- `JwtAuthorizationMessageHandler` (DelegatingHandler): reads JWT from `AuthStateService` and adds `Authorization: Bearer {token}` to all outgoing `HttpClient` requests.
- `CascadingAuthenticationState` at the root propagates state to all child components.

---

## 7. Error Handling

- API errors are caught in a global `HttpClient` handler that maps HTTP status codes to user-friendly messages.
- Unhandled exceptions in Blazor Server circuits are caught by the `ErrorBoundary` component and display a friendly error message without crashing the circuit.
- Network connectivity loss shows a reconnection banner (Blazor's built-in circuit reconnect UI).

---

## 8. Accessibility

- All images include `alt` text.
- Form inputs have associated `<label>` elements.
- Focus management is handled after modal open/close.
- Keyboard navigation is supported for all interactive elements.
- Color is not the sole means of conveying information (e.g., visibility status uses text + color).
