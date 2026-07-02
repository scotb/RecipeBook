using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Services;

namespace RecipeBook.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IMealPlanService, MealPlanService>();
        return services;
    }
}
