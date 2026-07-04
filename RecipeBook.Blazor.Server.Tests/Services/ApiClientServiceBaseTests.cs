using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Moq.Protected;
using RecipeBook.Application.Exceptions;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class ApiClientServiceBaseTests
{
    [Fact]
    public async Task SendAsync_WhenResponseIs401_CallsAuthStateServiceClearAndRedirects()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var authStateServiceMock = new Mock<IAuthStateService>();
        var testService = new TestApiClientService(httpClient, authStateServiceMock.Object);

        // Act
        await testService.SendTestAsync(new HttpRequestMessage(HttpMethod.Get, "/api/recipes"));

        // Assert
        authStateServiceMock.Verify(s => s.ClearUser(), Times.Once);
        testService.PendingRedirectUri.Should().Be("/login");
    }

    [Fact]
    public async Task SendAsync_WhenResponseIs403_CallsAuthStateServiceClearAndSetsForbiddenRedirect()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var authStateServiceMock = new Mock<IAuthStateService>();
        var testService = new TestApiClientService(httpClient, authStateServiceMock.Object);

        // Act
        await testService.SendTestAsync(new HttpRequestMessage(HttpMethod.Get, "/api/recipes"));

        // Assert
        authStateServiceMock.Verify(s => s.ClearUser(), Times.Once);
        testService.PendingRedirectUri.Should().Be("/forbidden");
    }

    [Fact]
    public async Task SendAsync_WhenResponseIs200_DeserializesJsonCorrectly()
    {
        // Arrange
        var json = """{"items":[],"totalCount":0,"page":1,"pageSize":10}""";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var authStateServiceMock = new Mock<IAuthStateService>();
        var testService = new TestApiClientService(httpClient, authStateServiceMock.Object);

        // Act
        var response = await testService.SendTestAsync(new HttpRequestMessage(HttpMethod.Get, "/api/recipes"));

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        authStateServiceMock.Verify(s => s.ClearUser(), Times.Never);
        testService.PendingRedirectUri.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenResponseHasProblemDetails_ErrorContainsDetailMessage()
    {
        // Arrange
        var json = """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Unprocessable Entity","status":422,"detail":"Title is required"}""";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/problem+json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var authStateServiceMock = new Mock<IAuthStateService>();
        var testService = new TestApiClientService(httpClient, authStateServiceMock.Object);

        // Act
        var act = async () => await testService.SendTestAsync(new HttpRequestMessage(HttpMethod.Post, "/api/recipes"));

        // Assert
        await act.Should().ThrowAsync<ApiException>().WithMessage("*Title is required*");
    }

    private class TestApiClientService : ApiClientServiceBase
    {
        public TestApiClientService(HttpClient httpClient, IAuthStateService authStateService)
            : base(httpClient, authStateService) { }

        public Task<HttpResponseMessage> SendTestAsync(HttpRequestMessage request, CancellationToken ct = default)
            => SendAsync(request, ct);
    }
}
