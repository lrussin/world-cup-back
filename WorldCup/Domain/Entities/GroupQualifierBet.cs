namespace WorldCup.Domain.Entities;

/// <summary>Palpite de quem passa em 1o e 2o lugar de um grupo. Travado antes do 1o jogo.</summary>
public class GroupQualifierBet
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Letra do grupo (A..L).</summary>
    public string Grupo { get; set; } = string.Empty;

    public int PrimeiroTeamId { get; set; }
    public Team? PrimeiroTeam { get; set; }

    public int SegundoTeamId { get; set; }
    public Team? SegundoTeam { get; set; }

    /// <summary>Pontos apurados ao fim da fase de grupos (3 por acerto de posicao).</summary>
    public int PontosObtidos { get; set; }
}
