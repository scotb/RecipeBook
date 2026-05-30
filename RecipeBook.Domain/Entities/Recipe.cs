using RecipeBook.Domain.Enums;

namespace RecipeBook.Domain.Entities;

public sealed class Recipe
{
    private readonly List<Ingredient> _ingredients = [];
    private readonly List<RecipeStep> _steps = [];
    private readonly List<RecipeTag> _tags = [];

    public Recipe(
        string title,
        string ownerId,
        int servingSize,
        RecipeCategory category,
        string? description = null,
        string? imageUrl = null,
        int? prepTimeMinutes = null,
        int? cookTimeMinutes = null,
        RecipeVisibility visibility = RecipeVisibility.Private,
        decimal? caloriesPerServing = null,
        decimal? proteinGrams = null,
        decimal? carbsGrams = null,
        decimal? fatGrams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentOutOfRangeException.ThrowIfLessThan(servingSize, 1);

        ThrowIfExceedsMaxLength(title, 200, nameof(title));
        ThrowIfExceedsMaxLength(description, 2000, nameof(description));

        if (prepTimeMinutes is not null)
            ArgumentOutOfRangeException.ThrowIfLessThan(prepTimeMinutes.Value, 1, nameof(prepTimeMinutes));

        if (cookTimeMinutes is not null)
            ArgumentOutOfRangeException.ThrowIfLessThan(cookTimeMinutes.Value, 1, nameof(cookTimeMinutes));

        ThrowIfNonPositive(caloriesPerServing, nameof(caloriesPerServing));
        ThrowIfNonPositive(proteinGrams, nameof(proteinGrams));
        ThrowIfNonPositive(carbsGrams, nameof(carbsGrams));
        ThrowIfNonPositive(fatGrams, nameof(fatGrams));

        if (imageUrl is not null &&
            (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ArgumentException("ImageUrl must be a valid absolute HTTP or HTTPS URL.", nameof(imageUrl));

        Id = Guid.NewGuid();
        Title = title;
        OwnerId = ownerId;
        ServingSize = servingSize;
        Category = category;
        Description = description;
        ImageUrl = imageUrl;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Visibility = visibility;
        CaloriesPerServing = caloriesPerServing;
        ProteinGrams = proteinGrams;
        CarbsGrams = carbsGrams;
        FatGrams = fatGrams;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public int? PrepTimeMinutes { get; private set; }
    public int? CookTimeMinutes { get; private set; }
    public int ServingSize { get; private set; }
    public RecipeCategory Category { get; private set; }
    public RecipeVisibility Visibility { get; private set; }
    public string OwnerId { get; }
    public Guid? SourceRecipeId { get; private set; }
    public decimal? CaloriesPerServing { get; private set; }
    public decimal? ProteinGrams { get; private set; }
    public decimal? CarbsGrams { get; private set; }
    public decimal? FatGrams { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyList<Ingredient> Ingredients => _ingredients.AsReadOnly();
    public IReadOnlyList<RecipeStep> Steps => _steps.AsReadOnly();
    public IReadOnlyList<RecipeTag> Tags => _tags.AsReadOnly();

    public void AddIngredient(string name, decimal? quantity = null, string? unit = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfExceedsMaxLength(name, 200, nameof(name));
        ThrowIfExceedsMaxLength(unit, 50, nameof(unit));
        ThrowIfExceedsMaxLength(notes, 500, nameof(notes));

        var ingredient = new Ingredient(Id, name, quantity, unit, notes, _ingredients.Count);
        _ingredients.Add(ingredient);
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        var ingredient = _ingredients.FirstOrDefault(i => i.Id == ingredientId)
            ?? throw new InvalidOperationException($"Ingredient '{ingredientId}' not found on this recipe.");

        _ingredients.Remove(ingredient);

        for (var i = 0; i < _ingredients.Count; i++)
            _ingredients[i].SortOrder = i;
    }

    public void AddStep(string body, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ThrowIfExceedsMaxLength(body, 5000, nameof(body));
        ThrowIfExceedsMaxLength(title, 200, nameof(title));

        var step = new RecipeStep(Id, body, title, _steps.Count);
        _steps.Add(step);
    }

    public void ReorderSteps(IEnumerable<Guid> orderedStepIds)
    {
        var ids = orderedStepIds.ToList();
        var existingIds = _steps.Select(s => s.Id).ToHashSet();

        if (ids.Count != existingIds.Count || ids.Any(id => !existingIds.Contains(id)))
            throw new ArgumentException("The provided step IDs must exactly match the current step set.", nameof(orderedStepIds));

        for (var i = 0; i < ids.Count; i++)
            _steps.First(s => s.Id == ids[i]).SortOrder = i;
    }

    public void AddTag(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = NormalizeTag(name);
        ThrowIfExceedsMaxLength(normalized, 100, nameof(name));

        if (_tags.Any(t => t.Name == normalized))
            return;

        _tags.Add(new RecipeTag(Id, normalized));
    }

    public void RemoveTag(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = NormalizeTag(name);
        var tag = _tags.FirstOrDefault(t => t.Name == normalized);

        if (tag is not null)
            _tags.Remove(tag);
    }

    public void SubmitForReview()
    {
        if (Visibility == RecipeVisibility.Private)
            Visibility = RecipeVisibility.PendingReview;
    }

    public void Approve()
    {
        Visibility = RecipeVisibility.Public;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Visibility = RecipeVisibility.Private;
        RejectionReason = reason;
    }

    public void MakePrivate()
    {
        Visibility = RecipeVisibility.Private;
    }

    public Recipe Fork(string newOwnerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newOwnerId);

        var fork = new Recipe(
            title: Title,
            ownerId: newOwnerId,
            servingSize: ServingSize,
            category: Category,
            description: Description,
            imageUrl: ImageUrl,
            prepTimeMinutes: PrepTimeMinutes,
            cookTimeMinutes: CookTimeMinutes,
            visibility: RecipeVisibility.Private,
            caloriesPerServing: CaloriesPerServing,
            proteinGrams: ProteinGrams,
            carbsGrams: CarbsGrams,
            fatGrams: FatGrams);

        fork.SourceRecipeId = Id;

        foreach (var i in _ingredients)
            fork._ingredients.Add(new Ingredient(fork.Id, i.Name, i.Quantity, i.Unit, i.Notes, i.SortOrder));

        foreach (var s in _steps)
            fork._steps.Add(new RecipeStep(fork.Id, s.Body, s.Title, s.SortOrder));

        foreach (var t in _tags)
            fork._tags.Add(new RecipeTag(fork.Id, t.Name));

        return fork;
    }

    private static void ThrowIfExceedsMaxLength(string? value, int maxLength, string paramName)
    {
        if (value is not null && value.Length > maxLength)
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", paramName);
    }

    private static void ThrowIfNonPositive(decimal? value, string paramName)
    {
        if (value is not null)
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value.Value, 0m, paramName);
    }

    private static string NormalizeTag(string name) => name.Trim().ToLowerInvariant();
}
