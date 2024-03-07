using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDPWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddTierToCategoryMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Tiers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_CategoryId",
                table: "Tiers",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tiers_Categories_CategoryId",
                table: "Tiers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tiers_Categories_CategoryId",
                table: "Tiers");

            migrationBuilder.DropIndex(
                name: "IX_Tiers_CategoryId",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tiers");
        }
    }
}
