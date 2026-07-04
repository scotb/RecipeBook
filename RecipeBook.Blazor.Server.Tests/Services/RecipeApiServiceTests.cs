using System.Net;
using FluentAssertions;
using Moq;
using Moq.Protected;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Blazor.Server.Services;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class RecipeApiServiceTests
{
    [Fact]
    public async Task GetPublicRecipesAsync_WhenApiReturns200_ReturnsPagedResult()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """{"items":[{"id":"00000000-0000-0000-0000-000000000001","title":"Pasta","description":null,"imageUrl":null,"category":3,"visibility":2,"servingSize":4,"prepTimeMinutes":10,"cookTimeMinutes":20,"tags":["quick"],"ownerId":"user-1","ownerDisplayName":"Test","createdAt":"2025-01-01T00:00:00Z"}],"totalCount":1,"page":1,"pageSize":10}""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act
        var result = await service.GetPublicRecipesAsync(search: null, category: null, tags: null, page: 1, pageSize: 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Pasta");
    }

    [Fact]
    public async Task CreateAsync_WhenApiReturns201_ReturnsRecipeDto()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """{"id":"00000000-0000-0000-0000-000000000001","title":"Pasta","description":null,"imageUrl":null,"prepTimeMinutes":10,"cookTimeMinutes":20,"servingSize":4,"category":3,"visibility":1,"ownerId":"user-1","ownerDisplayName":"Test","sourceRecipeId":null,"caloriesPerServing":null,"proteinGrams":null,"carbsGrams":null,"fatGrams":null,"tags":[],"ingredients":[],"steps":[],"createdAt":"2025-01-01T00:00:00Z","updatedAt":"2025-01-01T00:00:00Z"}""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);
        var request = new CreateRecipeRequest
        {
            Title = "Pasta",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Title.Should().Be("Pasta");
    }

    [Fact]
    public async Task DeleteAsync_WhenApiReturns204_DoesNotThrow()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act + Assert
        var act = async () => await service.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForkAsync_WhenApiReturns409_ThrowsApiException()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("""{"detail":"Recipe already forked"}""", System.Text.Encoding.UTF8, "application/problem+json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act
        var act = async () => await service.ForkAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ApiException>().WithMessage("*already forked*");
    }

    [Fact]
    public async Task ImportAsync_WhenApiReturns422_ThrowsApiExceptionWithProblemDetailsMessage()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent("""{"detail":"Invalid URL provided"}""", System.Text.Encoding.UTF8, "application/problem+json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act
        var act = async () => await service.ImportAsync("not-a-url");

        // Assert
        await act.Should().ThrowAsync<ApiException>().WithMessage("*Invalid URL*");
    }

    [Fact]
    public async Task GetPublicRecipesAsync_WhenApiReturns404_ThrowsApiException()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns((string?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act + Assert
        var act = async () => await service.GetPublicRecipesAsync(null, null, null, 1, 10);
        await act.Should().ThrowAsync<ApiException>().WithMessage("*Failed to fetch recipes*");
    }

    [Fact]
    public async Task ForkAsync_WhenApiReturns500_ThrowsApiException()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestRecipeApiService(httpClient, authStateServiceMock.Object);

        // Act + Assert
        var act = async () => await service.ForkAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<ApiException>();
    }

    private class TestRecipeApiService : RecipeApiService
    {
        public TestRecipeApiService(HttpClient httpClient, IAuthStateService authStateService)
            : base(httpClient, authStateService) { }

        public Task<HttpResponseMessage> SendTestAsync(HttpRequestMessage request, CancellationToken ct = default)
            => SendAsync(request, ct);
    }
}
