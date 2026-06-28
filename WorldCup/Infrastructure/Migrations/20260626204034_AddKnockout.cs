using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "HomeTeamId",
                table: "Matches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AwayTeamId",
                table: "Matches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Num",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VencedorTeamId",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_VencedorTeamId",
                table: "Matches",
                column: "VencedorTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_VencedorTeamId",
                table: "Matches",
                column: "VencedorTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_VencedorTeamId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_VencedorTeamId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Num",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "VencedorTeamId",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "HomeTeamId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AwayTeamId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
