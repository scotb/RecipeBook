using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Repositories;
using RecipeBook.Infrastructure.Services;

namespace RecipeBook.Infrastructure;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Overload for tests and simple scenarios — defaults to SQLite.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Database:Provider"] = "SQLite",
            })!
            .Build();
        return services.AddInfrastructure(cfg);
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var connectionString = cfg.GetConnectionString("Default") ?? "";
        var provider = cfg["Database:Provider"] ?? cfg["Database__Provider"] ?? "PostgreSQL";

        if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<RecipeBookDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            services.AddDbContext<RecipeBookDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<RecipeBookDbContext>();

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        return services;
    }
}
