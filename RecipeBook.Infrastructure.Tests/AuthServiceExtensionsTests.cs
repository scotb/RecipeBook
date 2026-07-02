using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Config;
using RecipeBook.Infrastructure.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace RecipeBook.Infrastructure.Tests;

public class AuthServiceExtensionsTests
{
    private static IServiceCollection SetupServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "RecipeBook",
            ["Jwt:Audience"] = "RecipeBookAPI",
            ["Jwt:SecretKey"] = "dev-jwt-secret-key-change-me-12345"
        };
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)!
            .Build();
        services.AddSingleton<IConfiguration>(cfg);
        return services;
    }

    // FIX 3, Test 1: AddAuthServices should accept IConfiguration as parameter
    [Fact]
    public void AddAuthServices_AcceptsIConfiguration()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        
        // This should compile — the method signature accepts IConfiguration
        var result = services.AddAuthServices(cfg);
        result.Should().NotBeNull();
        result.Should().NotBeNull();
    }

    // FIX 3, Test 2: JWT validation params should be read from the provided IConfiguration
    [Fact]
    public void AddAuthServices_JwtValidationUsesConfigValues()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        services.AddAuthServices(cfg);
        using var provider = services.BuildServiceProvider();
        var jwtOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var bearerOptions = jwtOptionsMonitor.Get("Bearer");
        var tvp = bearerOptions!.TokenValidationParameters;
        tvp.ValidIssuer.Should().Be("RecipeBook");
        tvp.ValidAudience.Should().Be("RecipeBookAPI");
    }

    // FIX 3, Test 3: ITokenService should be registered as Singleton
    [Fact]
    public void AddAuthServices_RegistersITokenServiceAsSingleton()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        services.AddAuthServices(cfg);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITokenService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    // FIX 3, Test 4: JWT Bearer scheme named "Bearer" with correct validation flags
    [Fact]
    public void AddAuthServices_JwtBearerSchemeNamedBearer_WithCorrectValidationFlags()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        services.AddAuthServices(cfg);
        using var provider = services.BuildServiceProvider();
        var jwtOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var bearerOptions = jwtOptionsMonitor.Get("Bearer");
        bearerOptions.Should().NotBeNull();
        var tvp = bearerOptions!.TokenValidationParameters;
        tvp.ValidateIssuer.Should().BeTrue();
        tvp.ValidateAudience.Should().BeTrue();
        tvp.ValidateLifetime.Should().BeTrue();
        tvp.ValidateIssuerSigningKey.Should().BeTrue();
    }

    [Fact]
    public void AddAuthServices_ReturnsAuthenticationBuilder()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        var result = services.AddAuthServices(cfg);
        result.Should().BeOfType<AuthenticationBuilder>();
    }

    [Fact]
    public void AddAuthServices_AllowsChainingForGoogleAuth()
    {
        var services = SetupServices();
        var cfg = services.BuildServiceProvider().GetService<IConfiguration>()!;
        var authBuilder = services.AddAuthServices(cfg);
        authBuilder.AddGoogle(google =>
        {
            google.ClientId = "test-client-id";
            google.ClientSecret = "test-client-secret";
        });
        using var provider = services.BuildServiceProvider();
        var jwtOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var bearerOptions = jwtOptionsMonitor.Get("Bearer");
        bearerOptions.Should().NotBeNull();
    }
}
