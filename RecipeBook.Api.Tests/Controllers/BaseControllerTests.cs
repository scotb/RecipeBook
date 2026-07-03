using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeBook.Api.Controllers;

namespace RecipeBook.Api.Tests.Controllers;

public class BaseControllerTests : TestBaseController
{
    [Fact]
    public void Problem_With400AndDetail_ReturnsObjectResultWithProblemDetails()
    {
        // Arrange & Act
        var result = Problem(400, "test detail");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
        
        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(400);
        problemDetails.Detail.Should().Be("test detail");
    }

    [Fact]
    public void GetUserId_WithValidClaim_ReturnsClaimValue()
    {
        // Arrange
        var userId = "user-123";
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        // Act
        var result = GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_WithMissingClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        var act = () => GetUserId();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("User identity claim is missing.");
    }
}
