namespace RecipeBook.Domain.Entities;

public sealed class RecipeStep
{
#pragma warning disable CS8618
    private RecipeStep() { }
#pragma warning restore CS8618

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
