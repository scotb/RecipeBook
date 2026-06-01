using RecipeBook.Domain.Entities;
using RecipeBook.Domain.Enums;

namespace RecipeBook.Tests.Shared;

public static class RecipeFactory
{
    public static Recipe CreateValid(
        string ownerId = "test-owner",
        RecipeCategory category = RecipeCategory.Dinner,
        string title = "Test Recipe",
        int servingSize = 4)
        => new Recipe(title, ownerId, servingSize, category);

    public static Recipe CreatePublic(
        string ownerId = "test-owner",
        RecipeCategory category = RecipeCategory.Dinner)
    {
        var recipe = new Recipe("Test Recipe", ownerId, 4, category);
        recipe.SubmitForReview();
        recipe.Approve();
        return recipe;
    }

    public static Recipe CreatePrivate(
        string ownerId = "test-owner",
        RecipeCategory category = RecipeCategory.Dinner)
        => new Recipe("Test Recipe", ownerId, 4, category);

    public static Recipe CreateWithIngredient(
        decimal quantity,
        int servingSize,
        string ownerId = "test-owner")
    {
        var recipe = new Recipe("Test Recipe", ownerId, servingSize, RecipeCategory.Dinner);
        recipe.AddIngredient("flour", quantity);
        return recipe;
    }

    public static Recipe CreateForked(
        string newOwnerId = "fork-owner",
        string sourceOwnerId = "source-owner")
    {
        var source = CreatePublic(sourceOwnerId);
        return source.Fork(newOwnerId);
    }
}
