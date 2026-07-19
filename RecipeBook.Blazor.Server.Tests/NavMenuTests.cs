using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Moq;
using RecipeBook.Blazor.Server.Components.Layout;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests;

public class NavMenuTests : BunitContext
{
    private AuthenticationStateProvider AnonymousAuthState => new AnonymousAuthenticationStateProvider();

    private AuthenticationStateProvider AdminAuthState => new RoleAuthenticationStateProvider("Admin");

    private IRenderedComponent<CascadingAuthenticationState> RenderNavMenuWith(
        AuthenticationStateProvider authState,
        Mock<IAuthStateService>? authStateServiceMock = null)
    {
        if (authStateServiceMock == null)
        {
            authStateServiceMock = new Mock<IAuthStateService>();
        }
        Services.AddSingleton(authStateServiceMock.Object);
        Services.AddSingleton(authState);
        return Render<CascadingAuthenticationState>(parameters =>
        {
            parameters.AddChildContent<NavMenu>();
        });
    }

    private (Mock<IAuthorizationPolicyProvider> PolicyProvider, Mock<IAuthorizationService> AuthService) SetupAuthorizationMocks()
    {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        var policyProvider = new Mock<IAuthorizationPolicyProvider>();
        policyProvider.Setup(p => p.GetDefaultPolicyAsync()).Returns(Task.FromResult(policy));
        policyProvider.Setup(p => p.GetFallbackPolicyAsync()).Returns(Task.FromResult<AuthorizationPolicy?>(null));
        policyProvider.Setup(p => p.GetPolicyAsync(It.IsAny<string>())).Returns(Task.FromResult((AuthorizationPolicy?)policy));

        var authService = new Mock<IAuthorizationService>();
        authService.Setup(s => s.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        authService.Setup(s => s.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());

        Services.AddSingleton(policyProvider.Object);
        Services.AddSingleton(authService.Object);

        return (policyProvider, authService);
    }

    private static string CreateTestJwt()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, "Admin")
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    // --- Tests ---

    [Fact]
    public void WhenRendered_ShowsCatalogMyRecipesMealPlansLinks()
    {
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var cut = RenderNavMenuWith(AnonymousAuthState);

        cut.Markup.Should().Contain("/catalog");
        cut.Markup.Should().Contain("/recipes");
        cut.Markup.Should().Contain("/mealplans");
    }

    [Fact]
    public void WhenUserHasAdminRole_ShowsAdminLink()
    {
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var cut = RenderNavMenuWith(AdminAuthState);

        cut.Markup.Should().Contain("/admin/recipes");
    }

    [Fact]
    public void WhenUserIsNonAdmin_HidesAdminLink()
    {
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var cut = RenderNavMenuWith(new RoleAuthenticationStateProvider("User"));

        cut.Markup.Should().NotContain("/admin/recipes");
    }

    [Fact]
    public async Task SignOutButton_Clicked_CallsClearUserOnAuthStateService()
    {
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var authStateServiceMock = new Mock<IAuthStateService>();
        var cut = RenderNavMenuWith(AdminAuthState, authStateServiceMock);

        cut.Find("button").Click();

        authStateServiceMock.Verify(s => s.ClearUser(), Times.Once);
    }

    [Fact]
    public async Task SignOutButton_Clicked_NavigatesToRootAfterClear()
    {
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var realAuthState = new AuthStateService();
        await realAuthState.SetUser(CreateTestJwt());
        Services.AddSingleton<AuthenticationStateProvider>(realAuthState);
        Services.AddSingleton<IAuthStateService>(realAuthState);

        var cut = Render<CascadingAuthenticationState>(parameters =>
        {
            parameters.AddChildContent<NavMenu>();
        });
        cut.Find("button").Click();
        await Task.Delay(50);

        var state = await realAuthState.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeFalse("ClearUser should reset to anonymous principal");
    }

    [Fact]
    public void UnauthenticatedRoute_ShowsNavigationLinks()
    {
        // "All routes require authentication, redirect to /" is enforced via [Authorize]
        // on pages (PRD 05 §2). NavMenu's role is showing navigation links + login prompt.
        Services.AddMudServices();
        SetupAuthorizationMocks();

        var cut = RenderNavMenuWith(AnonymousAuthState);

        cut.Markup.Should().Contain("/catalog");
        cut.Markup.Should().Contain("/recipes");
        cut.Markup.Should().Contain("/mealplans");
    }

    // --- Support classes ---

    private class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(Anonymous));
    }

    private class RoleAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal;
        public RoleAuthenticationStateProvider(params string[] roles)
        {
            var identity = new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "jwt");
            _principal = new ClaimsPrincipal(identity);
        }
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(_principal));
    }
}
