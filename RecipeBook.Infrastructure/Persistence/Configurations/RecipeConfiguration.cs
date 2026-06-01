using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Identity;

namespace RecipeBook.Infrastructure.Persistence.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Unit).HasMaxLength(50);
        builder.Property(i => i.Notes).HasMaxLength(500);
    }
}

public sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("recipe_steps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Body).IsRequired().HasMaxLength(5000);
        builder.Property(s => s.Title).HasMaxLength(200);
    }
}

public sealed class RecipeTagConfiguration : IEntityTypeConfiguration<RecipeTag>
{
    public void Configure(EntityTypeBuilder<RecipeTag> builder)
    {
        builder.ToTable("recipe_tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => new { t.RecipeId, t.Name }).IsUnique();
    }
}

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.OwnerId).IsRequired();
        builder.Property(r => r.ImageUrl).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);

        builder.Navigation(r => r.Ingredients).HasField("_ingredients").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Steps).WithOne().HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Tags).WithOne().HasForeignKey(t => t.RecipeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(r => r.SourceRecipeId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.OwnerId, r.Visibility });
        builder.HasIndex(r => r.Visibility);
        builder.HasIndex(r => r.SourceRecipeId);
    }
}
