namespace RecipeBook.Domain.Entities;

public sealed class RecipeStep
{
    internal RecipeStep(Guid recipeId, string body, string? title, int sortOrder)
    {
        Id = Guid.NewGuid();
        RecipeId = recipeId;
        Body = body;
        Title = title;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }
    public Guid RecipeId { get; }
    public int SortOrder { get; internal set; }
    public string? Title { get; }
    public string Body { get; }
}
