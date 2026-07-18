using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Moq;
using RecipeBook.Blazor.Server.Components.Pages;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests;

public class HomeTests : BunitContext
{
    [Fact]
    public void WhenUserIsNotAuthenticated_ShowsDemoLoginButton()
    {
        // Arrange
        Services.AddMudServices();
        var demoAuthMock = new Mock<IDemoAuthService>();
        var authStateMock = new Mock<IAuthStateService>();
        Services.AddSingleton(demoAuthMock.Object);
        Services.AddSingleton(authStateMock.Object);

        // Act
        var cut = Render<Home>();

        // Assert
        cut.Find("button").TextContent.Should().Contain("Login as Demo User");
    }

    [Fact]
    public async Task WhenButtonClickCalled_SetsAuthStateAndNavigatesToCatalog()
    {
        // Arrange
        Services.AddMudServices();

        var demoAuthMock = new Mock<IDemoAuthService>();
        demoAuthMock.Setup(s => s.LoginAsDemoAsync()).ReturnsAsync("test-jwt-token");

        var authStateMock = new Mock<IAuthStateService>();
        Services.AddSingleton(demoAuthMock.Object);
        Services.AddSingleton(authStateMock.Object);

        var cut = Render<Home>();

        // Act
        cut.Find("button").Click();

        // Allow async handler to complete
        await Task.Delay(100);

        // Assert
        demoAuthMock.Verify(s => s.LoginAsDemoAsync(), Times.Once);
        authStateMock.Verify(s => s.SetUser("test-jwt-token"), Times.Once);
    }
}
