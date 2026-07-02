using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Services;

namespace RecipeBook.Application.Tests;

public class ApplicationServiceExtensionsTests
{
    [Fact]
    public void AddApplicationServices_RegistersIRecipeServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        var descriptor = services.Single(d => d.ServiceType == typeof(IRecipeService));
        descriptor.ImplementationType.Should().Be(typeof(RecipeBook.Application.Services.RecipeService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_RegistersIMealPlanServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        var descriptor = services.Single(d => d.ServiceType == typeof(IMealPlanService));
        descriptor.ImplementationType.Should().Be(typeof(MealPlanService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
