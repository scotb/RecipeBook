namespace RecipeBook.Application.Interfaces;

public interface IUserContext
{
    string UserId { get; }
    bool IsInRole(string role);
}
