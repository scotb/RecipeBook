using RecipeBook.Application.DTOs;

namespace RecipeBook.Application.Interfaces;

public interface IRecipeImporter
{
    // Throws InvalidImportException if no parseable schema.org/Recipe data is found.
    // Throws ArgumentException if the URL is malformed.
    Task<CreateRecipeRequest> ImportFromUrlAsync(string url, CancellationToken ct = default);
}
