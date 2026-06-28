using WorldCup.Domain.Enums;

namespace WorldCup.Domain.Entities;

public class Match
{
    public int Id { get; set; }

    // Nullable: no mata-mata o confronto pode existir antes dos times serem conhecidos.
    public int? HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }

    public int? AwayTeamId { get; set; }
    public Team? AwayTeam { get; set; }

    public Fase Fase { get; set; } = Fase.Grupos;

    /// <summary>Id do confronto no chaveamento (73..104). Null nos jogos de grupo.</summary>
    public int? Num { get; set; }

    /// <summary>Time que avancou (mata-mata). Pode diferir do placar (penaltis).</summary>
    public int? VencedorTeamId { get; set; }
    public Team? Vencedor { get; set; }

    /// <summary>Letra do grupo (A..L) para jogos da fase de grupos. Vazio no mata-mata.</summary>
    public string Grupo { get; set; } = string.Empty;

    /// <summary>Horario de inicio (kickoff) em UTC. Trava o palpite do jogo.</summary>
    public DateTime DataHoraUtc { get; set; }

    public int? GolsMandante { get; set; }
    public int? GolsVisitante { get; set; }

    /// <summary>Marcado quando o admin lanca o resultado oficial e os pontos sao apurados.</summary>
    public bool Encerrado { get; set; }

    /// <summary>Resultado lancado manualmente pelo admin no app — o seed/import nunca sobrescreve.</summary>
    public bool ResultadoManual { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
}
