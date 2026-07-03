using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RecipeBook.Api.Tests.Integration;

public static class IntegrationTestAuthHelper
{
    public static string CreateToken(
        WebApplicationFactory<Program> factory,
        string userId,
        string email = "test@example.com",
        string[]? roles = null)
    {
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var issuer = configuration["Jwt:Issuer"] ?? "RecipeBook";
        var audience = configuration["Jwt:Audience"] ?? "RecipeBookAPI";
        var secretKey = configuration["Jwt:SecretKey"] ?? "dev-jwt-secret-key-change-me-12345";

        return CreateToken(userId, email, issuer, audience, secretKey, roles);
    }

    public static string CreateToken(
        string userId,
        string email = "test@example.com",
        string issuer = "RecipeBook",
        string audience = "RecipeBookAPI",
        string secretKey = "dev-jwt-secret-key-change-me-12345",
        string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, email.Split('@').First()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (roles != null)
        {
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
