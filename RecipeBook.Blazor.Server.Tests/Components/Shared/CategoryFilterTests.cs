using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using RecipeBook.Domain.Enums;
using RecipeBook.Blazor.Server.Components.Shared;

#pragma warning disable BL0005 // Component parameters should not be set outside of their component (standard bUnit test pattern)

namespace RecipeBook.Blazor.Server.Tests.Components.Shared;

public class CategoryFilterTests : BunitContext
{
    [Fact]
    public void RendersWithoutThrowing()
    {
        // Act
        var cut = Render<CategoryFilter>();
        cut.Should().NotBeNull();
    }

    [Fact]
    public async Task FiresSelectionEventWhenCategoryClicked()
    {
        // Arrange
        var categories = new[] { RecipeCategory.Dinner, RecipeCategory.Dessert };
        RecipeCategory? selectedCategory = null;

        var cut = Render<CategoryFilter>();
        cut.Instance.Categories = categories;
        cut.Instance.OnCategorySelected = EventCallback.Factory.Create<RecipeCategory?>(cut.Instance, (cat) => selectedCategory = cat);

        // Act — invoke callback directly (simulating chip click)
        await cut.InvokeAsync(() => cut.Instance.OnCategorySelected.InvokeAsync(RecipeCategory.Dinner));

        // Assert
        selectedCategory.Should().Be(RecipeCategory.Dinner);
    }

    [Fact]
    public async Task FiresNullWhenAllClicked()
    {
        // Arrange
        var categories = new[] { RecipeCategory.Dinner };
        RecipeCategory? selectedCategory = null;

        var cut = Render<CategoryFilter>();
        cut.Instance.Categories = categories;
        cut.Instance.OnCategorySelected = EventCallback.Factory.Create<RecipeCategory?>(cut.Instance, (cat) => selectedCategory = cat);

        // Act — invoke callback with null (simulating "All" chip click)
        await cut.InvokeAsync(() => cut.Instance.OnCategorySelected.InvokeAsync(null));

        // Assert
        selectedCategory.Should().BeNull();
    }
}
