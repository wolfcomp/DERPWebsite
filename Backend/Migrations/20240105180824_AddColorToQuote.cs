using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDPWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddColorToQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Chance",
                table: "Quotes",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Color",
                table: "Quotes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Quotes");

            migrationBuilder.AlterColumn<double>(
                name: "Chance",
                table: "Quotes",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }
    }
}
