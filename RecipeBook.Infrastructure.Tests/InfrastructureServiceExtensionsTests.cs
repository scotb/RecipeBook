using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Persistence;

namespace RecipeBook.Infrastructure.Tests;

public class InfrastructureServiceExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure("Host=localhost;Database=test;Username=test;Password=test");

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<RecipeBookDbContext>().Should().NotBeNull();
        provider.GetRequiredService<IRecipeRepository>().Should().NotBeNull();
        provider.GetRequiredService<IMealPlanRepository>().Should().NotBeNull();
        provider.GetRequiredService<IUserLookupService>().Should().NotBeNull();
    }
}
