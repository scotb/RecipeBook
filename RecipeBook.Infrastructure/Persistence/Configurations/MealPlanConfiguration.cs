using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecipeBook.Domain.Entities;
using RecipeBook.Infrastructure.Identity;

namespace RecipeBook.Infrastructure.Persistence.Configurations;

internal sealed class MealEntryConfiguration : IEntityTypeConfiguration<MealEntry>
{
    public void Configure(EntityTypeBuilder<MealEntry> builder)
    {
        builder.ToTable("meal_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.DayOfWeek).IsRequired();
        builder.Property(e => e.MealSlot).IsRequired();
        builder.Property(e => e.ServingCount).IsRequired();
        builder.HasIndex(e => new { e.MealPlanId, e.DayOfWeek, e.MealSlot }).IsUnique();
        builder.HasOne<Recipe>().WithMany().HasForeignKey(e => e.RecipeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("meal_plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.UserId).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Navigation(p => p.Entries).HasField("_entries").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(p => p.Entries)
            .WithOne()
            .HasForeignKey(e => e.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
