using System.Net;
using FluentAssertions;
using Moq;
using Moq.Protected;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Exceptions;
using RecipeBook.Blazor.Server.Services;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Blazor.Server.Tests.Services;

public class MealPlanApiServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenApiReturns200_ReturnsMealPlanSummaries()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """[{"id":"00000000-0000-0000-0000-000000000001","name":"Week 1","startDate":"2025-01-06","recipeCount":7,"createdAt":"2025-01-01T00:00:00Z"}]""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Week 1");
    }

    [Fact]
    public async Task GetByIdAsync_WhenApiReturns200_ReturnsMealPlanDto()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """{"id":"00000000-0000-0000-0000-000000000001","name":"Week 1","startDate":"2025-01-06","recipeCount":7,"entries":[],"createdAt":"2025-01-01T00:00:00Z","updatedAt":"2025-01-01T00:00:00Z"}""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Name.Should().Be("Week 1");
    }

    [Fact]
    public async Task CreateAsync_WhenApiReturns201_ReturnsMealPlanDto()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """{"id":"00000000-0000-0000-0000-000000000001","name":"Week 1","startDate":"2025-01-06","recipeCount":7,"entries":[],"createdAt":"2025-01-01T00:00:00Z","updatedAt":"2025-01-01T00:00:00Z"}""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);
        var request = new CreateMealPlanRequest(DateOnly.FromDateTime(new DateTime(2025, 1, 6)), "Week 1");

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Name.Should().Be("Week 1");
    }

    [Fact]
    public async Task UpdateAsync_WhenApiReturns200_ReturnsUpdatedMealPlanDto()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        var json = """{"id":"00000000-0000-0000-0000-000000000001","name":"Updated Week 1","startDate":"2025-01-06","recipeCount":7,"entries":[],"createdAt":"2025-01-01T00:00:00Z","updatedAt":"2025-01-03T00:00:00Z"}""";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);
        var request = new UpdateMealPlanRequest("Updated Week 1");

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        result.Name.Should().Be("Updated Week 1");
    }

    [Fact]
    public async Task SetEntryAsync_WhenApiReturns200_ReturnsMealEntryDto()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        var recipeId = Guid.NewGuid();
        var json = "{\"id\":\"00000000-0000-0000-0000-000000000002\",\"dayOfWeek\":1,\"mealSlot\":2,\"servingCount\":4,\"recipe\":{\"id\":\"" + recipeId + "\",\"title\":\"Pasta\",\"imageUrl\":null,\"category\":3,\"servingSize\":4}}";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);
        var request = new SetMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner, recipeId, 4);

        // Act
        var result = await service.SetEntryAsync(Guid.NewGuid(), request);

        // Assert
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
        result.MealSlot.Should().Be(MealSlot.Dinner);
        result.ServingCount.Should().Be(4);
    }

    [Fact]
    public async Task GetAllAsync_WhenApiReturns404_ThrowsApiException()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);

        // Act + Assert
        var act = async () => await service.GetAllAsync();
        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenApiReturns204_DoesNotThrow()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);

        // Act + Assert
        var act = async () => await service.DeleteAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteEntryAsync_WhenApiReturns204_DoesNotThrow()
    {
        // Arrange
        var authStateServiceMock = new Mock<IAuthStateService>();
        authStateServiceMock.Setup(s => s.GetJwt()).Returns("test-token");

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost") };
        var service = new TestMealPlanApiService(httpClient, authStateServiceMock.Object);
        var request = new ClearMealEntryRequest(DayOfWeek.Monday, MealSlot.Dinner);

        // Act + Assert
        var act = async () => await service.DeleteEntryAsync(Guid.NewGuid(), request);
        await act.Should().NotThrowAsync();
    }

    private class TestMealPlanApiService : MealPlanApiService
    {
        public TestMealPlanApiService(HttpClient httpClient, IAuthStateService authStateService)
            : base(httpClient, authStateService) { }

        public Task<HttpResponseMessage> SendTestAsync(HttpRequestMessage request, CancellationToken ct = default)
            => SendAsync(request, ct);
    }
}
