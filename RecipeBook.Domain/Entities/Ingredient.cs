namespace RecipeBook.Domain.Entities;

public sealed class Ingredient
{
#pragma warning disable CS8618
    private Ingredient() { }
#pragma warning restore CS8618

    internal Ingredient(Guid recipeId, string name, decimal? quantity, string? unit, string? notes, int sortOrder)
    {
        Id = Guid.NewGuid();
        RecipeId = recipeId;
        Name = name;
        Quantity = quantity;
        Unit = unit;
        Notes = notes;
        SortOrder = sortOrder;
    }

    public Guid Id { get; }
    public Guid RecipeId { get; }
    public int SortOrder { get; internal set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; }
    public string Name { get; }
    public string? Notes { get; }
}
