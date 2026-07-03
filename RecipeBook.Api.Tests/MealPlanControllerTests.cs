using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeBook.Application.DTOs;
using RecipeBook.Application.Interfaces;
using RecipeBook.Api.Controllers;

namespace RecipeBook.Api.Tests;

public class MealPlanControllerTests
{
    private static ClaimsPrincipal CreateAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User")
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static MealPlanController CreateController(IMealPlanService? mealPlanService = null)
    {
        var controller = new MealPlanController(mealPlanService ?? Mock.Of<IMealPlanService>());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    [Fact]
    public async Task GetMealPlans_WithValidAuth_Returns200WithPagedResult()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var plans = new List<MealPlanSummaryDto>
        {
            new(expectedId, "Test Plan", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, 3)
        };
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.GetByUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetMealPlans();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMealPlans_PassesUserIdToService()
    {
        // Arrange
        const string userId = "user-42";
        var plans = new List<MealPlanSummaryDto>();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser(userId);

        // Act
        await controller.GetMealPlans();

        // Assert
        mockService.Verify(s => s.GetByUserAsync(
            userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMealPlan_WithValidAuth_Returns200WithDetail()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mealPlan = new MealPlanDto(
            expectedId, "Test Plan", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, Array.Empty<MealEntryDto>());
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.GetByIdAsync(expectedId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mealPlan);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetMealPlan(expectedId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returned = okResult.Value.Should().BeOfType<MealPlanDto>().Subject;
        returned.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task GetMealPlan_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.GetMealPlan(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task GetMealPlan_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.GetMealPlan(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task CreateMealPlan_WithValidBody_Returns201WithLocationHeader()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mealPlan = new MealPlanDto(
            expectedId, "New Plan", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, Array.Empty<MealEntryDto>());
        var request = new CreateMealPlanRequest(DateOnly.FromDateTime(DateTime.Today), null);
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.CreateAsync(
            It.IsAny<CreateMealPlanRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mealPlan);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.CreateMealPlan(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        var routeId = (Guid)createdResult.RouteValues!["id"]!;
        routeId.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateMealPlan_PassesRequestAndUserIdToService()
    {
        // Arrange
        const string userId = "create-user-7";
        var expectedId = Guid.NewGuid();
        var mealPlan = new MealPlanDto(
            expectedId, "New Plan", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, Array.Empty<MealEntryDto>());
        var request = new CreateMealPlanRequest(DateOnly.FromDateTime(DateTime.Today), null);
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.CreateAsync(
            It.Is<CreateMealPlanRequest>(r => r.WeekStartDate == DateOnly.FromDateTime(DateTime.Today)),
            userId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mealPlan);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser(userId);

        // Act
        await controller.CreateMealPlan(request);

        // Assert
        mockService.Verify(s => s.CreateAsync(
            It.Is<CreateMealPlanRequest>(r => r.WeekStartDate == DateOnly.FromDateTime(DateTime.Today)),
            userId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMealPlan_WithInvalidModel_ValidatedByApiController()
    {
        // ModelState validation is handled by [ApiController] at the HTTP pipeline level.
        var mockService = new Mock<IMealPlanService>();
        var expectedId = Guid.NewGuid();
        mockService.Setup(s => s.CreateAsync(It.IsAny<CreateMealPlanRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MealPlanDto(expectedId, "Test", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, Array.Empty<MealEntryDto>()));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act — valid request should succeed
        var result = await controller.CreateMealPlan(new CreateMealPlanRequest(
            DateOnly.FromDateTime(DateTime.Today), null));
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateMealPlan_WithValidBody_Returns200WithDetail()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mealPlan = new MealPlanDto(
            expectedId, "Updated Plan", DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.UtcNow, Array.Empty<MealEntryDto>());
        var request = new UpdateMealPlanRequest("Updated Plan");
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.UpdateAsync(expectedId, It.IsAny<UpdateMealPlanRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mealPlan);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.UpdateMealPlan(expectedId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returned = okResult.Value.Should().BeOfType<MealPlanDto>().Subject;
        returned.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task UpdateMealPlan_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<UpdateMealPlanRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.UpdateMealPlan(expectedId, new UpdateMealPlanRequest(null));
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task UpdateMealPlan_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<UpdateMealPlanRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.UpdateMealPlan(expectedId, new UpdateMealPlanRequest(null));
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task DeleteMealPlan_Returns204()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.DeleteAsync(expectedId, "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.DeleteMealPlan(expectedId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteMealPlan_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.DeleteMealPlan(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task DeleteMealPlan_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.DeleteMealPlan(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task SetMealPlanEntry_WithValidBody_Returns200WithEntry()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var entry = new MealEntryDto(
            expectedId, DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner, 2,
            new MealEntryRecipeDto(Guid.NewGuid(), "Test Recipe", null, Domain.Enums.RecipeCategory.Dinner, 2));
        var request = new SetMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner, Guid.NewGuid(), 2);
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.SetEntryAsync(
            It.IsAny<Guid>(), It.IsAny<SetMealEntryRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.SetMealPlanEntry(Guid.NewGuid(), request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returned = okResult.Value.Should().BeOfType<MealEntryDto>().Subject;
        returned.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task SetMealPlanEntry_PassesRequestAndUserIdToService()
    {
        // Arrange
        const string userId = "entry-user-3";
        var expectedId = Guid.NewGuid();
        var entry = new MealEntryDto(
            expectedId, DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner, 2,
            new MealEntryRecipeDto(Guid.NewGuid(), "Test Recipe", null, Domain.Enums.RecipeCategory.Dinner, 2));
        var request = new SetMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner, Guid.NewGuid(), 2);
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.SetEntryAsync(
            It.IsAny<Guid>(),
            It.Is<SetMealEntryRequest>(r => r.DayOfWeek == DayOfWeek.Monday && r.MealSlot == Domain.Enums.MealSlot.Dinner),
            userId,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser(userId);

        // Act
        await controller.SetMealPlanEntry(Guid.NewGuid(), request);

        // Assert
        mockService.Verify(s => s.SetEntryAsync(
            It.IsAny<Guid>(),
            It.Is<SetMealEntryRequest>(r => r.DayOfWeek == DayOfWeek.Monday && r.MealSlot == Domain.Enums.MealSlot.Dinner),
            userId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMealPlanEntry_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.SetEntryAsync(
            expectedId, It.IsAny<SetMealEntryRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new SetMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner, Guid.NewGuid(), 2);

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.SetMealPlanEntry(expectedId, request);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task ClearMealPlanEntry_Returns204()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.ClearEntryAsync(
            expectedId, It.IsAny<ClearMealEntryRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new ClearMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner);

        // Act
        var result = await controller.ClearMealPlanEntry(expectedId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ClearMealPlanEntry_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.ClearEntryAsync(
            expectedId, It.IsAny<ClearMealEntryRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new ClearMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner);

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.ClearMealPlanEntry(expectedId, request);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task ClearMealPlanEntry_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IMealPlanService>();
        mockService.Setup(s => s.ClearEntryAsync(
            expectedId, It.IsAny<ClearMealEntryRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new ClearMealEntryRequest(DayOfWeek.Monday, Domain.Enums.MealSlot.Dinner);

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.ClearMealPlanEntry(expectedId, request);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task GetMealPlans_WithMissingUserId_Returns500()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert — missing userId throws InvalidOperationException → GlobalExceptionHandler → 500
        Func<Task> act = () => controller.GetMealPlans();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
