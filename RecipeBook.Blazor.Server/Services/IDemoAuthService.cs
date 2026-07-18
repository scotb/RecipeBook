namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Calls the dev-only demo auth endpoint to obtain a JWT.
/// Only active in Development environment (API returns 404 otherwise).
/// </summary>
public interface IDemoAuthService
{
    Task<string> LoginAsDemoAsync();
}
