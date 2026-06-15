using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchResultadoManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResultadoManual",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultadoManual",
                table: "Matches");
        }
    }
}
