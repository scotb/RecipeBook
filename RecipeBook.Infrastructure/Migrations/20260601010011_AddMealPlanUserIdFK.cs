using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanUserIdFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_meal_plans_UserId",
                table: "meal_plans",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_meal_plans_AspNetUsers_UserId",
                table: "meal_plans",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_plans_AspNetUsers_UserId",
                table: "meal_plans");

            migrationBuilder.DropIndex(
                name: "IX_meal_plans_UserId",
                table: "meal_plans");
        }
    }
}
