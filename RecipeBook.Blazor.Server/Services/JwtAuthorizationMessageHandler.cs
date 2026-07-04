using Microsoft.AspNetCore.Components.Authorization;

namespace RecipeBook.Blazor.Server.Services;

/// <summary>
/// DelegatingHandler that injects the JWT Bearer token from AuthStateService into outgoing requests.
/// </summary>
public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IAuthStateService _authStateService;

    public JwtAuthorizationMessageHandler(IAuthStateService authStateService)
    {
        _authStateService = authStateService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var jwt = _authStateService.GetJwt();
        if (!string.IsNullOrWhiteSpace(jwt))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        }

        return await base.SendAsync(request, ct);
    }
}
