using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace RecipeBook.Blazor.Server.Tests;

/// <summary>
/// Test wrapper that provides CascadingAuthenticationState for components using AuthorizeView.
/// </summary>
public class AuthStateWrapper : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
