using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDPWebsite.Migrations
{
    /// <inheritdoc />
    public partial class ChangeResourceToTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Expansions_ExpansionId",
                table: "Resources");

            migrationBuilder.DropTable(
                name: "Expansions");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ExpansionId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ExpansionId",
                table: "Resources");

            migrationBuilder.AddColumn<Guid>(
                name: "TierId",
                table: "Resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTiers",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TierId",
                table: "Resources",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Tiers_TierId",
                table: "Resources",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Tiers_TierId",
                table: "Resources");

            migrationBuilder.DropTable(
                name: "Tiers");

            migrationBuilder.DropIndex(
                name: "IX_Resources_TierId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "HasTiers",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "ExpansionId",
                table: "Resources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Expansions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expansions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ExpansionId",
                table: "Resources",
                column: "ExpansionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Expansions_ExpansionId",
                table: "Resources",
                column: "ExpansionId",
                principalTable: "Expansions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
