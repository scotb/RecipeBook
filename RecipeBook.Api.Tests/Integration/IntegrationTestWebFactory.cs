using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Services;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Repositories;
using RecipeBook.Infrastructure.Services;
using RecipeBook.Api;

namespace RecipeBook.Api.Tests.Integration;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _sharedConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add in-memory config that overrides specific keys from appsettings.json.
        var testConfig = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "RecipeBook",
            ["Jwt:Audience"] = "RecipeBookAPI",
            ["Jwt:SecretKey"] = "dev-jwt-secret-key-change-me-12345",
            ["Jwt:TokenLifetimeMinutes"] = "60",
            ["Authentication:Google:ClientId"] = "test-client-id",
            ["Authentication:Google:ClientSecret"] = "test-client-secret",
            ["Authentication:Facebook:AppId"] = "test-app-id",
            ["Authentication:Facebook:AppSecret"] = "test-app-secret"
        };

        builder.ConfigureAppConfiguration((ctx, configBuilder) =>
        {
            // Add in-memory source last so it takes priority over appsettings.json.
            configBuilder.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Create a shared SQLite connection so all DbContext instances share the same DB.
            _sharedConnection = new SqliteConnection("DataSource=:memory:");
            _sharedConnection.Open();

            // Remove the existing DbContext registration (if any from Program.cs pipeline).
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RecipeBookDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Register in-memory SQLite sharing one connection across all scoped DB contexts.
            services.AddDbContext<RecipeBookDbContext>(options =>
                options.UseSqlite(_sharedConnection!));

            // Register Identity with EF stores pointing at our test DbContext.
            services.AddIdentityCore<ApplicationUser>()
                .AddEntityFrameworkStores<RecipeBookDbContext>();

            // Register MVC controllers so routes are discovered.
            services.AddControllers();

            // Register infrastructure repositories and services.
            services.AddScoped<IUserContext, UserControllerContext>();
            services.AddScoped<RecipeBook.Application.Interfaces.IRecipeRepository, RecipeRepository>();
            services.AddScoped<RecipeBook.Application.Interfaces.IMealPlanRepository, MealPlanRepository>();
            services.AddScoped<IUserLookupService, UserLookupService>();

            // Remove Google and Facebook auth handlers to avoid validation errors.
            var handlerDescriptors = services.Where(d =>
                d.ServiceType == typeof(IAuthenticationHandler) &&
                d.ImplementationType?.Name is not null &&
                (d.ImplementationType.Name.Contains("Google") || d.ImplementationType.Name.Contains("Facebook")))
                .ToList();
            foreach (var hd in handlerDescriptors)
                services.Remove(hd);

            // Register a default IRecipeImporter that throws — tests can replace it via DI.
            if (!services.Any(d => d.ServiceType == typeof(IRecipeImporter)))
            {
                var mockImporter = new Mock<IRecipeImporter>();
                mockImporter.Setup(i => i.ImportFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RecipeBook.Application.Exceptions.InvalidImportException("No default importer configured for tests."));
                services.AddScoped(sp => mockImporter.Object);
            }
        });

        builder.Configure(app =>
        {
            // Program.cs already sets up routing, auth, and controller mapping.
            // No additional configuration needed for integration tests.
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seeds the in-memory DB by ensuring tables are created.
    /// </summary>
    public async Task SeedDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipeBookDbContext>();
        await db.Database.EnsureCreatedAsync();
        // Disable FK checks in SQLite for tests — allows creating recipes
        // with ownerIds that don't have corresponding ApplicationUser rows.
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
    }

    /// <summary>
    /// Resets the database state between tests by dropping and recreating all tables.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipeBookDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
    }

    /// <summary>
    /// Creates a scoped service provider for seeding test data.
    /// </summary>
    public IServiceScope CreateTestScope() => Services.CreateScope();

    /// <summary>
    /// Resolves a service from the DI container within a new scope.
    /// </summary>
    public async Task<T> ResolveScopedAsync<T>(Func<IServiceProvider, T> factory)
    {
        using var scope = Services.CreateScope();
        var service = factory(scope.ServiceProvider);
        return await (service switch
        {
            IAsyncDisposable ad => throw new InvalidOperationException("Use CreateTestScope for async disposal."),
            _ => Task.FromResult(service)
        });
    }

    /// <summary>
    /// Creates a test client with optional auth token.
    /// </summary>
    public HttpClient CreateClientWithToken(string? token = null)
    {
        var client = base.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Disposes the shared SQLite connection.
    /// </summary>
    public void DisposeSharedConnection()
    {
        _sharedConnection?.Dispose();
    }
}
