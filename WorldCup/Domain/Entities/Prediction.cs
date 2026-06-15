namespace WorldCup.Domain.Entities;

/// <summary>Palpite de placar de um usuario para um jogo (fase de grupos).</summary>
public class Prediction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int MatchId { get; set; }
    public Match? Match { get; set; }

    public int GolsMandantePalpite { get; set; }
    public int GolsVisitantePalpite { get; set; }

    /// <summary>Pontos apurados quando o resultado oficial e lancado (0/3/5).</summary>
    public int PontosObtidos { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
