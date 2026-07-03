using FluentAssertions;
using RecipeBook.Application.Models;

namespace RecipeBook.Api.Tests;

public class ModelValidationTests
{
    [Fact]
    public void RecipeQueryBuilder_WhenPageIsZero_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(null, null, null, 0, 20);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void RecipeQueryBuilder_WhenPageSizeIsZero_ReturnsError()
    {
        // Act
        var (error, query) = RecipeQueryBuilder.Create(null, null, null, 1, 0);

        // Assert
        error.Should().NotBeNull();
        query.Should().BeNull();
    }
}
