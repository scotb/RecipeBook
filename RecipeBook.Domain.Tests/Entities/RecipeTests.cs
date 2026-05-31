using FluentAssertions;
using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Domain.Tests.Entities;

public class RecipeTests
{
    [Fact]
    public void Constructor_WithRequiredArguments_CreatesRecipeWithCorrectDefaults()
    {
        var before = DateTimeOffset.UtcNow;

        var recipe = new Recipe(
            title: "Chocolate Chip Cookies",
            ownerId: "user-123",
            servingSize: 24,
            category: RecipeCategory.Dessert);

        var after = DateTimeOffset.UtcNow;

        recipe.Id.Should().NotBe(Guid.Empty);
        recipe.Title.Should().Be("Chocolate Chip Cookies");
        recipe.OwnerId.Should().Be("user-123");
        recipe.ServingSize.Should().Be(24);
        recipe.Category.Should().Be(RecipeCategory.Dessert);
        recipe.Visibility.Should().Be(RecipeVisibility.Private);
        recipe.Description.Should().BeNull();
        recipe.ImageUrl.Should().BeNull();
        recipe.PrepTimeMinutes.Should().BeNull();
        recipe.CookTimeMinutes.Should().BeNull();
        recipe.SourceRecipeId.Should().BeNull();
        recipe.CaloriesPerServing.Should().BeNull();
        recipe.ProteinGrams.Should().BeNull();
        recipe.CarbsGrams.Should().BeNull();
        recipe.FatGrams.Should().BeNull();
        recipe.Ingredients.Should().BeEmpty();
        recipe.Steps.Should().BeEmpty();
        recipe.Tags.Should().BeEmpty();
        recipe.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        recipe.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceTitle_ThrowsArgumentException(string? title)
    {
        var act = () => new Recipe(title!, ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceOwnerId_ThrowsArgumentException(string? ownerId)
    {
        var act = () => new Recipe("Valid Title", ownerId: ownerId!, servingSize: 1, category: RecipeCategory.Dessert);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithServingSizeLessThanOne_ThrowsArgumentOutOfRangeException(int servingSize)
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: servingSize, category: RecipeCategory.Dessert);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithTitleExceedingMaxLength_ThrowsArgumentException()
    {
        var title = new string('x', 201);

        var act = () => new Recipe(title, ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithDescriptionExceedingMaxLength_ThrowsArgumentException()
    {
        var description = new string('x', 2001);

        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, description: description);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositivePrepTimeMinutes_ThrowsArgumentOutOfRangeException(int prepTimeMinutes)
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, prepTimeMinutes: prepTimeMinutes);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveCookTimeMinutes_ThrowsArgumentOutOfRangeException(int cookTimeMinutes)
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, cookTimeMinutes: cookTimeMinutes);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(nameof(Recipe.CaloriesPerServing))]
    [InlineData(nameof(Recipe.ProteinGrams))]
    [InlineData(nameof(Recipe.CarbsGrams))]
    [InlineData(nameof(Recipe.FatGrams))]
    public void Constructor_WithNonPositiveNutritionalValue_ThrowsArgumentOutOfRangeException(string propertyName)
    {
        Action act = propertyName switch
        {
            nameof(Recipe.CaloriesPerServing) => () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, caloriesPerServing: -1m),
            nameof(Recipe.ProteinGrams)       => () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, proteinGrams: 0m),
            nameof(Recipe.CarbsGrams)         => () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, carbsGrams: -0.5m),
            nameof(Recipe.FatGrams)           => () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, fatGrams: -1m),
            _                                 => throw new InvalidOperationException("Unexpected property")
        };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("/relative/path.jpg")]
    public void Constructor_WithInvalidImageUrl_ThrowsArgumentException(string imageUrl)
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, imageUrl: imageUrl);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("http://example.com/image.jpg")]
    public void Constructor_WithValidImageUrl_DoesNotThrow(string imageUrl)
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, imageUrl: imageUrl);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullImageUrl_DoesNotThrow()
    {
        var act = () => new Recipe("Valid Title", ownerId: "user-123", servingSize: 1, category: RecipeCategory.Dessert, imageUrl: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddIngredient_WithValidName_AppearsInIngredients()
    {
        var recipe = BuildRecipe();

        recipe.AddIngredient("all-purpose flour");

        recipe.Ingredients.Should().ContainSingle(i => i.Name == "all-purpose flour");
    }

    [Fact]
    public void AddIngredient_MultipleIngredients_SortOrderIsZeroBased()
    {
        var recipe = BuildRecipe();

        recipe.AddIngredient("flour");
        recipe.AddIngredient("sugar");
        recipe.AddIngredient("butter");

        recipe.Ingredients.Select(i => i.SortOrder).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void AddIngredient_WithOptionalFields_StoredCorrectly()
    {
        var recipe = BuildRecipe();

        recipe.AddIngredient("butter", quantity: 2.5m, unit: "cups", notes: "room temperature");

        var ingredient = recipe.Ingredients.Single();
        ingredient.Quantity.Should().Be(2.5m);
        ingredient.Unit.Should().Be("cups");
        ingredient.Notes.Should().Be("room temperature");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddIngredient_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddIngredient(name!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddIngredient_WithNameExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddIngredient(new string('x', 201));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddIngredient_WithUnitExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddIngredient("flour", unit: new string('x', 51));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddIngredient_WithNotesExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddIngredient("flour", notes: new string('x', 501));

        act.Should().Throw<ArgumentException>();
    }

    private static Recipe BuildRecipe() =>
        new("Chocolate Chip Cookies", ownerId: "user-123", servingSize: 24, category: RecipeCategory.Dessert);

    [Fact]
    public void RemoveIngredient_WithExistingId_RemovesIngredient()
    {
        var recipe = BuildRecipe();
        recipe.AddIngredient("flour");
        recipe.AddIngredient("sugar");
        var targetId = recipe.Ingredients[0].Id;

        recipe.RemoveIngredient(targetId);

        recipe.Ingredients.Should().NotContain(i => i.Id == targetId);
        recipe.Ingredients.Should().ContainSingle(i => i.Name == "sugar");
    }

    [Fact]
    public void RemoveIngredient_AfterRemoval_SortOrdersAreCompacted()
    {
        var recipe = BuildRecipe();
        recipe.AddIngredient("flour");
        recipe.AddIngredient("sugar");
        recipe.AddIngredient("butter");
        var middleId = recipe.Ingredients[1].Id;

        recipe.RemoveIngredient(middleId);

        recipe.Ingredients.Select(i => i.SortOrder).Should().Equal(0, 1);
    }

    [Fact]
    public void RemoveIngredient_WithUnknownId_ThrowsInvalidOperationException()
    {
        var recipe = BuildRecipe();
        recipe.AddIngredient("flour");

        var act = () => recipe.RemoveIngredient(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddStep_WithValidBody_AppearsInSteps()
    {
        var recipe = BuildRecipe();

        recipe.AddStep("Mix dry ingredients.");

        recipe.Steps.Should().ContainSingle(s => s.Body == "Mix dry ingredients.");
    }

    [Fact]
    public void AddStep_MultipleSteps_SortOrderIsZeroBased()
    {
        var recipe = BuildRecipe();

        recipe.AddStep("Step one.");
        recipe.AddStep("Step two.");
        recipe.AddStep("Step three.");

        recipe.Steps.Select(s => s.SortOrder).Should().Equal(0, 1, 2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddStep_WithNullOrWhitespaceBody_ThrowsArgumentException(string? body)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddStep(body!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddStep_WithBodyExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddStep(new string('x', 5001));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddStep_WithTitleExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddStep("Valid body.", title: new string('x', 201));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReorderSteps_WithValidIds_UpdatesSortOrder()
    {
        var recipe = BuildRecipe();
        recipe.AddStep("Step one.");
        recipe.AddStep("Step two.");
        recipe.AddStep("Step three.");
        var ids = recipe.Steps.Select(s => s.Id).ToList();
        var reversed = ids.AsEnumerable().Reverse();

        recipe.ReorderSteps(reversed);

        recipe.Steps.OrderBy(s => s.SortOrder).Select(s => s.Id).Should().Equal(ids[2], ids[1], ids[0]);
    }

    [Fact]
    public void ReorderSteps_WithMissingId_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();
        recipe.AddStep("Step one.");
        recipe.AddStep("Step two.");

        var act = () => recipe.ReorderSteps([recipe.Steps[0].Id, Guid.NewGuid()]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReorderSteps_WithExtraId_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();
        recipe.AddStep("Step one.");

        var act = () => recipe.ReorderSteps([recipe.Steps[0].Id, Guid.NewGuid()]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTag_WithValidName_AppearsInTags()
    {
        var recipe = BuildRecipe();

        recipe.AddTag("vegetarian");

        recipe.Tags.Should().ContainSingle(t => t.Name == "vegetarian");
    }

    [Fact]
    public void AddTag_NormalizesNameToLowercaseTrimmed()
    {
        var recipe = BuildRecipe();

        recipe.AddTag("  Gluten-Free  ");

        recipe.Tags.Should().ContainSingle(t => t.Name == "gluten-free");
    }

    [Fact]
    public void AddTag_DuplicateName_OnlyOneTagStored()
    {
        var recipe = BuildRecipe();

        recipe.AddTag("vegan");
        recipe.AddTag("VEGAN");

        recipe.Tags.Should().ContainSingle(t => t.Name == "vegan");
    }

    [Fact]
    public void AddTag_WithNameExceedingMaxLength_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();

        var act = () => recipe.AddTag(new string('x', 101));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveTag_WithExistingName_RemovesTag()
    {
        var recipe = BuildRecipe();
        recipe.AddTag("vegan");

        recipe.RemoveTag("vegan");

        recipe.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_IsCaseInsensitive()
    {
        var recipe = BuildRecipe();
        recipe.AddTag("vegan");

        recipe.RemoveTag("VEGAN");

        recipe.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_WithNonExistentName_IsNoOp()
    {
        var recipe = BuildRecipe();
        recipe.AddTag("vegan");

        recipe.RemoveTag("keto");

        recipe.Tags.Should().ContainSingle();
    }

    [Fact]
    public void SubmitForReview_FromPrivate_SetsVisibilityToPendingReview()
    {
        var recipe = BuildRecipe();

        recipe.SubmitForReview();

        recipe.Visibility.Should().Be(RecipeVisibility.PendingReview);
    }

    [Fact]
    public void SubmitForReview_WhenAlreadyPending_IsNoOp()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();

        recipe.SubmitForReview(); // second call

        recipe.Visibility.Should().Be(RecipeVisibility.PendingReview);
    }

    [Fact]
    public void Approve_FromPendingReview_SetsVisibilityToPublic()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();

        recipe.Approve();

        recipe.Visibility.Should().Be(RecipeVisibility.Public);
    }

    [Fact]
    public void Reject_FromPendingReview_SetsVisibilityToPrivateWithReason()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();

        recipe.Reject("Contains inappropriate content.");

        recipe.Visibility.Should().Be(RecipeVisibility.Private);
        recipe.RejectionReason.Should().Be("Contains inappropriate content.");
    }

    [Fact]
    public void SubmitForReview_FromPublic_SetsVisibilityToPendingReview()
    {
        // Arrange
        var recipe = BuildRecipe();
        recipe.SubmitForReview();
        recipe.Approve(); // now public

        // Act
        recipe.SubmitForReview();

        // Assert
        recipe.Visibility.Should().Be(RecipeVisibility.PendingReview);
    }

    [Fact]
    public void SubmitForReview_FromPublic_UpdatesUpdatedAt()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();
        recipe.Approve(); // now public

        var before = recipe.UpdatedAt;

        recipe.SubmitForReview();

        recipe.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Approve_WhenNotPending_ThrowsInvalidOperationException()
    {
        var recipe = BuildRecipe();

        Action act = () => recipe.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_WhenNotPending_ThrowsInvalidOperationException()
    {
        var recipe = BuildRecipe();

        Action act = () => recipe.Reject("nope");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_ClearsRejectionReason()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();
        recipe.Reject("Spam.");
        recipe.SubmitForReview();

        recipe.Approve();

        recipe.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void MakePrivate_FromPublic_SetsVisibilityToPrivate()
    {
        var recipe = BuildRecipe();
        recipe.SubmitForReview();
        recipe.Approve();

        recipe.MakePrivate();

        recipe.Visibility.Should().Be(RecipeVisibility.Private);
    }

    [Fact]
    public void MakePrivate_WhenAlreadyPrivate_IsNoOp()
    {
        var recipe = BuildRecipe();

        recipe.MakePrivate();

        recipe.Visibility.Should().Be(RecipeVisibility.Private);
    }

    [Fact]
    public void Fork_ProducesNewRecipeWithDifferentId()
    {
        var original = BuildRecipe();

        var fork = original.Fork("user-456");

        fork.Id.Should().NotBe(original.Id);
    }

    [Fact]
    public void Fork_SetsSourceRecipeIdAndNewOwner()
    {
        var original = BuildRecipe();

        var fork = original.Fork("user-456");

        fork.SourceRecipeId.Should().Be(original.Id);
        fork.OwnerId.Should().Be("user-456");
        fork.Visibility.Should().Be(RecipeVisibility.Private);
    }

    [Fact]
    public void Fork_CopiesScalarFields()
    {
        var original = BuildRecipe();

        var fork = original.Fork("user-456");

        fork.Title.Should().Be(original.Title);
        fork.Category.Should().Be(original.Category);
        fork.ServingSize.Should().Be(original.ServingSize);
    }

    [Fact]
    public void Fork_DeepCopiesChildCollections()
    {
        var original = BuildRecipe();
        original.AddIngredient("flour");
        original.AddStep("Mix ingredients.");
        original.AddTag("vegan");

        var fork = original.Fork("user-456");

        fork.Ingredients.Should().HaveCount(1);
        fork.Steps.Should().HaveCount(1);
        fork.Tags.Should().HaveCount(1);

        // Mutating the fork must not affect the original
        fork.AddIngredient("sugar");
        original.Ingredients.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fork_WithNullOrWhitespaceNewOwnerId_ThrowsArgumentException(string? newOwnerId)
    {
        var original = BuildRecipe();

        var act = () => original.Fork(newOwnerId!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Mutations_UpdateUpdatedAt()
    {
        var recipe = BuildRecipe();

        var before = recipe.UpdatedAt;
        recipe.AddIngredient("salt");
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        var ingId = recipe.Ingredients[0].Id;
        recipe.RemoveIngredient(ingId);
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        recipe.AddStep("Stir.");
        recipe.UpdatedAt.Should().BeAfter(before);

        recipe.AddStep("Bake.");
        var ids = recipe.Steps.Select(s => s.Id).ToList();
        before = recipe.UpdatedAt;
        recipe.ReorderSteps(ids.AsEnumerable().Reverse());
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        recipe.AddTag("newtag");
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        recipe.RemoveTag("newtag");
        recipe.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void VisibilityTransitions_UpdateUpdatedAt_And_NoOpDoesNot()
    {
        var recipe = BuildRecipe();

        var before = recipe.UpdatedAt;
        recipe.SubmitForReview();
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        // no-op
        recipe.SubmitForReview();
        recipe.UpdatedAt.Should().Be(before);

        recipe.Approve();
        recipe.UpdatedAt.Should().BeAfter(before);

        // To reject, recipe must be pending review again
        before = recipe.UpdatedAt;
        recipe.SubmitForReview();
        recipe.UpdatedAt.Should().BeAfter(before);

        before = recipe.UpdatedAt;
        recipe.Reject("Bad content");
        recipe.UpdatedAt.Should().BeAfter(before);

        // MakePrivate when already private is a no-op
        before = recipe.UpdatedAt;
        recipe.MakePrivate();
        recipe.UpdatedAt.Should().Be(before);
    }

    [Fact]
    public void Update_WithValidArguments_SetsPropertiesAndTouchesUpdatedAt()
    {
        var recipe = BuildRecipe();
        var before = recipe.UpdatedAt;

        recipe.Update(
            title: "Updated Title",
            servingSize: 2,
            category: RecipeCategory.Lunch);

        recipe.Title.Should().Be("Updated Title");
        recipe.ServingSize.Should().Be(2);
        recipe.Category.Should().Be(RecipeCategory.Lunch);
        recipe.UpdatedAt.Should().BeAfter(before);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithNullOrWhitespaceTitle_ThrowsArgumentException(string? title)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.Update(title!, servingSize: 1, category: RecipeCategory.Dinner);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WithServingSizeLessThanOne_ThrowsArgumentOutOfRangeException(int servingSize)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.Update("Title", servingSize: servingSize, category: RecipeCategory.Dinner);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_WithTitleExceeding200Chars_ThrowsArgumentException()
    {
        var recipe = BuildRecipe();
        var longTitle = new string('x', 201);

        var act = () => recipe.Update(longTitle, servingSize: 1, category: RecipeCategory.Dinner);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WithNonPositivePrepTimeMinutes_ThrowsArgumentOutOfRangeException(int prepTime)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.Update("Title", servingSize: 1, category: RecipeCategory.Dinner, prepTimeMinutes: prepTime);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WithNonPositiveCookTimeMinutes_ThrowsArgumentOutOfRangeException(int cookTime)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.Update("Title", servingSize: 1, category: RecipeCategory.Dinner, cookTimeMinutes: cookTime);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(nameof(Recipe.CaloriesPerServing))]
    [InlineData(nameof(Recipe.ProteinGrams))]
    [InlineData(nameof(Recipe.CarbsGrams))]
    [InlineData(nameof(Recipe.FatGrams))]
    public void Update_WithNonPositiveNutritionalValue_ThrowsArgumentOutOfRangeException(string propertyName)
    {
        var recipe = BuildRecipe();
        Action act = propertyName switch
        {
            nameof(Recipe.CaloriesPerServing) => () => recipe.Update("Title", 1, RecipeCategory.Dinner, caloriesPerServing: -1m),
            nameof(Recipe.ProteinGrams)       => () => recipe.Update("Title", 1, RecipeCategory.Dinner, proteinGrams: 0m),
            nameof(Recipe.CarbsGrams)         => () => recipe.Update("Title", 1, RecipeCategory.Dinner, carbsGrams: -0.5m),
            nameof(Recipe.FatGrams)           => () => recipe.Update("Title", 1, RecipeCategory.Dinner, fatGrams: -1m),
            _                                 => throw new InvalidOperationException("Unexpected property")
        };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("/relative/path.jpg")]
    public void Update_WithInvalidImageUrl_ThrowsArgumentException(string imageUrl)
    {
        var recipe = BuildRecipe();

        var act = () => recipe.Update("Title", servingSize: 1, category: RecipeCategory.Dinner, imageUrl: imageUrl);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNullOptionalFields_ClearsThoseFields()
    {
        var recipe = BuildRecipe();

        recipe.Update(
            title: "Updated",
            servingSize: 1,
            category: RecipeCategory.Dinner,
            description: null,
            imageUrl: null,
            prepTimeMinutes: null,
            cookTimeMinutes: null,
            caloriesPerServing: null,
            proteinGrams: null,
            carbsGrams: null,
            fatGrams: null);

        recipe.Description.Should().BeNull();
        recipe.ImageUrl.Should().BeNull();
        recipe.PrepTimeMinutes.Should().BeNull();
        recipe.CookTimeMinutes.Should().BeNull();
        recipe.CaloriesPerServing.Should().BeNull();
        recipe.ProteinGrams.Should().BeNull();
        recipe.CarbsGrams.Should().BeNull();
        recipe.FatGrams.Should().BeNull();
    }
}
