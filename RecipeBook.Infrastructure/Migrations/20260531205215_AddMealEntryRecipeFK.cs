using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealEntryRecipeFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_meal_entries_RecipeId",
                table: "meal_entries",
                column: "RecipeId");

            migrationBuilder.AddForeignKey(
                name: "FK_meal_entries_recipes_RecipeId",
                table: "meal_entries",
                column: "RecipeId",
                principalTable: "recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_entries_recipes_RecipeId",
                table: "meal_entries");

            migrationBuilder.DropIndex(
                name: "IX_meal_entries_RecipeId",
                table: "meal_entries");
        }
    }
}
