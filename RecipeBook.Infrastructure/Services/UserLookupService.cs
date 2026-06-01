using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Application.Interfaces;
using RecipeBook.Infrastructure.Identity;

namespace RecipeBook.Infrastructure.Services;

public sealed class UserLookupService : IUserLookupService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserLookupService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<string?> GetDisplayNameAsync(string userId, CancellationToken ct = default)
    {
        var displayName = await _userManager.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);
        return displayName;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var idList = userIds.ToList();
        var users = await _userManager.Users
            .Where(u => idList.Contains(u.Id) && u.DisplayName != null)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Id, u => u.DisplayName!);
    }
}
