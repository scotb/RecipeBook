using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Identity;

namespace RecipeBook.Infrastructure.Persistence;

public sealed class RecipeBookDbContext : IdentityDbContext<ApplicationUser>
{
    public RecipeBookDbContext(DbContextOptions<RecipeBookDbContext> options) : base(options) { }

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(RecipeBookDbContext).Assembly);
    }
}
