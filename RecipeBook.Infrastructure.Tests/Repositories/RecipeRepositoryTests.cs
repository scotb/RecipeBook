using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Identity;
using RecipeBook.Infrastructure.Persistence;
using RecipeBook.Infrastructure.Repositories;
using RecipeBook.Tests.Shared;
using Testcontainers.PostgreSql;

namespace RecipeBook.Infrastructure.Tests.Repositories;

public class RecipeRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private RecipeBookDbContext _context = null!;
    private RecipeRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<RecipeBookDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        _context = new RecipeBookDbContext(options);
        await _context.Database.MigrateAsync();
        _sut = new RecipeRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<ApplicationUser> SeedUserAsync(string id = "user-1")
    {
        var user = new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpper(), Email = $"{id}@test.com", NormalizedEmail = $"{id}@test.com".ToUpper(), SecurityStamp = Guid.NewGuid().ToString(), CreatedAt = DateTimeOffset.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task AddAsync_WithValidRecipe_PersistsAndReturnsRecipe()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");

        var result = await _sut.AddAsync(recipe);

        result.Id.Should().Be(recipe.Id);
        var persisted = await _context.Recipes.FindAsync(recipe.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be(recipe.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeExists_ReturnsRecipeWithAllChildren()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");
        recipe.AddIngredient("Flour", 2m, "cups");
        recipe.AddStep("Mix the flour with water.");
        recipe.AddTag("quick");
        await _sut.AddAsync(recipe);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(recipe.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(recipe.Id);
        result.Ingredients.Should().HaveCount(1);
        result.Steps.Should().HaveCount(1);
        result.Tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenRecipeExists_ReturnsTrue()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");
        await _sut.AddAsync(recipe);

        var result = await _sut.ExistsAsync(recipe.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenRecipeDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.ExistsAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedRecipe_PersistsChanges()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");
        await _sut.AddAsync(recipe);
        _context.ChangeTracker.Clear();

        recipe.Update("Updated Title", recipe.ServingSize, recipe.Category);
        await _sut.UpdateAsync(recipe);
        _context.ChangeTracker.Clear();

        var persisted = await _sut.GetByIdAsync(recipe.Id);
        persisted!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteAsync_WhenRecipeExists_RemovesRecipeAndCascadesChildren()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");
        recipe.AddIngredient("Flour", 2m, "cups");
        recipe.AddTag("quick");
        await _sut.AddAsync(recipe);
        _context.ChangeTracker.Clear();

        await _sut.DeleteAsync(recipe.Id);

        (await _sut.ExistsAsync(recipe.Id)).Should().BeFalse();
        _context.Set<Ingredient>().Any(i => i.RecipeId == recipe.Id).Should().BeFalse();
        _context.Set<RecipeTag>().Any(t => t.RecipeId == recipe.Id).Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdsAsync_WithValidIds_ReturnsMatchingRecipes()
    {
        await SeedUserAsync("user-1");
        var r1 = RecipeFactory.CreateValid(ownerId: "user-1");
        var r2 = RecipeFactory.CreateValid(ownerId: "user-1");
        var r3 = RecipeFactory.CreateValid(ownerId: "user-1");
        await _sut.AddAsync(r1);
        await _sut.AddAsync(r2);
        await _sut.AddAsync(r3);

        var result = await _sut.GetByIdsAsync([r1.Id, r3.Id]);

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(new[] { r1.Id, r3.Id });
    }

    [Fact]
    public async Task GetByIdsAsync_WithEmptyList_ReturnsEmptyList()
    {
        var result = await _sut.GetByIdsAsync([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicAsync_WithNoFilters_ReturnsOnlyPublicRecipes()
    {
        await SeedUserAsync("user-1");
        var publicRecipe = RecipeFactory.CreatePublic(ownerId: "user-1");
        var privateRecipe = RecipeFactory.CreateValid(ownerId: "user-1"); // Private by default
        await _sut.AddAsync(publicRecipe);
        await _sut.AddAsync(privateRecipe);

        var result = await _sut.GetPublicAsync(new Application.Models.RecipeQuery());

        result.Items.Should().ContainSingle(r => r.Id == publicRecipe.Id);
        result.Items.Should().NotContain(r => r.Id == privateRecipe.Id);
    }

    [Fact]
    public async Task GetPublicAsync_WithCategoryFilter_ReturnsOnlyMatchingCategory()
    {
        await SeedUserAsync("user-1");
        var dinner = RecipeFactory.CreatePublic(ownerId: "user-1", category: Domain.Enums.RecipeCategory.Dinner);
        var breakfast = RecipeFactory.CreatePublic(ownerId: "user-1", category: Domain.Enums.RecipeCategory.Breakfast);
        await _sut.AddAsync(dinner);
        await _sut.AddAsync(breakfast);

        var result = await _sut.GetPublicAsync(new Application.Models.RecipeQuery(Category: Domain.Enums.RecipeCategory.Dinner));

        result.Items.Should().ContainSingle(r => r.Id == dinner.Id);
        result.Items.Should().NotContain(r => r.Id == breakfast.Id);
    }

    [Fact]
    public async Task GetPublicAsync_WithTagFilter_ReturnsOnlyRecipesWithAllTags()
    {
        await SeedUserAsync("user-1");
        var r1 = RecipeFactory.CreatePublic(ownerId: "user-1");
        r1.AddTag("chicken");
        r1.AddTag("quick");
        var r2 = RecipeFactory.CreatePublic(ownerId: "user-1");
        r2.AddTag("chicken");
        var r3 = RecipeFactory.CreatePublic(ownerId: "user-1");
        r3.AddTag("quick");
        await _sut.AddAsync(r1);
        await _sut.AddAsync(r2);
        await _sut.AddAsync(r3);

        var result = await _sut.GetPublicAsync(new Application.Models.RecipeQuery(Tags: ["chicken", "quick"]));

        result.Items.Should().ContainSingle(r => r.Id == r1.Id);
        result.Items.Should().NotContain(r => r.Id == r2.Id);
        result.Items.Should().NotContain(r => r.Id == r3.Id);
    }

    [Fact]
    public async Task GetPublicAsync_WithSearchText_ReturnsOnlyMatchingTitleOrDescription()
    {
        await SeedUserAsync("user-1");
        var match = new Recipe("Chicken Tikka Masala", "user-1", 4, Domain.Enums.RecipeCategory.Dinner);
        match.SubmitForReview();
        match.Approve();
        var noMatch = new Recipe("Beef Stew", "user-1", 4, Domain.Enums.RecipeCategory.Dinner);
        noMatch.SubmitForReview();
        noMatch.Approve();
        await _sut.AddAsync(match);
        await _sut.AddAsync(noMatch);

        var result = await _sut.GetPublicAsync(new Application.Models.RecipeQuery(SearchText: "Tikka"));

        result.Items.Should().ContainSingle(r => r.Id == match.Id);
        result.Items.Should().NotContain(r => r.Id == noMatch.Id);
    }

    [Fact]
    public async Task GetPublicAsync_ReturnsPaginatedResults()
    {
        await SeedUserAsync("user-1");
        for (var i = 0; i < 5; i++)
        {
            var recipe = RecipeFactory.CreatePublic(ownerId: "user-1");
            await _sut.AddAsync(recipe);
        }

        var page1 = await _sut.GetPublicAsync(new Application.Models.RecipeQuery(Page: 1, PageSize: 3));
        var page2 = await _sut.GetPublicAsync(new Application.Models.RecipeQuery(Page: 2, PageSize: 3));

        page1.TotalCount.Should().Be(5);
        page1.Items.Should().HaveCount(3);
        page2.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsAllRecipesForOwnerRegardlessOfVisibility()
    {
        await SeedUserAsync("user-1");
        await SeedUserAsync("user-2");
        var publicRecipe = RecipeFactory.CreatePublic(ownerId: "user-1");
        var privateRecipe = RecipeFactory.CreateValid(ownerId: "user-1");
        var otherOwner = RecipeFactory.CreatePublic(ownerId: "user-2");
        await _sut.AddAsync(publicRecipe);
        await _sut.AddAsync(privateRecipe);
        await _sut.AddAsync(otherOwner);

        var result = await _sut.GetByOwnerAsync("user-1", new Application.Models.RecipeQuery());

        result.Items.Should().HaveCount(2);
        result.Items.Select(r => r.Id).Should().BeEquivalentTo(new[] { publicRecipe.Id, privateRecipe.Id });
    }

    [Fact]
    public async Task GetByOwnerAsync_WithCategoryFilter_AppliesFilterWithinOwnerScope()
    {
        await SeedUserAsync("user-1");
        var dinner = RecipeFactory.CreateValid(ownerId: "user-1", category: Domain.Enums.RecipeCategory.Dinner);
        var breakfast = RecipeFactory.CreateValid(ownerId: "user-1", category: Domain.Enums.RecipeCategory.Breakfast);
        await _sut.AddAsync(dinner);
        await _sut.AddAsync(breakfast);

        var result = await _sut.GetByOwnerAsync("user-1", new Application.Models.RecipeQuery(Category: Domain.Enums.RecipeCategory.Dinner));

        result.Items.Should().ContainSingle(r => r.Id == dinner.Id);
        result.Items.Should().NotContain(r => r.Id == breakfast.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecipesRegardlessOfVisibilityOrOwner()
    {
        await SeedUserAsync("user-1");
        await SeedUserAsync("user-2");
        var publicRecipe = RecipeFactory.CreatePublic(ownerId: "user-1");
        var privateRecipe = RecipeFactory.CreateValid(ownerId: "user-1");
        var otherOwner = RecipeFactory.CreateValid(ownerId: "user-2");
        await _sut.AddAsync(publicRecipe);
        await _sut.AddAsync(privateRecipe);
        await _sut.AddAsync(otherOwner);

        var result = await _sut.GetAllAsync(new Application.Models.AdminRecipeQuery());

        result.TotalCount.Should().Be(3);
        result.Items.Select(r => r.Id).Should().BeEquivalentTo(new[] { publicRecipe.Id, privateRecipe.Id, otherOwner.Id });
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerIdFilter_ReturnsOnlyThatOwner()
    {
        await SeedUserAsync("user-1");
        await SeedUserAsync("user-2");
        var r1 = RecipeFactory.CreateValid(ownerId: "user-1");
        var r2 = RecipeFactory.CreateValid(ownerId: "user-2");
        await _sut.AddAsync(r1);
        await _sut.AddAsync(r2);

        var result = await _sut.GetAllAsync(new Application.Models.AdminRecipeQuery(OwnerId: "user-1"));

        result.Items.Should().ContainSingle(r => r.Id == r1.Id);
        result.Items.Should().NotContain(r => r.Id == r2.Id);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateTagName_ThrowsDbUpdateException()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreateValid(ownerId: "user-1");
        recipe.AddTag("chicken");
        await _sut.AddAsync(recipe);

        // Insert directly via SQL to bypass domain duplicate-check
        var dupId = Guid.NewGuid();
        var act = async () => await _context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO recipe_tags (\"Id\", \"RecipeId\", \"Name\") VALUES ({dupId}, {recipe.Id}, {"chicken"})");

        await act.Should().ThrowAsync<Npgsql.NpgsqlException>();
    }

    [Fact]
    public async Task GetPublicAsync_ReturnsRecipesWithTagsLoaded()
    {
        await SeedUserAsync("user-1");
        var recipe = RecipeFactory.CreatePublic(ownerId: "user-1");
        recipe.AddTag("chicken");
        await _sut.AddAsync(recipe);

        _context.ChangeTracker.Clear();
        var result = await _sut.GetPublicAsync(new Application.Models.RecipeQuery());

        result.Items.Should().ContainSingle();
        result.Items[0].Tags.Should().ContainSingle(t => t.Name == "chicken");
    }
}
