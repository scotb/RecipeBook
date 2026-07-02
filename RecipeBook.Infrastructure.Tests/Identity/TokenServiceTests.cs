using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Config;
using RecipeBook.Infrastructure.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecipeBook.Infrastructure.Tests.Identity;

public class TokenServiceTests
{
    private static Mock<IOptions<JwtConfig>> CreateJwtOptions(
        string issuer = "RecipeBook.Api",
        string audience = "RecipeBook.Client",
        string secretKey = "a-very-long-and-secure-secret-key-at-least-32-chars",
        int expiryMinutes = 60)
    {
        var mock = new Mock<IOptions<JwtConfig>>();
        mock.Setup(o => o.Value).Returns(new JwtConfig(issuer, audience, secretKey)
        {
            TokenLifetimeMinutes = expiryMinutes
        });
        return mock;
    }

    [Fact]
    public void GenerateToken_WithValidClaims_ReturnsValidJwt()
    {
        var jwtOptions = CreateJwtOptions();
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var userId = "user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var avatarUrl = "https://example.com/avatar.png";
        var roles = new[] { "User" };

        var token = tokenService.GenerateToken(userId, email, displayName, avatarUrl, roles);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithMissingAvatarUrl_ReturnsValidJwt()
    {
        var jwtOptions = CreateJwtOptions();
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var userId = "user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        string? avatarUrl = null;
        var roles = new[] { "User" };

        var token = tokenService.GenerateToken(userId, email, displayName, avatarUrl, roles);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithCorrectExpiration_HasValidExpClaim()
    {
        var jwtOptions = CreateJwtOptions(expiryMinutes: 60);
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var userId = "user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var avatarUrl = "https://example.com/avatar.png";
        var roles = new[] { "User" };

        var token = tokenService.GenerateToken(userId, email, displayName, avatarUrl, roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expClaim = jwtToken.Claims.First(c => c.Type == "exp").Value;
        var expUnixTime = long.Parse(expClaim);
        var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;

        expDateTime.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_WithCorrectIssuerAndAudience_HasCorrectClaims()
    {
        var issuer = "CustomIssuer";
        var audience = "CustomAudience";
        var jwtOptions = CreateJwtOptions(issuer: issuer, audience: audience);
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var userId = "user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var avatarUrl = "https://example.com/avatar.png";
        var roles = new[] { "User" };

        var token = tokenService.GenerateToken(userId, email, displayName, avatarUrl, roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(issuer);
        jwtToken.Audiences.Should().Contain(audience);
    }

    [Fact]
    public void GenerateToken_WithMultipleRoles_IncludesAllRoleClaims()
    {
        var jwtOptions = CreateJwtOptions();
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var userId = "user-123";
        var email = "test@example.com";
        var displayName = "Test User";
        var avatarUrl = "https://example.com/avatar.png";
        var roles = new[] { "User", "Admin" };

        var token = tokenService.GenerateToken(userId, email, displayName, avatarUrl, roles);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().HaveCount(2);
        roleClaims.Should().Contain("User");
        roleClaims.Should().Contain("Admin");
    }

    // FIX 4, Test 1: Token generated with a specific secret key should validate with that same key
    [Fact]
    public void GenerateToken_WithSpecificSecretKey_ValidatesWithSameKey()
    {
        var secretKey = "abcdefghijklmnopqrstuvwxyz012345";
        var jwtOptions = CreateJwtOptions(secretKey: secretKey);
        ITokenService tokenService = new TokenService(jwtOptions.Object);

        var token = tokenService.GenerateToken("user-1", "test@example.com", "Test User", null, Array.Empty<string>());

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
        principal.Should().NotBeNull();
        validatedToken.Should().NotBeNull();
    }

    // FIX 4, Test 2: Token generated with one key should NOT validate with a different key
    [Fact]
    public void GenerateToken_WithDifferentSecretKey_FailsValidation()
    {
        var secretKey1 = "abcdefghijklmnopqrstuvwxyz012345";
        var secretKey2 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ012345";
        
        var jwtOptions1 = CreateJwtOptions(secretKey: secretKey1);
        ITokenService tokenService = new TokenService(jwtOptions1.Object);

        var token = tokenService.GenerateToken("user-1", "test@example.com", "Test User", null, Array.Empty<string>());

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey2))
        };

        var exception = Record.Exception(() => handler.ValidateToken(token, validationParameters, out _));
        exception.Should().NotBeNull("Token signed with key1 should not validate against key2");
    }
}
