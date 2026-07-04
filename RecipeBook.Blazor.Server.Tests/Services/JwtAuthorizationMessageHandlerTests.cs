using FluentAssertions;
using Moq;
using Moq.Protected;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class JwtAuthorizationMessageHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenUserIsAuthenticated_AddsBearerAuthorizationHeader()
    {
        // Arrange
        var token = "test-jwt-token";
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns(token);

        HttpRequestMessage? capturedRequest = null;
        var innerMock = new Mock<HttpMessageHandler>();
        innerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

        var handler = new JwtAuthorizationMessageHandler(authStateServiceMock.Object);
        handler.InnerHandler = innerMock.Object;

        using var httpClient = new HttpClient(handler);

        // Act
        await httpClient.GetAsync("https://localhost/api/recipes");

        // Assert
        capturedRequest!.Headers.Authorization?.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task SendAsync_WhenUserIsNotAuthenticated_DoesNotAddAuthorizationHeader()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        HttpRequestMessage? capturedRequest = null;
        var innerMock = new Mock<HttpMessageHandler>();
        innerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

        var handler = new JwtAuthorizationMessageHandler(authStateServiceMock.Object);
        handler.InnerHandler = innerMock.Object;

        using var httpClient = new HttpClient(handler);

        // Act
        await httpClient.GetAsync("https://localhost/api/recipes");

        // Assert
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_PassesRequestThroughToNextHandler()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        var innerMock = new Mock<HttpMessageHandler>();
        innerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("ok") });

        var handler = new JwtAuthorizationMessageHandler(authStateServiceMock.Object);
        handler.InnerHandler = innerMock.Object;

        using var httpClient = new HttpClient(handler);

        // Act
        var response = await httpClient.GetAsync("https://localhost/api/recipes");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("ok");
    }
}
