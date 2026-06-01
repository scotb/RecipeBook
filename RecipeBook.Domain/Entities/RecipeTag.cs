namespace RecipeBook.Domain.Entities;

public sealed class RecipeTag
{
#pragma warning disable CS8618
    private RecipeTag() { }
#pragma warning restore CS8618

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
