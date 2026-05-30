# PRD 03 — API Specification

## 1. Overview

| Attribute | Value |
|---|---|
| Project | `RecipeBook.Api` |
| Framework | ASP.NET Core 10 Web API |
| Base URL (local) | `https://localhost:7100` |
| Base URL (prod) | `https://api.recipebook.example.com` |
| Auth | JWT Bearer (`Authorization: Bearer <token>`) |
| Content type | `application/json` |
| API versioning | URL prefix: `/api/v1/` |
| Pagination | Query params: `page` (default 1), `pageSize` (default 20, max 100) |
| Error format | RFC 7807 `ProblemDetails` |

All authenticated endpoints return `401 Unauthorized` if no valid JWT is present.  
All ownership-protected endpoints return `403 Forbidden` if the authenticated user does not own the resource.

---

## 2. Authentication Endpoints

### `GET /api/v1/auth/login/google`
Initiates the Google OAuth flow. Redirects the browser to Google's authorization endpoint.  
**Auth:** None  
**Response:** `302 Redirect` to Google

---

### `GET /api/v1/auth/login/facebook`
Initiates the Facebook OAuth flow.  
**Auth:** None  
**Response:** `302 Redirect` to Facebook

---

### `GET /api/v1/auth/callback`
OAuth callback handler. Called by the OAuth provider after user consent.  
Creates or updates the local `ApplicationUser`, then issues a JWT.  
**Auth:** None (handled internally by ASP.NET Core Identity + OAuth middleware)  
**Response:** `302 Redirect` to the UI with the JWT in the URL fragment:  
`https://<ui-host>/auth/callback#token=<jwt>&expires=<unix-timestamp>`

---

### `POST /api/v1/auth/logout`
Client-side logout — no server session to invalidate (JWT is stateless).  
**Auth:** Bearer JWT  
**Response:** `204 No Content`

---

### `GET /api/v1/auth/me`
Returns the current user's profile information.  
**Auth:** Bearer JWT  
**Response:** `200 OK`
```json
{
  "userId": "string",
  "displayName": "string",
  "email": "string",
  "avatarUrl": "string | null",
  "roles": ["User"],
  "createdAt": "2025-01-01T00:00:00Z"
}
```

---

## 3. Recipe Endpoints

### `GET /api/v1/recipes`
Browse public (catalog) recipes. Accessible to all authenticated users.  
**Auth:** Bearer JWT  
**Query parameters:**

| Param | Type | Description |
|---|---|---|
| `search` | `string?` | Full-text search on title and description |
| `category` | `string?` | Filter by `RecipeCategory` enum name |
| `tags` | `string[]?` | Filter by one or more tag names (comma-separated) |
| `page` | `int` | Default: 1 |
| `pageSize` | `int` | Default: 20, max 100 |

**Response:** `200 OK`
```json
{
  "items": [ <RecipeSummaryDto> ],
  "totalCount": 120,
  "page": 1,
  "pageSize": 20
}
```

---

### `GET /api/v1/recipes/my`
List the current user's personal recipes (all visibility levels).  
**Auth:** Bearer JWT  
**Query parameters:** Same as above.  
**Response:** `200 OK` — same paged structure as above.

---

### `GET /api/v1/recipes/{id}`
Get full recipe detail.  
**Auth:** Bearer JWT  
**Rules:** Returns `404 Not Found` if the recipe does not exist. Returns `403 Forbidden` if the recipe is Private and the caller is not the owner.  
**Response:** `200 OK`
```json
{
  "id": "uuid",
  "title": "string",
  "description": "string | null",
  "imageUrl": "string | null",
  "prepTimeMinutes": 15,
  "cookTimeMinutes": 30,
  "servingSize": 4,
  "category": "Dinner",
  "visibility": "Public",
  "ownerId": "string",
  "ownerDisplayName": "string",
  "sourceRecipeId": "uuid | null",
  "caloriesPerServing": 350.0,
  "proteinGrams": 28.5,
  "carbsGrams": 42.0,
  "fatGrams": 8.0,
  "tags": ["quick", "low-carb"],
  "ingredients": [
    {
      "id": "uuid",
      "sortOrder": 0,
      "quantity": 2.0,
      "unit": "cups",
      "name": "all-purpose flour",
      "notes": "sifted"
    }
  ],
  "steps": [
    {
      "id": "uuid",
      "sortOrder": 0,
      "title": "Prepare the dough",
      "body": "Combine flour and water in a large bowl..."
    }
  ],
  "createdAt": "2025-01-01T00:00:00Z",
  "updatedAt": "2025-06-01T00:00:00Z"
}
```

