using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeBook.Api.Controllers;
using RecipeBook.Application.Interfaces;

namespace RecipeBook.Api.Tests;

public class AuthControllerTests
{
    private static ClaimsPrincipal CreateAuthenticatedUser(string userId, string email, string name)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static AuthController CreateController(ITokenService? tokenService = null)
    {
        var controller = new AuthController(tokenService ?? Mock.Of<ITokenService>());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    [Fact]
    public void GetMe_WithAuthenticatedUser_Returns200()
    {
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1", "test@example.com", "Test User");
        var result = controller.GetMe();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Callback_CallsTokenServiceAndReturnsToken()
    {
        var expectedToken = "fake-jwt-token-123";
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(s => s.GenerateToken(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>>()))
            .Returns(expectedToken);
        var controller = CreateController(mockTokenService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("google-user-1", "user@gmail.com", "Gmail User");
        var result = controller.Callback();
        mockTokenService.Verify(s => s.GenerateToken(
            "google-user-1", "user@gmail.com", "Gmail User", null, Enumerable.Empty<string>()), Times.Once);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Logout_Returns200()
    {
        var controller = CreateController();
        var result = controller.Logout();
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public void Callback_WhenUnauthenticated_Returns200()
    {
        var controller = CreateController();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var result = controller.Callback();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Logout_WhenUnauthenticated_Returns200()
    {
        var controller = CreateController();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var result = controller.Logout();
        result.Should().BeOfType<OkResult>();
    }
}
