namespace RecipeBook.Application.Interfaces;

/// <summary>
/// Service responsible for generating signed JWT tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT for the specified user based on their identity information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user (mapped to 'sub' claim).</param>
    /// <param name="email">The user's email address (mapped to 'email' claim).</param>
    /// <param name="displayName">The user's display name (mapped to 'name' claim).</param>
    /// <param name="avatarUrl">The user's avatar URL (mapped to 'picture' claim).</param>
    /// <param name="roles">The roles assigned to the user (mapped to 'role' claims).</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateToken(
        string userId, 
        string email, 
        string displayName, 
        string? avatarUrl, 
        IEnumerable<string> roles);
}
