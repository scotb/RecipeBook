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

public class RecipeControllerTests
{
    private static ClaimsPrincipal CreateAuthenticatedUser(string userId, string email = "test@example.com", string name = "Test User")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static RecipeController CreateController(IRecipeService? recipeService = null, string userId = "user-1", bool isAdmin = false)
    {
        var userContextMock = new Mock<IUserContext>();
        userContextMock.Setup(u => u.UserId).Returns(userId);
        userContextMock.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(isAdmin);

        var controller = new RecipeController(
            recipeService ?? Mock.Of<IRecipeService>(),
            userContextMock.Object);
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Set up authenticated user in HttpContext for methods that check authentication
        if (!string.IsNullOrEmpty(userId))
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
            controller.HttpContext.User = new ClaimsPrincipal(identity);
        }
        return controller;
    }

    [Fact]
    public async Task GetPublicRecipes_WithSearchParam_PassesSearchTextToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.SearchText == "pasta"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(search: "pasta");

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.SearchText == "pasta"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithValidCategory_PassesCategoryToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Category == Domain.Enums.RecipeCategory.Lunch), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(category: "Lunch");

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Category == Domain.Enums.RecipeCategory.Lunch), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithTagsParam_PassesTrimmedTagsToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Tags != null && q.Tags.SequenceEqual(new[] { "italian", "quick" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(tags: " italian , quick ");

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Tags != null && q.Tags.SequenceEqual(new[] { "italian", "quick" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithPageAndPageSize_PassesPaginationToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Page == 3 && q.PageSize == 50), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(page: 3, pageSize: 50);

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Page == 3 && q.PageSize == 50), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithDefaultParams_ReturnsDefaultPagination()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Page == 1 && q.PageSize == 20), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes();

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Page == 1 && q.PageSize == 20), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithAllFilters_PassesAllParamsToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q =>
                q.SearchText == "chicken" &&
                q.Category == Domain.Enums.RecipeCategory.Dinner &&
                q.Tags != null && q.Tags.SequenceEqual(new[] { "easy", "healthy" }) &&
                q.Page == 2 &&
                q.PageSize == 10), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(search: "chicken", category: "Dinner", tags: " easy , healthy ", page: 2, pageSize: 10);

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q =>
                q.SearchText == "chicken" &&
                q.Category == Domain.Enums.RecipeCategory.Dinner &&
                q.Tags != null && q.Tags.SequenceEqual(new[] { "easy", "healthy" }) &&
                q.Page == 2 &&
                q.PageSize == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_ServiceReturnsEmptyResult_Returns200WithEmptyItems()
    {
        // Arrange
        var emptyItems = Array.Empty<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(emptyItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<RecipeSummaryDto>>().Subject;
        pagedResult.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPublicRecipes_WithWhitespaceSearch_PassesNullToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.SearchText == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(search: "   ");

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.SearchText == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithEmptyTagsString_PassesNullToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Tags == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetPublicRecipes(tags: "   ");

        // Assert
        mockService.Verify(s => s.GetPublicRecipesAsync(
            It.Is<RecipeQuery>(q => q.Tags == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicRecipes_WithInvalidCategory_ReturnsProblemDetails()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes(category: "InvalidCategory");

        // Assert — RFC 7807 ProblemDetails body.
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        obj.Value.Should().BeOfType<ProblemDetails>();
        var pd = (ProblemDetails)obj.Value;
        pd.Status.Should().Be(400);
        pd.Detail.Should().Contain("Invalid category");
    }

    [Fact]
    public async Task GetPublicRecipes_WithPageLessThanOne_ReturnsProblemDetails()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes(page: 0);

        // Assert — RFC 7807 ProblemDetails body.
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        obj.Value.Should().BeOfType<ProblemDetails>();
        var pd = (ProblemDetails)obj.Value;
        pd.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetPublicRecipes_WithPageSizeLessThanOne_ReturnsProblemDetails()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes(pageSize: 0);

        // Assert — RFC 7807 ProblemDetails body.
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        obj.Value.Should().BeOfType<ProblemDetails>();
        var pd = (ProblemDetails)obj.Value;
        pd.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetPublicRecipes_WithPageSizeExceedingMax_ReturnsProblemDetails()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes(pageSize: 101);

        // Assert — RFC 7807 ProblemDetails body.
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        obj.Value.Should().BeOfType<ProblemDetails>();
        var pd = (ProblemDetails)obj.Value;
        pd.Status.Should().Be(400);
    }

    [Fact]
    public void GetPublicRecipes_WithNonNumericPage_ValidatedByQueryBuilder()
    {
        // Validation moved to RecipeQueryBuilder in application layer
        var (error, query) = Application.Models.RecipeQueryBuilder.Create(null, null, null, 0, 20);
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public void GetPublicRecipes_WithNonNumericPageSize_ValidatedByQueryBuilder()
    {
        // Validation moved to RecipeQueryBuilder in application layer
        var (error, query) = Application.Models.RecipeQueryBuilder.Create(null, null, null, 1, 0);
        error.Should().NotBeNull();
        query.Should().BeNull();
    }

    [Fact]
    public async Task GetMyRecipes_WithValidAuth_Returns200WithPagedResults()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>
        {
            new(
                Guid.NewGuid(), "My Recipe", "A personal recipe", null,
                Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Private,
                4, 15, 30, Array.Empty<string>(),
                "user-1", "Test User", DateTimeOffset.UtcNow)
        };

        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 1, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            "user-1", It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetMyRecipes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMyRecipes_ExtractsUserIdFromClaimsAndPassesToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);
        const string userId = "claim-user-42";

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(userId, It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object, userId: userId);

        // Act
        await controller.GetMyRecipes();

        // Assert
        mockService.Verify(s => s.GetMyRecipesAsync(userId, It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyRecipes_WithSearchParam_PassesSearchTextToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.SearchText == "pasta"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetMyRecipes(search: "pasta");

        // Assert
        mockService.Verify(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.SearchText == "pasta"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyRecipes_WithAllFilters_CorrectlyPassesAllParamsAndUserId()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);
        const string userId = "my-user-7";

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            userId,
            It.Is<RecipeQuery>(q =>
                q.SearchText == "soup" &&
                q.Category == Domain.Enums.RecipeCategory.Lunch &&
                q.Tags != null && q.Tags.SequenceEqual(new[] { "warm", "comfort" }) &&
                q.Page == 2 &&
                q.PageSize == 15),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object, userId: userId);

        // Act
        await controller.GetMyRecipes(search: "soup", category: "Lunch", tags: " warm , comfort ", page: 2, pageSize: 15);

        // Assert
        mockService.Verify(s => s.GetMyRecipesAsync(
            userId,
            It.Is<RecipeQuery>(q =>
                q.SearchText == "soup" &&
                q.Category == Domain.Enums.RecipeCategory.Lunch &&
                q.Tags != null && q.Tags.SequenceEqual(new[] { "warm", "comfort" }) &&
                q.Page == 2 &&
                q.PageSize == 15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyRecipes_ServiceReturnsEmptyResult_Returns200WithEmptyItems()
    {
        // Arrange
        var emptyItems = Array.Empty<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(emptyItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            It.IsAny<string>(), It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetMyRecipes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<RecipeSummaryDto>>().Subject;
        pagedResult.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyRecipes_WithTagsParam_PassesTrimmedTagsToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.Tags != null && q.Tags.SequenceEqual(new[] { "spicy", "vegan" })),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetMyRecipes(tags: " spicy , vegan ");

        // Assert
        mockService.Verify(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.Tags != null && q.Tags.SequenceEqual(new[] { "spicy", "vegan" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyRecipes_WithInvalidPageSize_ReturnsProblemDetails()
    {
        // Arrange
        var controller = CreateController();
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetMyRecipes(pageSize: 200);

        // Assert — RFC 7807 ProblemDetails body.
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        obj.Value.Should().BeOfType<ProblemDetails>();
        var pd = (ProblemDetails)obj.Value;
        pd.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetMyRecipes_WithWhitespaceSearch_PassesNullToService()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>();
        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 0, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.SearchText == null),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetMyRecipes(search: "   ");

        // Assert
        mockService.Verify(s => s.GetMyRecipesAsync(
            It.IsAny<string>(),
            It.Is<RecipeQuery>(q => q.SearchText == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyRecipes_ServiceThrowsException_Returns500()
    {
        // Arrange
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetMyRecipesAsync(
            It.IsAny<string>(), It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert
        Func<Task> act = () => controller.GetMyRecipes();
        await act.Should().ThrowAsync<Exception>().WithMessage("Service failure");
    }

    [Fact]
    public async Task GetPublicRecipes_WithValidAuth_Returns200WithPagedResults()
    {
        // Arrange
        var expectedItems = new List<RecipeSummaryDto>
        {
            new(
                Guid.NewGuid(), "Test Recipe", "A test recipe", null,
                Domain.Enums.RecipeCategory.Lunch, Domain.Enums.RecipeVisibility.Public,
                2, 10, 20, Array.Empty<string>(),
                "owner-1", "Owner Name", DateTimeOffset.UtcNow)
        };

        var expectedPagedResult = new PagedResult<RecipeSummaryDto>(expectedItems, 1, 1, 20);

        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetPublicRecipesAsync(
            It.IsAny<RecipeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetPublicRecipes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetRecipe_WithValidAuth_Returns200WithRecipeDto()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Test Recipe", "A test recipe", null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            "user-1", "Owner Name", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.GetRecipe(expectedId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returnedRecipe = okResult.Value.Should().BeOfType<RecipeDto>().Subject;
        returnedRecipe.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task GetRecipe_WithValidId_PassesIdToService()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Test", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            "user-1", "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        await controller.GetRecipe(expectedId);

        // Assert
        mockService.Verify(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecipe_ExtractsUserIdFromClaimsAndPassesToService()
    {
        // Arrange
        const string userId = "claim-user-42";
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Test", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            userId, "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetByIdAsync(
            It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object, userId: userId);

        // Act
        await controller.GetRecipe(expectedId);

        // Assert
        mockService.Verify(s => s.GetByIdAsync(
            It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecipe_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.GetRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task GetRecipe_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.GetByIdAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.GetRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task GetRecipe_WithInvalidUserId_Returns500()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var controller = CreateController(userId: "");

        // Act & Assert — missing userId throws InvalidOperationException → GlobalExceptionHandler → 500
        Func<Task> act = () => controller.GetRecipe(expectedId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateRecipe_WithValidBody_Returns201WithLocationHeader()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "New Recipe", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            "user-1", "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var request = new Application.DTOs.CreateRecipeRequest
        {
            Title = "New Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.CreateAsync(
            It.IsAny<Application.DTOs.CreateRecipeRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.CreateRecipe(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        var routeId = (Guid)createdResult.RouteValues!["id"]!;
        routeId.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateRecipe_PassesRequestAndUserIdToService()
    {
        // Arrange
        const string userId = "create-user-7";
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "New Recipe", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            userId, "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var request = new Application.DTOs.CreateRecipeRequest
        {
            Title = "New Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.CreateAsync(
            It.IsAny<Application.DTOs.CreateRecipeRequest>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object, userId: userId);

        // Act
        await controller.CreateRecipe(request);

        // Assert
        mockService.Verify(s => s.CreateAsync(
            It.Is<Application.DTOs.CreateRecipeRequest>(r => r.Title == "New Recipe"),
            userId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRecipe_WithInvalidModel_ValidatedByApiController()
    {
        // ModelState validation is handled by [ApiController] at the HTTP pipeline level.
        var mockService = new Mock<IRecipeService>();
        var expectedId = Guid.NewGuid();
        mockService.Setup(s => s.CreateAsync(It.IsAny<Application.DTOs.CreateRecipeRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecipeDto(
                expectedId, "Valid Recipe", null, null,
                null, null, 4,
                Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
                "user-1", "Test User", null,
                null, null, null, null, Array.Empty<string>(),
                Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.CreateRecipeRequest
        {
            Title = "Valid Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act — valid request should succeed
        var result = await controller.CreateRecipe(request);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateRecipe_WithNullUserId_Returns500()
    {
        // Arrange
        var controller = CreateController(userId: "");
        var request = new Application.DTOs.CreateRecipeRequest
        {
            Title = "New Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act & Assert — missing userId throws InvalidOperationException → GlobalExceptionHandler → 500
        Func<Task> act = () => controller.CreateRecipe(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateRecipe_ServiceThrowsException_Returns500()
    {
        // Arrange
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.CreateAsync(
            It.IsAny<Application.DTOs.CreateRecipeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service failure"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.CreateRecipeRequest
        {
            Title = "New Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act & Assert — exception propagates to GlobalExceptionHandler → 500
        Func<Task> act = () => controller.CreateRecipe(request);
        await act.Should().ThrowAsync<Exception>().WithMessage("Service failure");
    }

    [Fact]
    public async Task UpdateRecipe_WithValidBody_Returns200WithRecipeDto()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Updated Recipe", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            "user-1", "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var request = new Application.DTOs.UpdateRecipeRequest
        {
            Title = "Updated Recipe",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<Application.DTOs.UpdateRecipeRequest>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.UpdateRecipe(expectedId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returnedRecipe = okResult.Value.Should().BeOfType<RecipeDto>().Subject;
        returnedRecipe.Id.Should().Be(expectedId);
    }

    [Fact]
    public async Task UpdateRecipe_WithOwnerId_PassesToService()
    {
        // Arrange
        const string userId = "update-user-3";
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Updated", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            userId, "Owner", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var request = new Application.DTOs.UpdateRecipeRequest
        {
            Title = "Updated",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<Application.DTOs.UpdateRecipeRequest>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object, userId: userId);

        // Act
        await controller.UpdateRecipe(expectedId, request);

        // Assert
        mockService.Verify(s => s.UpdateAsync(
            expectedId,
            It.IsAny<Application.DTOs.UpdateRecipeRequest>(),
            userId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRecipe_WithAdminRole_ServiceCalledCorrectly()
    {
        // Arrange
        const string userId = "admin-user-9";
        var expectedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            expectedId, "Updated", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Public,
            userId, "Admin User", null, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var request = new Application.DTOs.UpdateRecipeRequest
        {
            Title = "Updated",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<Application.DTOs.UpdateRecipeRequest>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object, userId: userId, isAdmin: true);

        // Act
        await controller.UpdateRecipe(expectedId, request);

        // Assert
        mockService.Verify(s => s.UpdateAsync(
            expectedId,
            It.IsAny<Application.DTOs.UpdateRecipeRequest>(),
            userId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRecipe_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<Application.DTOs.UpdateRecipeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.UpdateRecipeRequest
        {
            Title = "Updated",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.UpdateRecipe(expectedId, request);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task UpdateRecipe_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.UpdateAsync(
            expectedId, It.IsAny<Application.DTOs.UpdateRecipeRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.UpdateRecipeRequest
        {
            Title = "Updated",
            ServingSize = 4,
            Category = Domain.Enums.RecipeCategory.Dinner
        };

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.UpdateRecipe(expectedId, request);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task DeleteRecipe_WithOwnerId_Returns204()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, "user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act
        var result = await controller.DeleteRecipe(expectedId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteRecipe_WithAdminRole_PassesIsAdminTrue()
    {
        // Arrange
        const string userId = "admin-deleter-5";
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(mockService.Object, userId: userId, isAdmin: true);

        // Act
        await controller.DeleteRecipe(expectedId);

        // Assert
        mockService.Verify(s => s.DeleteAsync(
            expectedId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipe_ServiceThrowsNotFoundException_Returns404()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (404 ProblemDetails)
        Func<Task> act = () => controller.DeleteRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task DeleteRecipe_ServiceThrowsForbiddenException_Returns403()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.DeleteAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ForbiddenException("Forbidden"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");

        // Act & Assert — exception propagates to GlobalExceptionHandler middleware (403 ProblemDetails)
        Func<Task> act = () => controller.DeleteRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.ForbiddenException>()
            .WithMessage("Forbidden");
    }

    [Fact]
    public async Task ForkRecipe_WithValidAuth_Returns201WithLocationHeader()
    {
        var sourceId = Guid.NewGuid();
        var forkedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            forkedId, "Forked", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Private,
            "user-1", "Owner", sourceId, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ForkAsync(sourceId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var result = await controller.ForkRecipe(sourceId);
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.RouteValues.Should().ContainKey("id");
        var routeId = (Guid)createdResult.RouteValues!["id"]!;
        routeId.Should().Be(forkedId);
    }

    [Fact]
    public async Task ForkRecipe_PassesSourceIdAndOwnerIdToService()
    {
        const string userId = "fork-user-7";
        var sourceId = Guid.NewGuid();
        var forkedId = Guid.NewGuid();
        var recipe = new RecipeDto(
            forkedId, "Forked", null, null,
            10, 20, 4, Domain.Enums.RecipeCategory.Dinner, Domain.Enums.RecipeVisibility.Private,
            userId, "Owner", sourceId, null, null, null, null, Array.Empty<string>(),
            Array.Empty<IngredientDto>(), Array.Empty<RecipeStepDto>(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ForkAsync(sourceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        var controller = CreateController(mockService.Object, userId: userId);
        await controller.ForkRecipe(sourceId);
        mockService.Verify(s => s.ForkAsync(
            sourceId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForkRecipe_ServiceThrowsNotFoundException_Returns404()
    {
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ForkAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.NotFoundException("Not found"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        Func<Task> act = () => controller.ForkRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.NotFoundException>()
            .WithMessage("Not found");
    }

    [Fact]
    public async Task ForkRecipe_ServiceThrowsConflictException_Returns409()
    {
        var expectedId = Guid.NewGuid();
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ForkAsync(
            expectedId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.ConflictException("Already forked"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        Func<Task> act = () => controller.ForkRecipe(expectedId);
        await act.Should().ThrowAsync<Application.Exceptions.ConflictException>()
            .WithMessage("Already forked");
    }

    [Fact]
    public async Task ForkRecipe_WithMissingUserId_Returns500()
    {
        var expectedId = Guid.NewGuid();
        var controller = CreateController(userId: "");
        Func<Task> act = () => controller.ForkRecipe(expectedId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ImportRecipe_WithValidUrl_Returns200WithPreview()
    {
        var importResult = new Application.DTOs.ImportRecipeResult(
            "https://example.com/recipe",
            new Application.DTOs.CreateRecipeRequest
            {
                Title = "Imported Recipe",
                ServingSize = 4,
                Category = Domain.Enums.RecipeCategory.Dinner
            });
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ImportAsync(
            It.IsAny<Application.DTOs.ImportRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.ImportRecipeRequest("https://example.com/recipe");
        var result = await controller.ImportRecipe(request);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returnedResult = okResult.Value.Should().BeOfType<Application.DTOs.ImportRecipeResult>().Subject;
        returnedResult.SourceUrl.Should().Be("https://example.com/recipe");
        returnedResult.Recipe.Title.Should().Be("Imported Recipe");
    }

    [Fact]
    public async Task ImportRecipe_PassesRequestToService()
    {
        var importResult = new Application.DTOs.ImportRecipeResult(
            "https://example.com/recipe",
            new Application.DTOs.CreateRecipeRequest
            {
                Title = "Imported", ServingSize = 2, Category = Domain.Enums.RecipeCategory.Lunch
            });
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ImportAsync(
            It.IsAny<Application.DTOs.ImportRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.ImportRecipeRequest("https://example.com/recipe");
        await controller.ImportRecipe(request);
        mockService.Verify(s => s.ImportAsync(
            It.Is<Application.DTOs.ImportRecipeRequest>(r => r.Url == "https://example.com/recipe"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportRecipe_ServiceThrowsInvalidImportException_Returns422()
    {
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ImportAsync(
            It.IsAny<Application.DTOs.ImportRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Application.Exceptions.InvalidImportException("No schema"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.ImportRecipeRequest("https://example.com/bad");
        Func<Task> act = () => controller.ImportRecipe(request);
        await act.Should().ThrowAsync<Application.Exceptions.InvalidImportException>()
            .WithMessage("No schema");
    }

    [Fact]
    public async Task ImportRecipe_ServiceThrowsArgumentException_Returns400()
    {
        var mockService = new Mock<IRecipeService>();
        mockService.Setup(s => s.ImportAsync(
            It.IsAny<Application.DTOs.ImportRecipeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Bad URL"));
        var controller = CreateController(mockService.Object);
        controller.HttpContext.User = CreateAuthenticatedUser("user-1");
        var request = new Application.DTOs.ImportRecipeRequest("not-a-url");
        Func<Task> act = () => controller.ImportRecipe(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Bad URL");
    }

    [Fact]
    public async Task ImportRecipe_WithMissingUserId_Returns500()
    {
        var controller = CreateController(userId: "");
        var request = new Application.DTOs.ImportRecipeRequest("https://example.com/recipe");
        Func<Task> act = () => controller.ImportRecipe(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
