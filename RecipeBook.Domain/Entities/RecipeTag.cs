namespace RecipeBook.Domain.Entities;

public sealed class RecipeTag
{
    internal RecipeTag(Guid recipeId, string name)
    {
        Id = Guid.NewGuid();
        RecipeId = recipeId;
        Name = name;
    }

    public Guid Id { get; }
    public Guid RecipeId { get; }
    public string Name { get; }
}
