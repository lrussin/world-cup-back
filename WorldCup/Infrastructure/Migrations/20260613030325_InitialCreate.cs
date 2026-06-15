using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LockBetsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PontosPlacarExato = table.Column<int>(type: "int", nullable: false),
                    PontosResultado = table.Column<int>(type: "int", nullable: false),
                    PontosClassificacaoPorAcerto = table.Column<int>(type: "int", nullable: false),
                    PontosCampeao = table.Column<int>(type: "int", nullable: false),
                    PontosArtilheiro = table.Column<int>(type: "int", nullable: false),
                    PontosMelhorJogador = table.Column<int>(type: "int", nullable: false),
                    RegraDesempate = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CodigoBandeira = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Pago = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupResults",
                columns: table => new
                {
                    Grupo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PrimeiroTeamId = table.Column<int>(type: "int", nullable: false),
                    SegundoTeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupResults", x => x.Grupo);
                    table.ForeignKey(
                        name: "FK_GroupResults_Teams_PrimeiroTeamId",
                        column: x => x.PrimeiroTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupResults_Teams_SegundoTeamId",
                        column: x => x.SegundoTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    Fase = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GolsMandante = table.Column<int>(type: "int", nullable: true),
                    GolsVisitante = table.Column<int>(type: "int", nullable: true),
                    Encerrado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupQualifierBets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PrimeiroTeamId = table.Column<int>(type: "int", nullable: false),
                    SegundoTeamId = table.Column<int>(type: "int", nullable: false),
                    PontosObtidos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupQualifierBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupQualifierBets_Teams_PrimeiroTeamId",
                        column: x => x.PrimeiroTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupQualifierBets_Teams_SegundoTeamId",
                        column: x => x.SegundoTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupQualifierBets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    GolsMandantePalpite = table.Column<int>(type: "int", nullable: false),
                    GolsVisitantePalpite = table.Column<int>(type: "int", nullable: false),
                    PontosObtidos = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Predictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialBets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CampeaoTeamId = table.Column<int>(type: "int", nullable: false),
                    ArtilheiroPlayerId = table.Column<int>(type: "int", nullable: false),
                    MelhorJogadorPlayerId = table.Column<int>(type: "int", nullable: false),
                    Bloqueado = table.Column<bool>(type: "bit", nullable: false),
                    PontosObtidos = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialBets_Players_ArtilheiroPlayerId",
                        column: x => x.ArtilheiroPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialBets_Players_MelhorJogadorPlayerId",
                        column: x => x.MelhorJogadorPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialBets_Teams_CampeaoTeamId",
                        column: x => x.CampeaoTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialBets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampeaoTeamId = table.Column<int>(type: "int", nullable: true),
                    ArtilheiroPlayerId = table.Column<int>(type: "int", nullable: true),
                    MelhorJogadorPlayerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentResults_Players_ArtilheiroPlayerId",
                        column: x => x.ArtilheiroPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentResults_Players_MelhorJogadorPlayerId",
                        column: x => x.MelhorJogadorPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentResults_Teams_CampeaoTeamId",
                        column: x => x.CampeaoTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupQualifierBets_PrimeiroTeamId",
                table: "GroupQualifierBets",
                column: "PrimeiroTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupQualifierBets_SegundoTeamId",
                table: "GroupQualifierBets",
                column: "SegundoTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupQualifierBets_UserId_Grupo",
                table: "GroupQualifierBets",
                columns: new[] { "UserId", "Grupo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupResults_PrimeiroTeamId",
                table: "GroupResults",
                column: "PrimeiroTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupResults_SegundoTeamId",
                table: "GroupResults",
                column: "SegundoTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DataHoraUtc",
                table: "Matches",
                column: "DataHoraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_MatchId",
                table: "Predictions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId_MatchId",
                table: "Predictions",
                columns: new[] { "UserId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialBets_ArtilheiroPlayerId",
                table: "SpecialBets",
                column: "ArtilheiroPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialBets_CampeaoTeamId",
                table: "SpecialBets",
                column: "CampeaoTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialBets_MelhorJogadorPlayerId",
                table: "SpecialBets",
                column: "MelhorJogadorPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialBets_UserId",
                table: "SpecialBets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Grupo",
                table: "Teams",
                column: "Grupo");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentResults_ArtilheiroPlayerId",
                table: "TournamentResults",
                column: "ArtilheiroPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentResults_CampeaoTeamId",
                table: "TournamentResults",
                column: "CampeaoTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentResults_MelhorJogadorPlayerId",
                table: "TournamentResults",
                column: "MelhorJogadorPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupQualifierBets");

            migrationBuilder.DropTable(
                name: "GroupResults");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "SpecialBets");

            migrationBuilder.DropTable(
                name: "TournamentResults");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
