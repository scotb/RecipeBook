using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RecipeBook.Api.Tests.Integration;

public class IntegrationTestAuthHelperTests
{
    [Fact]
    public void CreateToken_ReturnsValidJwt()
    {
        // Arrange
        var factory = new IntegrationTestWebFactory();
        var userId = "test-user-123";
        var email = "user@example.com";

        // Act
        var token = IntegrationTestAuthHelper.CreateToken(factory, userId, email);

        // Assert — should be a well-formed JWT with three parts.
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        // Decode and verify claims.
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "RecipeBook",
            ValidateAudience = true,
            ValidAudience = "RecipeBookAPI",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("dev-jwt-secret-key-change-me-12345"))
        }, out var validatedToken);

        validatedToken.Should().NotBeNull();
        var jwtToken = (JwtSecurityToken)validatedToken;

        // Verify the name claim exists in the raw token.
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name);
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(email.Split('@').First());

        var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        principal.Identity.Should().BeOfType<ClaimsIdentity>();

        var subClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Value.Should().Be(userId);
    }

    [Fact]
    public void CreateToken_WithRoles_IncludesRoleClaims()
    {
        // Arrange
        var roles = new[] { "Admin", "Editor" };

        // Act
        var token = IntegrationTestAuthHelper.CreateToken("user-1", "u@ex.com", roles: roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).OrderBy(x => x).ToList();
        roleClaims.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public void CreateToken_WithDefaultParams_GeneratesTokenWithExpectedIssuer()
    {
        // Arrange & Act
        var token = IntegrationTestAuthHelper.CreateToken("u1");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("RecipeBook");
        jwt.Audiences.Should().ContainSingle("RecipeBookAPI");
    }
}
