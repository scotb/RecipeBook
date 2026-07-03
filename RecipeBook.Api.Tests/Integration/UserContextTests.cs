using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RecipeBook.Api;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api.Tests.Integration;

public class UserContextTests
{
    [Fact]
    public void GetUserId_WhenUserHasClaim_ReturnsCorrectId()
    {
        // Arrange
        var expectedUserId = "user-123";
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId),
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "Test");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new UserControllerContext(mockAccessor.Object);

        // Act
        var result = context.UserId;

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetUserId_WhenNoUser_ReturnsEmptyString()
    {
        // Arrange
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var context = new UserControllerContext(mockAccessor.Object);

        // Act
        var result = context.UserId;

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void IsInRole_WhenUserHasRole_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new UserControllerContext(mockAccessor.Object);

        // Act
        var result = context.IsInRole("Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WhenUserLacksRole_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new UserControllerContext(mockAccessor.Object);

        // Act
        var result = context.IsInRole("SuperAdmin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInRole_WhenNoHttpContext_ReturnsFalse()
    {
        // Arrange
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var context = new UserControllerContext(mockAccessor.Object);

        // Act
        var result = context.IsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }
}
