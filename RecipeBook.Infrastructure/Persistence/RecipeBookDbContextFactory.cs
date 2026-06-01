using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RecipeBook.Infrastructure.Persistence;

public sealed class RecipeBookDbContextFactory : IDesignTimeDbContextFactory<RecipeBookDbContext>
{
    public RecipeBookDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=recipebook_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<RecipeBookDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new RecipeBookDbContext(options);
    }
}
