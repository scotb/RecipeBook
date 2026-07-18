using FluentAssertions;
using Moq;
using Moq.Protected;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class DemoAuthServiceTests
{
    [Fact]
    public async Task LoginAsDemoAsync_WhenApiReturns200_ReturnsToken()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"token":"eyJhbGciOiJIUzI1NiJ9.test.payload"}""",
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new DemoAuthService(httpClient);

        // Act
        var token = await service.LoginAsDemoAsync();

        // Assert
        token.Should().Be("eyJhbGciOiJIUzI1NiJ9.test.payload");
    }
}
