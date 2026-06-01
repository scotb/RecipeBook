using FluentAssertions;
using RecipeBook.Infrastructure.Identity;

namespace RecipeBook.Infrastructure.Tests.Identity;

public class ApplicationUserTests
{
    [Fact]
    public void NewUser_ShouldHaveCreatedAtInitializedToUtcNow()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        // We check if the date is within a small window (e.g., 5 seconds) of now
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(5));
    }
}