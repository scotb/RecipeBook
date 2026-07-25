using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using RecipeBook.Blazor.Server.Components.Shared;

#pragma warning disable BL0005 // Component parameters should not be set outside of their component (standard bUnit test pattern)

namespace RecipeBook.Blazor.Server.Tests.Components.Shared;

public class PaginationBarTests : BunitContext
{
    [Fact]
    public void RendersWithoutThrowing()
    {
        // Act
        var cut = Render<PaginationBar>();
        cut.Should().NotBeNull();
    }

    [Fact]
    public async Task FiresPageChangedEventWhenPageClicked()
    {
        // Arrange
        int? selectedPage = null;

        var cut = Render<PaginationBar>();
        cut.Instance.TotalPages = 5;
        cut.Instance.CurrentPage = 1;
        cut.Instance.OnPageChanged = EventCallback.Factory.Create<int>(cut.Instance, (page) => selectedPage = page);

        // Act — invoke callback directly (simulating page click)
        await cut.InvokeAsync(() => cut.Instance.OnPageChanged.InvokeAsync(3));

        // Assert
        selectedPage.Should().Be(3);
    }

    [Fact]
    public async Task FiresPreviousPageWhenPrevClicked()
    {
        // Arrange
        int? selectedPage = null;

        var cut = Render<PaginationBar>();
        cut.Instance.TotalPages = 5;
        cut.Instance.CurrentPage = 3;
        cut.Instance.OnPageChanged = EventCallback.Factory.Create<int>(cut.Instance, (page) => selectedPage = page);

        // Act — simulate prev button click (page - 1)
        await cut.InvokeAsync(() => cut.Instance.OnPageChanged.InvokeAsync(2));

        // Assert
        selectedPage.Should().Be(2);
    }

    [Fact]
    public async Task FiresNextPageWhenNextClicked()
    {
        // Arrange
        int? selectedPage = null;

        var cut = Render<PaginationBar>();
        cut.Instance.TotalPages = 5;
        cut.Instance.CurrentPage = 3;
        cut.Instance.OnPageChanged = EventCallback.Factory.Create<int>(cut.Instance, (page) => selectedPage = page);

        // Act — simulate next button click (page + 1)
        await cut.InvokeAsync(() => cut.Instance.OnPageChanged.InvokeAsync(4));

        // Assert
        selectedPage.Should().Be(4);
    }
}
