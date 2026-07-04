using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// Scoped service that stores JWT auth state in server-side circuit memory.
/// Never persists to browser storage.
/// </summary>
public class AuthStateService : AuthenticationStateProvider, IAuthStateService
{
    private string? _jwt;
    private ClaimsPrincipal _cachedPrincipal = Anonymous;
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public async Task SetUser(string jwt)
    {
        _jwt = jwt;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(jwt);

        var claims = jwtToken.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();

        // Map 'sub' claim to NameIdentifier if not already present
        var hasNameIdentifier = claims.Any(c => c.Type == ClaimTypes.NameIdentifier);
        if (!hasNameIdentifier && jwtToken.Subject != null)
        {
            claims.Insert(0, new Claim(ClaimTypes.NameIdentifier, jwtToken.Subject));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        _cachedPrincipal = principal;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public void ClearUser()
    {
        _jwt = null;
        _cachedPrincipal = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    public string? GetJwt() => _jwt;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_cachedPrincipal));
    }
}