---

### `POST /api/v1/recipes`
Create a new recipe manually.  
**Auth:** Bearer JWT  
**Request body:** `CreateRecipeRequest`
```json
{
  "title": "string (required, max 200)",
  "description": "string | null (max 2000)",
  "imageUrl": "string | null",
  "prepTimeMinutes": "int | null",
  "cookTimeMinutes": "int | null",
  "servingSize": "int (required, min 1)",
  "category": "RecipeCategory (required)",
  "visibility": "Private | Public",
  "caloriesPerServing": "decimal | null",
  "proteinGrams": "decimal | null",
  "carbsGrams": "decimal | null",
  "fatGrams": "decimal | null",
  "tags": ["string"],
  "ingredients": [
    {
      "quantity": "decimal | null",
      "unit": "string | null",
      "name": "string (required)",
      "notes": "string | null"
    }
  ],
  "steps": [
    {
      "title": "string | null",
      "body": "string (required)"
    }
  ]
}
```
**Response:** `201 Created` with `Location` header → `/api/v1/recipes/{id}` and the full `RecipeDto` body.

---

### `PUT /api/v1/recipes/{id}`
Update an existing recipe.  
**Auth:** Bearer JWT  
**Rules:** Only the owner can update. `Admin` can update any recipe.  
**Request body:** `UpdateRecipeRequest` — same shape as `CreateRecipeRequest`. Full replacement of ingredients/steps/tags (not patch).  
**Response:** `200 OK` with updated `RecipeDto`.

---

### `DELETE /api/v1/recipes/{id}`
Delete a recipe.  
**Auth:** Bearer JWT  
**Rules:** Only the owner can delete their recipe. `Admin` can delete any recipe.  
**Response:** `204 No Content`

---

### `POST /api/v1/recipes/{id}/fork`
Fork a public or catalog recipe into the caller's personal collection.  
**Auth:** Bearer JWT  
**Rules:** Source recipe must be `Public`. Returns `409 Conflict` if the user has already forked this specific recipe.  
**Request body:** None  
**Response:** `201 Created` with `Location` header and full `RecipeDto` of the new forked recipe.

---

### `POST /api/v1/recipes/import`
Import a recipe from a URL by fetching and parsing `schema.org/Recipe` JSON-LD.  
**Auth:** Bearer JWT  
**Request body:**
```json
{
  "url": "https://www.example.com/some-recipe"
}
```
**Response:** `200 OK` — returns a `CreateRecipeRequest`-shaped preview object populated from the parsed data. The recipe is **not saved** at this point. The client presents it in the edit form for review, then calls `POST /api/v1/recipes` to save.
```json
{
  "sourceUrl": "https://www.example.com/some-recipe",
  "recipe": { /* CreateRecipeRequest shape */ }
}
```
**Error responses:**
- `422 Unprocessable Entity` if the URL does not contain parseable `schema.org/Recipe` data.
- `400 Bad Request` if the URL is malformed.

---

## 4. Meal Plan Endpoints

### `GET /api/v1/mealplans`
List all meal plans belonging to the current user.  
**Auth:** Bearer JWT  
**Response:** `200 OK`
```json
[
  {
    "id": "uuid",
    "name": "string | null",
    "weekStartDate": "2025-01-06",
    "createdAt": "2025-01-01T00:00:00Z",
    "entryCount": 12
  }
]
```

---

### `GET /api/v1/mealplans/{id}`
Get full meal plan detail including all entries.  
**Auth:** Bearer JWT  
**Rules:** `403 Forbidden` if not the owner.  
**Response:** `200 OK`
```json
{
  "id": "uuid",
  "name": "string | null",
  "weekStartDate": "2025-01-06",
  "createdAt": "2025-01-01T00:00:00Z",
  "entries": [
    {
      "id": "uuid",
      "dayOfWeek": "Monday",
      "mealSlot": "Dinner",
      "servingCount": 4,
      "recipe": {
        "id": "uuid",
        "title": "string",
        "imageUrl": "string | null",
        "category": "Dinner",
        "servingSize": 4
      }
    }
  ]
}
```

