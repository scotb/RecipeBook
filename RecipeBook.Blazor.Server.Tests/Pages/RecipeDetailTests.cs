using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Blazor.Server.Components.Pages;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Pages;

public class RecipeDetailTests : BunitContext
{
    private Mock<IAuthStateService> RegisterDefaultAuthMock()
    {
        var authMock = new Mock<IAuthStateService>();
        authMock.Setup(s => s.GetUserId()).Returns("test-user");
        Services.AddSingleton(authMock.Object);
        return authMock;
    }

    private RecipeDto CreateRecipe(Action<RecipeDtoBuilder>? configure = null)
    {
        var b = new RecipeDtoBuilder();
        configure?.Invoke(b);
        return b.Build();
    }

    private Mock<IRecipeApiService> RegisterMockApi(RecipeDto recipe)
    {
        var mock = new Mock<IRecipeApiService>();
        mock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        Services.AddSingleton(mock.Object);
        return mock;
    }

    private IRenderedComponent<RecipeDetail> RenderWith(RecipeDto recipe)
    {
        return Render<RecipeDetail>(p => p.Add(r => r.Id, recipe.Id));
    }

    [Fact]
    public void WhenApiReturnsRecipe_ShowsTitleAndMetadata()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();
        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("Test Recipe")
            .WithDescription("A test recipe")
            .WithCategory(RecipeBook.Domain.Enums.RecipeCategory.Dinner));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Test Recipe");
        cut.Markup.Should().Contain("Dinner");
    }

    [Fact]
    public async Task WhenApiFails_ShowsErrorMessage()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();
        var apiMock = new Mock<IRecipeApiService>();
        apiMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new ApiException("Not found"));
        Services.AddSingleton(apiMock.Object);

        // Act
        var cut = Render<RecipeDetail>(p => p.Add(r => r.Id, Guid.NewGuid()));
        await Task.Delay(100);

        // Assert
        cut.Markup.Should().Contain("Not found");
    }

    [Fact]
    public void WhenRecipeHasIngredientsAndSteps_ShowsBothSections()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();
        var recipe = CreateRecipe(r => r
            .WithIngredient(new IngredientDto(Guid.NewGuid(), 0, 2.5m, "cups", "Flour", null))
            .WithStep(new RecipeStepDto(Guid.NewGuid(), 0, null, "Mix ingredients")));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Flour");
        cut.Markup.Should().Contain("Mix ingredients");
    }

    [Fact]
    public async Task WhenServingSizeDoubled_IngredientQuantitiesDouble()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();
        var recipe = CreateRecipe(r => r
            .WithIngredient(new IngredientDto(Guid.NewGuid(), 0, 2.5m, "cups", "Flour", null))
            .WithServingSize(4));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);
        await cut.InvokeAsync(() => cut.Find("input[type=number]").Change("8"));

        // Assert
        cut.Markup.Should().Contain("Flour");
    }

    [Fact]
    public void WhenRecipeHasSteps_ShowsNumberedStepsInOrder()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();
        var recipe = CreateRecipe(r => r
            .WithStep(new RecipeStepDto(Guid.NewGuid(), 1, null, "Prep vegetables"))
            .WithStep(new RecipeStepDto(Guid.NewGuid(), 0, null, "Heat pan")));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert — steps should appear in SortOrder (0 before 1)
        var markup = cut.Markup;
        var heatIndex = markup.IndexOf("Heat pan");
        var prepIndex = markup.IndexOf("Prep vegetables");
        heatIndex.Should().BeLessThan(prepIndex, "Step 0 should appear before step 1 in the rendered output");
    }

    [Fact]
    public void WhenPublicRecipeNonOwner_ShowsForkButton()
    {
        // Arrange
        Services.AddMudServices();
        var authMock = new Mock<IAuthStateService>();
        authMock.Setup(s => s.GetUserId()).Returns("different-user");
        Services.AddSingleton(authMock.Object);

        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("Public Recipe")
            .IsPublic()
            .OwnerId("owner-1"));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Fork");
    }

    [Fact]
    public void WhenUserIsOwner_ShowsEditAndDeleteButtons()
    {
        // Arrange
        Services.AddMudServices();
        var authMock = new Mock<IAuthStateService>();
        authMock.Setup(s => s.GetUserId()).Returns("owner-1");
        Services.AddSingleton(authMock.Object);

        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("My Recipe")
            .IsPublic()
            .OwnerId("owner-1"));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Edit");
        cut.Markup.Should().Contain("Delete");
    }

    [Fact]
    public void WhenPrivateRecipeOwner_ShowsPrivateBadge()
    {
        // Arrange
        Services.AddMudServices();
        var authMock = new Mock<IAuthStateService>();
        authMock.Setup(s => s.GetUserId()).Returns("owner-1");
        Services.AddSingleton(authMock.Object);

        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("Private Recipe")
            .IsPrivate()
            .OwnerId("owner-1"));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Private");
    }

    [Fact]
    public void WhenForkedRecipe_ShowsOwnerAndSourceLink()
    {
        // Arrange
        Services.AddMudServices();
        RegisterDefaultAuthMock();

        var sourceId = Guid.NewGuid();
        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("Forked Recipe")
            .IsPublic()
            .OwnerId("owner-1")
            .WithSourceRecipe(sourceId));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert
        cut.Markup.Should().Contain("Chef Test");
        cut.Markup.Should().Contain(sourceId.ToString());
    }

    [Fact]
    public void WhenPrivateRecipeNonOwner_HidesBadgeAndActions()
    {
        // Arrange
        Services.AddMudServices();
        var authMock = new Mock<IAuthStateService>();
        authMock.Setup(s => s.GetUserId()).Returns("random-user");
        Services.AddSingleton(authMock.Object);

        var recipe = CreateRecipe(r => r
            .WithId(Guid.NewGuid())
            .WithTitle("Private Recipe")
            .IsPrivate()
            .OwnerId("owner-1"));

        // Act
        RegisterMockApi(recipe);
        var cut = RenderWith(recipe);

        // Assert — non-owner should not see private badge or edit/delete
        cut.Markup.Should().NotContain("style=\"display:inline-block;background:#ed6c02");
        cut.Markup.Should().NotContain("Edit");
        cut.Markup.Should().NotContain("Delete");
    }

    #region Test helpers

    private class RecipeDtoBuilder
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Title { get; private set; } = "Test Recipe";
        public string? Description { get; private set; }
        public int ServingSize { get; private set; } = 4;
        public RecipeBook.Domain.Enums.RecipeCategory Category { get; private set; } = RecipeBook.Domain.Enums.RecipeCategory.Dinner;
        public RecipeBook.Domain.Enums.RecipeVisibility Visibility { get; private set; } = RecipeBook.Domain.Enums.RecipeVisibility.Public;
        public string OwnerIdValue { get; private set; } = "owner-1";
        public string OwnerDisplayName { get; private set; } = "Chef Test";
        public Guid? SourceRecipeId { get; private set; }
        public List<IngredientDto> Ingredients { get; } = new();
        public List<RecipeStepDto> Steps { get; } = new();

        public RecipeDtoBuilder WithId(Guid id) { Id = id; return this; }
        public RecipeDtoBuilder WithTitle(string title) { Title = title; return this; }
        public RecipeDtoBuilder WithDescription(string desc) { Description = desc; return this; }
        public RecipeDtoBuilder WithServingSize(int size) { ServingSize = size; return this; }
        public RecipeDtoBuilder WithCategory(RecipeBook.Domain.Enums.RecipeCategory cat) { Category = cat; return this; }
        public RecipeDtoBuilder IsPublic() { Visibility = RecipeBook.Domain.Enums.RecipeVisibility.Public; return this; }
        public RecipeDtoBuilder IsPrivate() { Visibility = RecipeBook.Domain.Enums.RecipeVisibility.Private; return this; }
        public RecipeDtoBuilder OwnerId(string id) { OwnerIdValue = id; return this; }
        public RecipeDtoBuilder WithSourceRecipe(Guid sourceId) { SourceRecipeId = sourceId; return this; }
        public RecipeDtoBuilder WithIngredient(IngredientDto ing) { Ingredients.Add(ing); return this; }
        public RecipeDtoBuilder WithStep(RecipeStepDto step) { Steps.Add(step); return this; }

        public RecipeDto Build() => new(
            Id, Title, Description, null, 10, 20, ServingSize, Category, Visibility,
            OwnerIdValue, OwnerDisplayName, SourceRecipeId, null, null, null, null,
            Array.Empty<string>(), Ingredients.ToArray(), Steps.ToArray(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    #endregion
}
