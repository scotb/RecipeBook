using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Services;
using Testcontainers.PostgreSql;

namespace RecipeBook.Infrastructure.Tests.Services;

public class UserLookupServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private RecipeBookDbContext _context = null!;
    private UserLookupService _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<RecipeBookDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _context = new RecipeBookDbContext(options);
        await _context.Database.MigrateAsync();
        var userManager = CreateUserManager(_context);
        _sut = new UserLookupService(userManager);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static UserManager<ApplicationUser> CreateUserManager(RecipeBookDbContext context)
    {
        var store = new UserStore<ApplicationUser>(context);
        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new Logger<UserManager<ApplicationUser>>(new LoggerFactory()));
    }

    private async Task SeedUserAsync(string id, string? displayName = null)
    {
        var user = new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpper(), Email = $"{id}@test.com", NormalizedEmail = $"{id}@test.com".ToUpper(), SecurityStamp = Guid.NewGuid().ToString(), CreatedAt = DateTimeOffset.UtcNow, DisplayName = displayName };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDisplayNameAsync_WhenUserExists_ReturnsDisplayName()
    {
        await SeedUserAsync("user-1", displayName: "Alice");

        var result = await _sut.GetDisplayNameAsync("user-1");

        result.Should().Be("Alice");
    }

    [Fact]
    public async Task GetDisplayNameAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetDisplayNameAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDisplayNameAsync_WhenDisplayNameIsNull_ReturnsNull()
    {
        await SeedUserAsync("user-1", displayName: null);

        var result = await _sut.GetDisplayNameAsync("user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDisplayNamesAsync_WithMultipleUserIds_ReturnsDictionary()
    {
        await SeedUserAsync("user-1", displayName: "Alice");
        await SeedUserAsync("user-2", displayName: "Bob");

        var result = await _sut.GetDisplayNamesAsync(["user-1", "user-2"]);

        result.Should().HaveCount(2);
        result["user-1"].Should().Be("Alice");
        result["user-2"].Should().Be("Bob");
    }

    [Fact]
    public async Task GetDisplayNamesAsync_WithUnknownIds_OmitsThemFromResult()
    {
        await SeedUserAsync("user-1", displayName: "Alice");

        var result = await _sut.GetDisplayNamesAsync(["user-1", "nonexistent"]);

        result.Should().HaveCount(1);
        result.ContainsKey("user-1").Should().BeTrue();
        result.ContainsKey("nonexistent").Should().BeFalse();
    }
}
