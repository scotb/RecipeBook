using FluentAssertions;
using RecipeBook.Application.Models;

namespace RecipeBook.Application.Tests.Models;

public class RecipeQueryBuilderTests
{
    [Fact]
    public void Create_WhenPageIsZero_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: null, tags: null, 
            page: 0, pageSize: 20);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void Create_WhenPageSizeIsZero_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: null, tags: null, 
            page: 1, pageSize: 0);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void Create_WhenPageSizeExceeds100_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: null, tags: null, 
            page: 1, pageSize: 101);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void Create_WhenSearchIsWhitespaceOnly_ReturnsNullSearch()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: "   ", category: null, tags: null, 
            page: 1, pageSize: 20);

        // Assert
        error.Should().BeNull();
        query.Should().NotBeNull();
        query!.SearchText.Should().BeNull();
    }

    [Fact]
    public void Create_WhenCategoryIsInvalid_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: "InvalidCategory", tags: null, 
            page: 1, pageSize: 20);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void Create_WhenCategoryIsValid_ReturnsValidQuery()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: "Lunch", tags: null, 
            page: 1, pageSize: 20);

        // Assert
        error.Should().BeNull();
        query.Should().NotBeNull();
        query!.Category.Should().Be(Domain.Enums.RecipeCategory.Lunch);
    }

    [Fact]
    public void Create_WhenTagsAreCommaSeparated_ParsesCorrectly()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: null, category: null, tags: "warm, comfort, easy", 
            page: 1, pageSize: 20);

        // Assert
        error.Should().BeNull();
        query.Should().NotBeNull();
        query!.Tags.Should().Contain(new[] { "warm", "comfort", "easy" });
    }

    [Fact]
    public void Create_WhenAllValidParameters_ProducesValidQuery()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(
            search: "soup", category: "Soup", tags: "warm, comfort", 
            page: 2, pageSize: 15);

        // Assert
        error.Should().BeNull();
        query.Should().NotBeNull();
        query!.SearchText.Should().Be("soup");
        query.Category.Should().Be(Domain.Enums.RecipeCategory.Soup);
        query.Tags.Should().Contain(new[] { "warm", "comfort" });
        query.Page.Should().Be(2);
        query.PageSize.Should().Be(15);
    }
}
