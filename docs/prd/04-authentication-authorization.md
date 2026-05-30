# PRD 04 — Authentication & Authorization

## 1. Principles

- **No local passwords.** Users authenticate exclusively via external OAuth providers (Google, Facebook). The application never stores or manages passwords.
- **ASP.NET Core Identity** manages the local user record, roles, and claims. It delegates credential verification entirely to OAuth providers.
- **JWT Bearer tokens** secure all API endpoints. All UI stacks (Blazor, Angular, React, WPF) authenticate using the same token mechanism.
- **Roles** are simple: `User` (default for all registered users) and `Admin` (manually seeded).

---

## 2. OAuth Providers

| Provider | Scopes Requested | Data Captured |
|---|---|---|
| Google | `openid`, `profile`, `email` | Email, display name, avatar URL |
| Facebook | `email`, `public_profile` | Email, display name, avatar URL |

Providers are registered in `RecipeBook.Api` via ASP.NET Core's `AddAuthentication()` extension methods. Client IDs and secrets are stored exclusively in environment variables or Azure Key Vault — never in source-controlled `appsettings.json`.

Required environment variables:
```
Authentication__Google__ClientId
Authentication__Google__ClientSecret
Authentication__Facebook__AppId
Authentication__Facebook__AppSecret
Jwt__SecretKey           (min 256-bit / 32-char random string)
Jwt__Issuer
Jwt__Audience
Jwt__ExpiryMinutes       (default: 60)
```

---

## 3. Authentication Flow

### 3.1 Step-by-Step (Browser/SPA Client)

```
1. User clicks "Sign in with Google" on UI
2. UI navigates to: GET /api/v1/auth/login/google
3. API redirects browser to Google's authorization endpoint
4. User consents on Google's page
5. Google redirects to: GET /api/v1/auth/callback?code=...&state=...
6. API:
   a. Exchanges code for tokens with Google
   b. Reads user profile (email, name, picture)
   c. Finds or creates ApplicationUser in Identity
   d. Assigns "User" role if first login
   e. Issues a JWT (see §3.2)
   f. Redirects UI to: https://<ui-host>/auth/callback#token=<jwt>&expires=<unix-ts>
7. UI extracts JWT from URL fragment (not querystring — never sent to server)
8. UI stores JWT in memory (not localStorage — see §5 Security Notes)
9. UI includes JWT in Authorization header for all API calls
```

### 3.2 JWT Claims

| Claim | Value |
|---|---|
| `sub` | `ApplicationUser.Id` (ASP.NET Identity GUID as string) |
| `email` | User's email address |
| `name` | `ApplicationUser.DisplayName` |
| `picture` | `ApplicationUser.AvatarUrl` |
| `roles` | Array of role names: `["User"]` or `["User","Admin"]` |
| `iss` | Value of `Jwt__Issuer` config |
| `aud` | Value of `Jwt__Audience` config |
| `iat` | Issued at (Unix timestamp) |
| `exp` | Expiry (Unix timestamp) |
| `jti` | Unique token ID (GUID) |

JWT is signed using HMAC-SHA256 with the `Jwt__SecretKey`.

### 3.3 Token Refresh

V1 uses single-use JWTs with a configurable expiry (default 60 minutes). When the token expires, the user must re-authenticate via OAuth. Silent re-auth (via OAuth `prompt=none`) is a V2 concern.

### 3.4 Blazor Server Specifics

Blazor Server runs server-side. It can use ASP.NET Core cookie authentication directly without storing tokens in the browser. The recommended flow for Blazor Server:

1. User initiates login — navigates to `/api/v1/auth/login/google`
2. After OAuth callback, the API issues an encrypted cookie in addition to the JWT
3. Blazor Server reads the cookie to establish the authenticated `ClaimsPrincipal`
4. When Blazor Server calls the API on behalf of the user, it uses the JWT (stored server-side in the user's circuit state)

The cookie is:
- `HttpOnly: true`
- `Secure: true`
- `SameSite: Strict`
- Encrypted using ASP.NET Core Data Protection

---

## 4. ASP.NET Core Identity Configuration

```csharp
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    // No password options needed — no local accounts
})
.AddEntityFrameworkStores<RecipeBookDbContext>()
.AddDefaultTokenProviders();
```

**External login flow:**  
`UserManager.FindByEmailAsync()` → if not found, `UserManager.CreateAsync()` → `UserManager.AddToRoleAsync("User")` → `UserManager.AddLoginAsync()` (links the OAuth provider record)

**No local account fallback.** If `UserManager.FindByLoginAsync()` fails and no matching email exists, a new user is created. Users cannot log in with a password under any circumstance.

---

## 5. Authorization

### 5.1 Roles

| Role | Assignment | Capabilities |
|---|---|---|
| `User` | Assigned automatically on first OAuth login | Browse catalog, manage personal recipes and meal plans |
| `Admin` | Assigned manually via seed/migration | All `User` capabilities + create/edit/delete catalog (public) recipes + access admin endpoints |

Admin role seeding: a configurable list of email addresses is read at startup from `appsettings.json` (`Admin__SeedEmails`). If a user with a matching email exists, they are added to the `Admin` role. This runs idempotently at startup.

### 5.2 Authorization Policies

Defined in `RecipeBook.Api` at startup:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser",  policy => policy.RequireRole("User"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RecipeOwnerOrAdmin", policy =>
        policy.Requirements.Add(new ResourceOwnerOrAdminRequirement()));
});
```

**`ResourceOwnerOrAdminRequirement`**  
A custom `IAuthorizationRequirement` + `AuthorizationHandler<T, Recipe>`.  
Passes if:
- The authenticated user's `sub` claim matches `Recipe.OwnerId`, **or**
- The authenticated user has the `Admin` role.

An equivalent handler exists for `MealPlan`.

### 5.3 Controller Attribute Usage

```csharp
[Authorize(Policy = "RequireUser")]         // All recipe CRUD endpoints
[Authorize(Policy = "RequireAdmin")]        // Admin endpoints
// Resource-level check via IAuthorizationService injected in the handler
```

---

## 6. Security Notes

| Concern | Approach |
|---|---|
| JWT storage | Blazor Server: server-side circuit state. SPA: in-memory JS variable (not `localStorage`). `localStorage` is vulnerable to XSS. |
| CSRF | Not applicable to JWT Bearer (stateless). Cookie auth uses `SameSite: Strict` to prevent CSRF. |
| OAuth state parameter | ASP.NET Core handles state validation automatically to prevent CSRF on the OAuth callback. |
| Secrets | Never in source control. Use environment variables in dev; Azure Key Vault in production. |
| HTTPS | Enforced in production. `UseHttpsRedirection()` is enabled. |
| JWT secret rotation | V2 concern. V1 uses a single signing key. |
| Scope of JWT | JWT is audience-restricted to `Jwt__Audience`. Tokens for one deployment cannot be used against another. |

---

## 7. Account Linking

If a user signs in with Google and later signs in with Facebook using the same email address, the accounts are linked automatically to the same `ApplicationUser` via `UserManager.AddLoginAsync()`. The user ends up with one profile accessible from both providers.

---

## 8. Test Considerations

- Authentication middleware is bypassed in unit tests via mocked `ClaimsPrincipal`.
- API integration tests (WebApplicationFactory) use a test JWT signed with a test key.
- A helper `TestAuthHandler` is registered in the test WebApplicationFactory to inject a configurable test user without hitting OAuth providers.
- Admin role tests inject a principal with both `User` and `Admin` roles.
