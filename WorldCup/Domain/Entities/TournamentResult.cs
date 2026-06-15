namespace WorldCup.Domain.Entities;

/// <summary>Resultado final do torneio (linha unica, Id = 1). Preenchido no fim pelo admin.</summary>
public class TournamentResult
{
    public int Id { get; set; }

    public int? CampeaoTeamId { get; set; }
    public Team? CampeaoTeam { get; set; }

    public int? ArtilheiroPlayerId { get; set; }
    public Player? ArtilheiroPlayer { get; set; }

    public int? MelhorJogadorPlayerId { get; set; }
    public Player? MelhorJogadorPlayer { get; set; }
}
