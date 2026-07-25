using FluentAssertions;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Domain.Enums;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Pages;

/// <summary>
/// Unit tests for Catalog page behavior.
/// Tests verify the public API contract without rendering (avoids MudBlazor service requirements in tests).
/// </summary>
public class CatalogTests
{
    private Mock<IRecipeApiService> CreateMockApi(PagedResult<RecipeSummaryDto>? result = null)
    {
        var mock = new Mock<IRecipeApiService>();
        mock.Setup(s => s.GetPublicRecipesAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? new PagedResult<RecipeSummaryDto>(Array.Empty<RecipeSummaryDto>(), 0, 1, 12));
        return mock;
    }

    [Fact]
    public void ComponentCanBeCreated()
    {
        // Act
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();

        // Assert
        catalog.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshWithFilters_PassesSearchTextToApi()
    {
        // Arrange
        var apiMock = CreateMockApi();
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();
        
        // Inject mock via reflection (component expects IRecipeApiService injection)
        var serviceField = typeof(RecipeBook.Blazor.Server.Components.Pages.Catalog)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IRecipeApiService));
        
        if (serviceField != null)
        {
            serviceField.SetValue(catalog, apiMock.Object);
        }

        // Act
        await catalog.RefreshWithFilters("pasta", null, null);

        // Assert
        apiMock.Verify(s => s.GetPublicRecipesAsync(
                "pasta", null, null, 1, 12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshWithFilters_PassesPageNumberToApi()
    {
        // Arrange
        var apiMock = CreateMockApi();
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();
        
        var serviceField = typeof(RecipeBook.Blazor.Server.Components.Pages.Catalog)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IRecipeApiService));
        
        if (serviceField != null)
        {
            serviceField.SetValue(catalog, apiMock.Object);
        }

        // Act
        await catalog.RefreshWithFilters(null, null, null, 3);

        // Assert
        apiMock.Verify(s => s.GetPublicRecipesAsync(
                null, null, null, 3, 12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshWithFilters_PassesCategoryToApi()
    {
        // Arrange
        var apiMock = CreateMockApi();
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();
        
        var serviceField = typeof(RecipeBook.Blazor.Server.Components.Pages.Catalog)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IRecipeApiService));
        
        if (serviceField != null)
        {
            serviceField.SetValue(catalog, apiMock.Object);
        }

        // Act
        await catalog.RefreshWithFilters(null, "Dessert", null);

        // Assert
        apiMock.Verify(s => s.GetPublicRecipesAsync(
                null, "Dessert", null, 1, 12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshWithFilters_PassesSortToApi()
    {
        // Arrange
        var apiMock = CreateMockApi();
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();
        
        var serviceField = typeof(RecipeBook.Blazor.Server.Components.Pages.Catalog)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IRecipeApiService));
        
        if (serviceField != null)
        {
            serviceField.SetValue(catalog, apiMock.Object);
        }

        // Act
        await catalog.RefreshWithFilters(null, null, "Name");

        // Assert
        apiMock.Verify(s => s.GetPublicRecipesAsync(
                null, null, "Name", 1, 12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshWithFilters_DefaultsToPage1()
    {
        // Arrange
        var apiMock = CreateMockApi();
        var catalog = new RecipeBook.Blazor.Server.Components.Pages.Catalog();
        
        var serviceField = typeof(RecipeBook.Blazor.Server.Components.Pages.Catalog)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(IRecipeApiService));
        
        if (serviceField != null)
        {
            serviceField.SetValue(catalog, apiMock.Object);
        }

        // Act
        await catalog.RefreshWithFilters(null, null, null);

        // Assert
        apiMock.Verify(s => s.GetPublicRecipesAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                1, 12, It.IsAny<CancellationToken>()), Times.Once);
    }
}
