using RecipeBook.Domain.Enums;

namespace RecipeBook.Domain.Entities;

public sealed class MealPlan
{
    private readonly List<MealEntry> _entries = [];

    public MealPlan(string userId, DateOnly weekStartDate, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("WeekStartDate must be a Monday.", nameof(weekStartDate));

        if (name is not null && name.Length > 100)
            throw new ArgumentException("Name must not exceed 100 characters.", nameof(name));

        Id = Guid.NewGuid();
        UserId = userId;
        WeekStartDate = weekStartDate;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public string UserId { get; }
    public DateOnly WeekStartDate { get; }
    public string? Name { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<MealEntry> Entries => _entries.AsReadOnly();

    public void SetEntry(DayOfWeek day, MealSlot slot, Guid? recipeId, int? servingCount = null)
    {
        if (servingCount.HasValue)
            ArgumentOutOfRangeException.ThrowIfLessThan(servingCount.Value, 1, nameof(servingCount));

        var existing = _entries.FirstOrDefault(e => e.DayOfWeek == day && e.MealSlot == slot);

        if (recipeId is null)
        {
            if (existing is not null)
                _entries.Remove(existing);
            return;
        }

        if (existing is not null)
        {
            existing.RecipeId = recipeId!.Value;
            if (servingCount.HasValue)
                existing.ServingCount = servingCount.Value;
        }
        else
        {
            if (!servingCount.HasValue)
                throw new ArgumentException("servingCount is required when adding a new entry.", nameof(servingCount));

            _entries.Add(new MealEntry(Id, day, slot, recipeId.Value, servingCount.Value));
        }
    }

    public void ClearEntry(DayOfWeek day, MealSlot slot)
    {
        var existing = _entries.FirstOrDefault(e => e.DayOfWeek == day && e.MealSlot == slot);
        if (existing is not null)
            _entries.Remove(existing);
    }

    public void Rename(string? name)
    {
        if (name is not null && name.Length > 100)
            throw new ArgumentException("Name must not exceed 100 characters.", nameof(name));

        Name = name;
    }
}
