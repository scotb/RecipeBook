namespace RecipeBook.Application.Interfaces;

public interface IUserLookupService
{
    Task<string?> GetDisplayNameAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
        IEnumerable<string> userIds, CancellationToken ct = default);
}
