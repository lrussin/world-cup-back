namespace WorldCup.Domain.Entities;

/// <summary>
/// Configuracao global do bolao (linha unica, Id = 1). Inclui o ScoringConfig (valores de pontos
/// ajustaveis sem recompilar), a trava global de apostas e a regra de desempate.
/// </summary>
public class Settings
{
    public int Id { get; set; }

    /// <summary>
    /// Prazo de bloqueio dos palpites de classificacao e especiais.
    /// Se null, usa o kickoff do primeiro jogo do torneio.
    /// </summary>
    public DateTime? LockBetsAtUtc { get; set; }

    // ----- ScoringConfig (pontuacao configuravel) -----
    public int PontosPlacarExato { get; set; } = 5;
    public int PontosResultado { get; set; } = 3;
    public int PontosClassificacaoPorAcerto { get; set; } = 3;
    public int PontosCampeao { get; set; } = 25;
    public int PontosArtilheiro { get; set; } = 20;
    public int PontosMelhorJogador { get; set; } = 15;

    /// <summary>Regra de desempate exibida no ranking (configuravel pelo admin).</summary>
    public string RegraDesempate { get; set; } =
        "Maior numero de placares exatos; persistindo, quem cadastrou o palpite primeiro.";
}
