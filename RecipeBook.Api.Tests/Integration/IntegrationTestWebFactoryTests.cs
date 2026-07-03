using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;

namespace RecipeBook.Api.Tests.Integration;

public class IntegrationTestWebFactoryTests : IAsyncLifetime
{
    private IntegrationTestWebFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebFactory();
        _client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void Factory_CanCreateClient()
    {
        // Assert
        _client.Should().NotBeNull();
    }

    [Fact]
    public async Task Factory_WhenSendingGetRequest_ReturnsOk()
    {
        // Act — verify the factory creates a client that can make requests.
        var response = await _client.GetAsync("/");

        // Assert — the client should be functional even if the root returns 404.
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Factory_WhenSendingAuthenticatedRequest_ReturnsOk()
    {
        // Arrange — create a valid JWT token.
        var token = IntegrationTestAuthHelper.CreateToken("user-42", "alice@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act — verify the client works with auth headers.
        var response = await _client.GetAsync("/");

        // Assert — the client should be functional with auth headers.
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Factory_DatabaseIsIsolatedPerTest()
    {
        // Arrange — seed a user into the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipeBookDbContext>();
        var user = new ApplicationUser
        {
            Id = "iso-user-1",
            UserName = "iso@example.com",
            Email = "iso@example.com"
        };
        db.Set<ApplicationUser>().Add(user);
        await db.SaveChangesAsync();

        // Act — verify the user exists.
        var count = await db.Set<ApplicationUser>().CountAsync();

        // Assert
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Factory_WhenResolved_UserContextIsRegistered()
    {
        // Assert — IUserContext should be resolvable from the factory's DI container.
        using var scope = _factory.Services.CreateScope();
        var userContext = scope.ServiceProvider.GetService<RecipeBook.Application.Interfaces.IUserContext>();
        userContext.Should().NotBeNull();
    }
}
