using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Interfaces;
using RecipeBook.Application.Models;
using RecipeBook.Api.Controllers;

namespace RecipeBook.Api.Tests;

public class AdminControllerTests
{
    private static ClaimsPrincipal CreateAdminUser(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim(ClaimTypes.Name, "Admin User"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static AdminController CreateController(IRecipeService? recipeService = null)
    {
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(u => u.UserId).Returns("admin-1");
        userContextMock.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(true);

        var controller = new AdminController(
            recipeService ?? Mock.Of<IRecipeService>(),
            userContextMock.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    [Fact]
    public async Task GetAdminRecipes_WithValidAuth_Returns200WithPagedResult()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetAllRecipesAsync(
            It.IsAny<AdminRecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        var result = await controller.GetRecipes();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAdminRecipes_PassesQueryParamsToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 10, 2, 5);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetAllRecipesAsync(
            It.Is<AdminRecipeQuery>(q =>
                q.SearchText == "pasta" &&
                q.OwnerId == "user-42" &&
                q.Page == 2 &&
                q.PageSize == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        await controller.GetRecipes(search: "pasta", ownerId: "user-42", page: 2, pageSize: 5);

        // Assert
        mockService.Verify(s => s.GetAllRecipesAsync(
            It.Is<AdminRecipeQuery>(q =>
                q.SearchText == "pasta" &&
                q.OwnerId == "user-42" &&
                q.Page == 2 &&
                q.PageSize == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminRecipes_WithInvalidPage_Returns400()
    {
        // Arrange
        var mockService = new Mock<IRecipeService>();
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        var result = await controller.GetRecipes(page: 0);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.ObjectResult>();
        var objectResult = (Microsoft.AspNetCore.Mvc.ObjectResult)result;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetAdminRecipes_WithMissingUserId_Returns500()
    {
        // Arrange
        var mockService = new Mock<IRecipeService>();
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(u => u.UserId).Returns("");

        var controller = new AdminController(
            mockService.Object,
            userContextMock.Object);

        // Act
        var act = async () => await controller.GetRecipes();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User identity claim is missing.");
    }

    [Fact]
    public async Task DeleteAdminRecipe_Returns204()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, "admin-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        var result = await controller.DeleteRecipe(expectedId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAdminRecipe_PassesUserIdToService()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        await controller.DeleteRecipe(expectedId);

        // Assert
        mockService.Verify(s => s.DeleteAsync(
            expectedId, "admin-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAdminRecipe_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAdminUser("admin-1");

        // Act
        var act = async () => await controller.DeleteRecipe(expectedId);

        // Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task DeleteAdminRecipe_WithMissingUserId_Returns500()
    {
        // Arrange
        var mockService = new Mock<IRecipeService>();
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(u => u.UserId).Returns("");

        var controller = new AdminController(
            mockService.Object,
            userContextMock.Object);

        // Act
        var act = async () => await controller.DeleteRecipe(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User identity claim is missing.");
    }
}
