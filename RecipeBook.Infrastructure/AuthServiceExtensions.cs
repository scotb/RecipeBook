using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Identity;
using System.Text;

namespace RecipeBook.Infrastructure;

public static class AuthServiceExtensions
{
    public static AuthenticationBuilder AddAuthServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddSingleton<ITokenService, TokenService>();
        var jwtSection = cfg.GetSection("Jwt");
        var builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!))
                };
            });
        return builder;
    }
}
