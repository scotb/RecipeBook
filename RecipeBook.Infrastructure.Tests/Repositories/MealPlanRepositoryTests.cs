using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Repositories;
using RecipeBook.Tests.Shared;
using Testcontainers.PostgreSql;

namespace RecipeBook.Infrastructure.Tests.Repositories;

public class MealPlanRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private RecipeBookDbContext _context = null!;
    private MealPlanRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<RecipeBookDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _context = new RecipeBookDbContext(options);
        await _context.Database.MigrateAsync();
        _sut = new MealPlanRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task SeedUserAsync(string id)
    {
        var user = new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpper(), Email = $"{id}@test.com", NormalizedEmail = $"{id}@test.com".ToUpper(), SecurityStamp = Guid.NewGuid().ToString(), CreatedAt = DateTimeOffset.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task<Guid> SeedRecipeAsync(string ownerId)
    {
        var recipe = RecipeFactory.CreateValid(ownerId: ownerId);
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        return recipe.Id;
    }

    [Fact]
    public async Task AddAsync_WithValidMealPlan_PersistsAndReturnsMealPlan()
    {
        await SeedUserAsync("user-1");
        var mealPlan = MealPlanFactory.CreateValid(userId: "user-1");

        var result = await _sut.AddAsync(mealPlan);

        _context.ChangeTracker.Clear();
        var persisted = await _context.MealPlans.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMealPlanExists_ReturnsMealPlanWithEntries()
    {
        await SeedUserAsync("user-1");
        var recipeId = await SeedRecipeAsync("user-1");
        var mealPlan = MealPlanFactory.CreateWithEntry(recipeId, userId: "user-1");
        await _sut.AddAsync(mealPlan);

        _context.ChangeTracker.Clear();
        var result = await _sut.GetByIdAsync(mealPlan.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(mealPlan.Id);
        result.Entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMealPlanDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsAllMealPlansForUser()
    {
        await SeedUserAsync("user-1");
        await SeedUserAsync("user-2");
        var monday1 = GetMonday(0);
        var monday2 = GetMonday(7);
        var plan1 = MealPlanFactory.CreateValid(userId: "user-1", weekStartDate: monday1);
        var plan2 = MealPlanFactory.CreateValid(userId: "user-1", weekStartDate: monday2);
        var otherPlan = MealPlanFactory.CreateValid(userId: "user-2", weekStartDate: monday1);
        await _sut.AddAsync(plan1);
        await _sut.AddAsync(plan2);
        await _sut.AddAsync(otherPlan);

        var result = await _sut.GetByUserAsync("user-1");

        result.Should().HaveCount(2);
        result.Select(p => p.Id).Should().BeEquivalentTo(new[] { plan1.Id, plan2.Id });
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedMealPlan_PersistsChanges()
    {
        await SeedUserAsync("user-1");
        var mealPlan = MealPlanFactory.CreateValid(userId: "user-1");
        await _sut.AddAsync(mealPlan);

        mealPlan.Rename("My Updated Plan");
        await _sut.UpdateAsync(mealPlan);

        _context.ChangeTracker.Clear();
        var persisted = await _context.MealPlans.FindAsync(mealPlan.Id);
        persisted!.Name.Should().Be("My Updated Plan");
    }

    [Fact]
    public async Task DeleteAsync_WhenMealPlanExists_RemovesMealPlanAndCascadesEntries()
    {
        await SeedUserAsync("user-1");
        var recipeId = await SeedRecipeAsync("user-1");
        var mealPlan = MealPlanFactory.CreateWithEntry(recipeId, userId: "user-1");
        await _sut.AddAsync(mealPlan);
        var entryId = mealPlan.Entries[0].Id;

        await _sut.DeleteAsync(mealPlan.Id);

        _context.ChangeTracker.Clear();
        var plan = await _context.MealPlans.FindAsync(mealPlan.Id);
        plan.Should().BeNull();
        var entry = await _context.Set<MealEntry>().FindAsync(entryId);
        entry.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRecipe_WhenMealEntryReferencesIt_ThrowsDbUpdateException()
    {
        await SeedUserAsync("user-1");
        var recipeId = await SeedRecipeAsync("user-1");
        var mealPlan = MealPlanFactory.CreateWithEntry(recipeId, userId: "user-1");
        await _sut.AddAsync(mealPlan);

        // Try to delete the recipe while a meal entry references it (Restrict FK)
        // Clear tracker first so EF Core doesn't intercept with in-memory orphan detection
        _context.ChangeTracker.Clear();
        var act = async () =>
        {
            var recipe = await _context.Recipes.FindAsync(recipeId);
            _context.Recipes.Remove(recipe!);
            await _context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>();
    }

    private static DateOnly GetMonday(int daysFromNow)
    {
        var d = DateOnly.FromDateTime(DateTime.Today.AddDays(daysFromNow));
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)d.DayOfWeek + 7) % 7;
        return d.AddDays(daysUntilMonday == 0 ? 0 : daysUntilMonday);
    }
}
