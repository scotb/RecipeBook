using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Components.Authorization;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class AuthStateServiceTests
{
    private static string CreateTestJwt(string sub = "user-123", string email = "test@example.com", string name = "Test User")
    {
        var key = new byte[32]; // dummy key, not validated by AuthStateService
        var tokenHandler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, sub),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = null // no signature validation in AuthStateService
        };
        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task SetUser_WhenCalled_StoresClaimsAndRaisesEvent()
    {
        // Arrange
        var service = new AuthStateService();
        var jwt = CreateTestJwt();
        bool eventRaised = false;
        service.AuthenticationStateChanged += _ => { eventRaised = true; };

        // Act
        await service.SetUser(jwt);

        // Assert
        eventRaised.Should().BeTrue("AuthenticationStateChanged should be raised when user is set");
        var state = await service.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task SetUser_WhenCalled_ExtractsSubAndEmailClaims()
    {
        // Arrange
        var service = new AuthStateService();
        var jwt = CreateTestJwt(sub: "sub-456", email: "user@example.com");

        // Act
        await service.SetUser(jwt);

        // Assert
        var state = await service.GetAuthenticationStateAsync();
        var identity = state.User.Identity as ClaimsIdentity;
        identity!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("sub-456");
        identity.FindFirst(ClaimTypes.Email)?.Value.Should().Be("user@example.com");
    }

    [Fact]
    public async Task ClearUser_WhenCalled_ResetsToAnonymousPrincipal()
    {
        // Arrange
        var service = new AuthStateService();
        await service.SetUser(CreateTestJwt());

        // Act
        service.ClearUser();

        // Assert
        var state = await service.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetJwt_WhenCalledAfterSetUser_ReturnsRawTokenString()
    {
        // Arrange
        var service = new AuthStateService();
        var jwt = CreateTestJwt();

        // Act
        await service.SetUser(jwt);

        // Assert
        service.GetJwt().Should().Be(jwt);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenNoUser_ReturnsAnonymousState()
    {
        // Arrange
        var service = new AuthStateService();

        // Act
        var state = await service.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenCalledMultipleTimes_ReturnsSameCachedPrincipal()
    {
        // Arrange
        var service = new AuthStateService();
        await service.SetUser(CreateTestJwt());

        // Act
        var state1 = await service.GetAuthenticationStateAsync();
        var state2 = await service.GetAuthenticationStateAsync();

        // Assert — same cached object returned both times
        ReferenceEquals(state1.User, state2.User).Should().BeTrue("GetAuthenticationStateAsync should return the cached principal, not re-parse");
    }
}
