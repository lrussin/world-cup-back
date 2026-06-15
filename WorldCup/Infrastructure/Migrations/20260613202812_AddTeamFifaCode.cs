using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamFifaCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FifaCode",
                table: "Teams",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FifaCode",
                table: "Teams");
        }
    }
}
