namespace RecipeBook.Domain.Entities;

public sealed class Ingredient
{
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
    public decimal? Quantity { get; }
    public string? Unit { get; }
    public string Name { get; }
    public string? Notes { get; }
}
