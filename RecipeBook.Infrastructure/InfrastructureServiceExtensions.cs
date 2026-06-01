using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Repositories;
using RecipeBook.Infrastructure.Services;

namespace RecipeBook.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RecipeBookDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<RecipeBookDbContext>();

        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        return services;
    }
}
