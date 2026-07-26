using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Service for managing JWT authentication state in the Blazor circuit.
/// </summary>
public interface IAuthStateService
{
    Task SetUser(string jwt);
    void ClearUser();
    string? GetJwt();
    string? GetUserId();
}
