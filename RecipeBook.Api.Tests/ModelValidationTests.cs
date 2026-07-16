using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeBook.Api.Controllers;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api.Tests;

public class ModelValidationTests
{
    private static RecipeController CreateController()
    {
        var controller = new RecipeController(Mock.Of<IRecipeService>());
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1")
        }, "Test"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task GetPublicRecipes_WithNonNumericPage_ReturnsBadRequest()
    {
        // Arrange: [ApiController] converts BadRequestResult → ProblemDetails JSON at pipeline level.
        var controller = CreateController();
        controller.ModelState.AddModelError("page", "The field page must be a number.");

        // Act
        var result = await controller.GetPublicRecipes(page: 1);

        // Assert — ProblemDetails via ObjectResult (400).
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
    }
}
