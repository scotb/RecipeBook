using FluentAssertions;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;
using RecipeBook.Application.Services;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Application.Tests.Services;

public class RecipeServiceTests
{
    private readonly Mock<IRecipeRepository> _repoMock = new();
    private readonly Mock<IUserLookupService> _userLookupMock = new();
    private readonly Mock<IRecipeImporter> _importerMock = new();
    private readonly RecipeService _sut;

    public RecipeServiceTests()
    {
        _sut = new RecipeService(_repoMock.Object, _userLookupMock.Object, _importerMock.Object);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsRecipeDto()
    {
        var request = new CreateRecipeRequest
        {
            Title = "Pancakes",
            ServingSize = 4,
            Category = RecipeCategory.Breakfast
        };
        const string ownerId = "user-1";
        const string displayName = "Alice";

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        _userLookupMock
            .Setup(u => u.GetDisplayNameAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(displayName);

        var result = await _sut.CreateAsync(request, ownerId);

        result.Title.Should().Be("Pancakes");
        result.OwnerId.Should().Be(ownerId);
        result.OwnerDisplayName.Should().Be(displayName);
        result.ServingSize.Should().Be(4);
        result.Category.Should().Be(RecipeCategory.Breakfast);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithIngredientsAndSteps_AddsThem()
    {
        var request = new CreateRecipeRequest
        {
            Title = "Pasta",
            ServingSize = 2,
            Category = RecipeCategory.Dinner,
            Ingredients = [new CreateIngredientRequest(200m, "g", "Pasta", null)],
            Steps = [new CreateRecipeStepRequest(null, "Boil water"), new CreateRecipeStepRequest("Step 2", "Cook pasta")]
        };

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        _userLookupMock
            .Setup(u => u.GetDisplayNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bob");

        var result = await _sut.CreateAsync(request, "user-2");

        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Name.Should().Be("Pasta");
        result.Steps.Should().HaveCount(2);
        result.Steps[0].Body.Should().Be("Boil water");
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenRecipeIsPublic_ReturnsDto()
    {
        var recipe = new Recipe("Soup", "owner-1", 2, RecipeCategory.Lunch,
            visibility: RecipeVisibility.Public);
        var id = recipe.Id;

        _repoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync("Charlie");

        var result = await _sut.GetByIdAsync(id, requestingUserId: "any-user");

        result.Id.Should().Be(id);
        result.Title.Should().Be("Soup");
        result.OwnerDisplayName.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecipeNotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Recipe?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid(), "user-x");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPrivateAndNotOwner_ThrowsForbiddenException()
    {
        var recipe = new Recipe("Private", "owner-1", 1, RecipeCategory.Dinner,
            visibility: RecipeVisibility.Private);

        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var act = () => _sut.GetByIdAsync(recipe.Id, requestingUserId: "other-user");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPrivateAndAdmin_ReturnsDto()
    {
        var recipe = new Recipe("Private", "owner-1", 1, RecipeCategory.Dinner,
            visibility: RecipeVisibility.Private);

        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync("Owner");

        var result = await _sut.GetByIdAsync(recipe.Id, requestingUserId: "admin-user", isAdmin: true);

        result.Id.Should().Be(recipe.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_WhenOwnerUpdates_ReturnsUpdatedDto()
    {
        var recipe = new Recipe("Old Title", "owner-1", 1, RecipeCategory.Lunch);

        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync("Owner");

        var request = new UpdateRecipeRequest { Title = "New Title", ServingSize = 3, Category = RecipeCategory.Dinner };

        var result = await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "owner-1");

        result.Title.Should().Be("New Title");
        result.ServingSize.Should().Be(3);
        _repoMock.Verify(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var recipe = new Recipe("Title", "owner-1", 1, RecipeCategory.Lunch);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var request = new UpdateRecipeRequest { Title = "X", ServingSize = 1, Category = RecipeCategory.Lunch };

        var act = () => _sut.UpdateAsync(recipe.Id, request, requestingUserId: "other-user");

        await act.Should().ThrowAsync<ForbiddenException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminAndNotOwner_Succeeds()
    {
        var recipe = new Recipe("Title", "owner-1", 1, RecipeCategory.Lunch);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("owner-1", It.IsAny<CancellationToken>())).ReturnsAsync("Owner");

        var request = new UpdateRecipeRequest { Title = "Admin Edit", ServingSize = 1, Category = RecipeCategory.Lunch };

        var result = await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "admin-user", isAdmin: true);

        result.Title.Should().Be("Admin Edit");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenOwner_DeletesRecipe()
    {
        var recipe = new Recipe("Soup", "owner-1", 1, RecipeCategory.Lunch);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        await _sut.DeleteAsync(recipe.Id, requestingUserId: "owner-1");

        _repoMock.Verify(r => r.DeleteAsync(recipe.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsForbiddenException()
    {
        var recipe = new Recipe("Soup", "owner-1", 1, RecipeCategory.Lunch);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var act = () => _sut.DeleteAsync(recipe.Id, requestingUserId: "other-user");

        await act.Should().ThrowAsync<ForbiddenException>();
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenAdminAndNotOwner_DeletesRecipe()
    {
        var recipe = new Recipe("Soup", "owner-1", 1, RecipeCategory.Lunch);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        await _sut.DeleteAsync(recipe.Id, requestingUserId: "admin-user", isAdmin: true);

        _repoMock.Verify(r => r.DeleteAsync(recipe.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ForkAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForkAsync_WhenSourceExists_ReturnsFork()
    {
        var source = new Recipe("Original", "owner-1", 2, RecipeCategory.Dessert,
            visibility: RecipeVisibility.Public);

        _repoMock.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe r, CancellationToken _) => r);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("fork-user", It.IsAny<CancellationToken>())).ReturnsAsync("Forker");

        var result = await _sut.ForkAsync(source.Id, newOwnerId: "fork-user");

        result.Title.Should().Be("Original");
        result.OwnerId.Should().Be("fork-user");
        result.SourceRecipeId.Should().Be(source.Id);
        result.Visibility.Should().Be(RecipeVisibility.Private);
    }

    [Fact]
    public async Task ForkAsync_WhenSourceNotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Recipe?)null);

        var act = () => _sut.ForkAsync(Guid.NewGuid(), "fork-user");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ForkAsync_WhenUserAlreadyForkedSourceRecipe_ThrowsConflictException()
    {
        var src = new Recipe("Original", "owner-1", 2, RecipeCategory.Dessert,
            visibility: RecipeVisibility.Public);
        _repoMock.Setup(r => r.GetByIdAsync(src.Id, It.IsAny<CancellationToken>())).ReturnsAsync(src);
        _repoMock.Setup(r => r.HasForkAsync(src.Id, "fork-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var act = () => _sut.ForkAsync(src.Id, newOwnerId: "fork-user");
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── GetPublicRecipesAsync / GetMyRecipesAsync ─────────────────────────────

    [Fact]
    public async Task GetPublicRecipesAsync_ReturnsMappedSummaries()
    {
        var recipe = new Recipe("Tacos", "owner-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Public);
        var paged = new PagedResult<Recipe>([recipe], TotalCount: 1, Page: 1, PageSize: 20);
        var query = new RecipeQuery();

        _repoMock.Setup(r => r.GetPublicAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        _userLookupMock
            .Setup(u => u.GetDisplayNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["owner-1"] = "Alice" });

        var result = await _sut.GetPublicRecipesAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Tacos");
        result.Items[0].OwnerDisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetMyRecipesAsync_ReturnsMappedSummaries()
    {
        var recipe = new Recipe("Waffles", "owner-2", 1, RecipeCategory.Breakfast);
        var paged = new PagedResult<Recipe>([recipe], TotalCount: 1, Page: 1, PageSize: 20);
        var query = new RecipeQuery();

        _repoMock.Setup(r => r.GetByOwnerAsync("owner-2", query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        _userLookupMock
            .Setup(u => u.GetDisplayNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["owner-2"] = "Bob" });

        var result = await _sut.GetMyRecipesAsync("owner-2", query);

        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Waffles");
        result.Items[0].OwnerDisplayName.Should().Be("Bob");
    }

    // ── GetAllRecipesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRecipesAsync_ReturnsMappedSummaries()
    {
        var recipe = new Recipe("Secret", "owner-3", 1, RecipeCategory.Snack);
        var paged = new PagedResult<Recipe>([recipe], TotalCount: 1, Page: 1, PageSize: 20);
        var query = new AdminRecipeQuery();

        _repoMock.Setup(r => r.GetAllAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        _userLookupMock
            .Setup(u => u.GetDisplayNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["owner-3"] = "Carol" });

        var result = await _sut.GetAllRecipesAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Secret");
        result.Items[0].OwnerDisplayName.Should().Be("Carol");
    }

    // ── ImportAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_WhenImporterSucceeds_ReturnsResult()
    {
        var request = new ImportRecipeRequest("https://example.com/recipe");
        var parsed = new CreateRecipeRequest { Title = "Imported", ServingSize = 2, Category = RecipeCategory.Dinner };

        _importerMock
            .Setup(i => i.ImportFromUrlAsync(request.Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsed);

        var result = await _sut.ImportAsync(request);

        result.SourceUrl.Should().Be(request.Url);
        result.Recipe.Title.Should().Be("Imported");
    }

    [Fact]
    public async Task ImportAsync_WhenImporterThrows_Propagates()
    {
        var request = new ImportRecipeRequest("https://example.com/bad");

        _importerMock
            .Setup(i => i.ImportFromUrlAsync(request.Url, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidImportException("No recipe data found."));

        var act = () => _sut.ImportAsync(request);

        await act.Should().ThrowAsync<InvalidImportException>();
    }

    // ── UpdateAsync (tags / ingredients / steps / visibility) ─────────────────

    [Fact]
    public async Task UpdateAsync_ReplacesTagsFromRequest()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner);
        recipe.AddTag("OldTag");
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Tags = ["NewTag1", "NewTag2"]
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Tags.Select(t => t.Name).Should().BeEquivalentTo(["newtag1", "newtag2"]);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesIngredientsFromRequest()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner);
        recipe.AddIngredient("OldIng", 100m, "g");
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Ingredients = [new CreateIngredientRequest(200m, "ml", "NewIng", null)]
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Ingredients.Should().HaveCount(1);
        recipe.Ingredients[0].Name.Should().Be("NewIng");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesStepsFromRequest()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner);
        recipe.AddStep("Old step");
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Steps = [new CreateRecipeStepRequest(null, "New step")]
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Steps.Should().HaveCount(1);
        recipe.Steps[0].Body.Should().Be("New step");
    }

    [Fact]
    public async Task UpdateAsync_WhenVisibilityPrivate_CallsMakePrivate()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.PendingReview);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Visibility = RecipeVisibility.Private
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Visibility.Should().Be(RecipeVisibility.Private);
    }

    [Fact]
    public async Task UpdateAsync_WhenVisibilityPendingReview_CallsSubmitForReview()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.Private);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Visibility = RecipeVisibility.PendingReview
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Visibility.Should().Be(RecipeVisibility.PendingReview);
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnerSetsVisibilityPublic_ThrowsForbiddenException()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Visibility = RecipeVisibility.Public
        };

        var act = () => _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1", isAdmin: false);

        await act.Should().ThrowAsync<ForbiddenException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminSetsVisibilityPublic_ApprovesRecipe()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.PendingReview);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        var request = new UpdateRecipeRequest
        {
            Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner,
            Visibility = RecipeVisibility.Public
        };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "admin-user", isAdmin: true);

        recipe.Visibility.Should().Be(RecipeVisibility.Public);
    }

    [Fact]
    public async Task UpdateAsync_WhenVisibilityNotSpecified_DoesNotChangeExistingVisibility()
    {
        var recipe = new Recipe("Pasta", "user-1", 2, RecipeCategory.Dinner, visibility: RecipeVisibility.PendingReview);
        _repoMock.Setup(r => r.GetByIdAsync(recipe.Id, It.IsAny<CancellationToken>())).ReturnsAsync(recipe);
        _repoMock.Setup(r => r.UpdateAsync(recipe, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userLookupMock.Setup(u => u.GetDisplayNameAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync("User");

        // No Visibility set — omitted from request
        var request = new UpdateRecipeRequest { Title = "Pasta", ServingSize = 2, Category = RecipeCategory.Dinner };

        await _sut.UpdateAsync(recipe.Id, request, requestingUserId: "user-1");

        recipe.Visibility.Should().Be(RecipeVisibility.PendingReview);
    }

}
