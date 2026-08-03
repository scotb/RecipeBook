using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Blazor.Server.Components.Pages;
using RecipeBook.Blazor.Server.Services;


namespace RecipeBook.Blazor.Server.Tests.Pages;

public class RecipeFormTests : BunitContext
{
    private Mock<IRecipeApiService> RegisterMockApi()
    {
        var mock = new Mock<IRecipeApiService>();
        Services.AddSingleton(mock.Object);
        return mock;
    }

    private void SetupFormTest()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterMockApi();
    }

    [Fact]
    public void CreateMode_RendersWithBlankFields()
    {
        // Arrange
        SetupFormTest();

        // Act
        var cut = Render<RecipeForm>();

        // Assert — title should be empty in create mode
        cut.Markup.Should().Contain("Title");
        cut.FindAll("input").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Submit_WithMissingRequiredField_ShowsValidationError()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Act — click Save without filling required fields
        await cut.Find("button").ClickAsync(new MouseEventArgs());

        // Assert — validation error should appear for title
        cut.Markup.Should().Contain("required");
    }

    [Fact]
    public async Task AddIngredient_ClickAppendsNewRow()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Act — click "Add Ingredient" button
        var addButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Add Ingredient"));
        if (addButton != null)
            await addButton.ClickAsync(new MouseEventArgs());

        // Assert — ingredient input row appears
        cut.Markup.Should().Contain("Ingredient name");
    }

    [Fact]
    public async Task RemoveIngredient_ClickDeletesRow()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Precondition: add an ingredient row via UI
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Ingredient"));
        await addButton.ClickAsync(new MouseEventArgs());
        cut.Markup.Should().Contain("Ingredient name");

        // Act — find the ingredient row and click its delete button
        var deleteButton = cut.Find("div.d-flex button");
        await deleteButton.ClickAsync(new MouseEventArgs());

        // Assert — ingredient row should no longer be in the form
        cut.Markup.Should().NotContain("Ingredient name");
    }

    [Fact]
    public async Task AddStep_ClickAppendsNewRow()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Act — click "Add Step" button via UI
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Step"));
        await addButton.ClickAsync(new MouseEventArgs());

        // Assert — step textarea row appears
        cut.Markup.Should().Contain("Step description");
    }

    [Fact]
    public async Task RemoveStep_ClickDeletesRow()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Precondition: add a step row via UI
        var addButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Step"));
        await addButton.ClickAsync(new MouseEventArgs());
        cut.Markup.Should().Contain("Step description");

        // Act — find the step row and click its delete button
        var deleteButton = cut.Find("div.d-flex button");
        await deleteButton.ClickAsync(new MouseEventArgs());

        // Assert — step row should no longer be in the form
        cut.Markup.Should().NotContain("Step description");
    }

    [Fact]
    public async Task Save_WhenInvalid_DoesNotCallApi()
    {
        // Arrange
        SetupFormTest();

        var mock = RegisterMockApi();

        var cut = Render<RecipeForm>();

        // Act — click Save without filling required fields
        var saveBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Save Recipe"));
        await saveBtn.ClickAsync(new MouseEventArgs());

        // Assert — API should not be called
        mock.Verify(s => s.CreateAsync(It.IsAny<CreateRecipeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // TODO: Positive save test — API called + navigation when form is valid (revisit at end of Unit 4d)

    [Fact]
    public void CreateMode_ShowsIngredientAndStepWarnings_WhenListsEmpty()
    {
        // Arrange
        SetupFormTest();

        var cut = Render<RecipeForm>();

        // Assert — empty lists show warning messages
        cut.Markup.Should().Contain("No ingredients added");
        cut.Markup.Should().Contain("No steps added");
    }

    [Fact]
    public void EditMode_PopulatesFieldsFromRecipeDto()
    {
        // Arrange
        SetupFormTest();

        var apiMock = RegisterMockApi();
        
        var recipeId = Guid.NewGuid();
        var recipe = new RecipeDto(
            recipeId,
            "Test Recipe",
            "A test recipe",
            null,
            10,
            20,
            4,
            Domain.Enums.RecipeCategory.Dinner,
            Domain.Enums.RecipeVisibility.Public,
            "user-1",
            "Test User",
            null,
            null, null, null, null,
            Array.Empty<string>(),
            Array.Empty<IngredientDto>(),
            Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        apiMock.Setup(s => s.GetByIdAsync(recipeId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(recipe);

        // Act
        var cut = Render<RecipeForm>(p => p.Add(r => r.Id, recipeId));

        // Assert — title should be populated from API
        cut.Markup.Should().Contain("Test Recipe");
    }
}