---

### `POST /api/v1/mealplans`
Create a new meal plan.  
**Auth:** Bearer JWT  
**Request body:**
```json
{
  "weekStartDate": "2025-01-06 (must be a Monday)",
  "name": "string | null"
}
```
**Response:** `201 Created` with full `MealPlanDto`.

---

### `PUT /api/v1/mealplans/{id}`
Update meal plan metadata (name only — entries are managed separately).  
**Auth:** Bearer JWT  
**Request body:**
```json
{
  "name": "string | null"
}
```
**Response:** `200 OK`

---

### `DELETE /api/v1/mealplans/{id}`
Delete a meal plan and all its entries.  
**Auth:** Bearer JWT  
**Response:** `204 No Content`

---

### `PUT /api/v1/mealplans/{id}/entries`
Set or update a meal entry (assign a recipe to a day/slot).  
**Auth:** Bearer JWT  
**Request body:**
```json
{
  "dayOfWeek": "Monday",
  "mealSlot": "Dinner",
  "recipeId": "uuid (required)",
  "servingCount": 4
}
```
**Response:** `200 OK` with the updated `MealEntryDto`.

---

### `DELETE /api/v1/mealplans/{id}/entries`
Clear a meal slot (remove the recipe assignment).  
**Auth:** Bearer JWT  
**Request body:**
```json
{
  "dayOfWeek": "Monday",
  "mealSlot": "Dinner"
}
```
**Response:** `204 No Content`

---

## 5. Admin Endpoints

All admin endpoints require the `Admin` role. Return `403 Forbidden` for non-admin users.

### `GET /api/v1/admin/recipes`
List all recipes across all users.  
**Auth:** Bearer JWT + `Admin` role  
**Query parameters:** `page`, `pageSize`, `search`, `ownerId?`  
**Response:** `200 OK` — paged `RecipeSummaryDto` list with owner information.

---

### `DELETE /api/v1/admin/recipes/{id}`
Delete any recipe regardless of owner.  
**Auth:** Bearer JWT + `Admin` role  
**Response:** `204 No Content`

---

## 6. DTO Definitions

### `RecipeSummaryDto`
```json
{
  "id": "uuid",
  "title": "string",
  "description": "string | null",
  "imageUrl": "string | null",
  "category": "Dinner",
  "visibility": "Public",
  "servingSize": 4,
  "prepTimeMinutes": 15,
  "cookTimeMinutes": 30,
  "tags": ["string"],
  "ownerId": "string",
  "ownerDisplayName": "string",
  "createdAt": "2025-01-01T00:00:00Z"
}
```

---

## 7. Error Responses

All errors follow RFC 7807 `ProblemDetails`:
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "title": ["Title is required."],
    "servingSize": ["Serving size must be at least 1."]
  }
}
```

| HTTP Status | Meaning |
|---|---|
| `200 OK` | Successful read or update |
| `201 Created` | Successful creation |
| `204 No Content` | Successful delete or action with no body |
| `400 Bad Request` | Malformed request (invalid JSON, bad URL format) |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Authenticated but not authorized (wrong owner, wrong role) |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Business rule conflict (e.g., duplicate fork, duplicate meal plan week) |
| `422 Unprocessable Entity` | Validation failure (model validation or domain rule) |
| `500 Internal Server Error` | Unhandled exception |

---

## 8. OpenAPI

The API exposes an OpenAPI (Swagger) document at `/openapi/v1.json` in development mode.  
Swagger UI is available at `/swagger` in development only — **not** in production.

---

## 9. API Versioning

- URL path versioning: `/api/v1/`.
- When a breaking change is needed, `/api/v2/` is introduced. V1 continues to function until explicitly deprecated.
- Version is not required in headers.

---

## 10. Rate Limiting

Not implemented in V1. Noted as a V2 infrastructure concern.

---

## 11. CORS

CORS policy is configured to allow:
- Blazor Server: `https://localhost:7200` (dev), production domain
- Angular stub: `https://localhost:4200` (dev)
- React stub: `https://localhost:3000` (dev)

CORS origins are configured via `appsettings.json` `AllowedOrigins` array. Production origins use environment variables.
