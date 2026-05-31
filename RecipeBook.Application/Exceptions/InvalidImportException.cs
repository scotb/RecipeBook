namespace RecipeBook.Application.Exceptions;

public sealed class InvalidImportException : Exception
{
    public InvalidImportException(string message) : base(message) { }
}
