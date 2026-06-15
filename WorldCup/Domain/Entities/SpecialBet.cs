namespace WorldCup.Domain.Entities;

/// <summary>Apostas especiais (campeao, artilheiro, melhor jogador). Criadas uma vez e bloqueadas.</summary>
public class SpecialBet
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    // Opcionais: o bolao coleta as categorias (campeao/artilheiro/melhor) em momentos diferentes.
    public int? CampeaoTeamId { get; set; }
    public Team? CampeaoTeam { get; set; }

    public int? ArtilheiroPlayerId { get; set; }
    public Player? ArtilheiroPlayer { get; set; }

    public int? MelhorJogadorPlayerId { get; set; }
    public Player? MelhorJogadorPlayer { get; set; }

    /// <summary>Quando true, nao pode mais ser alterada.</summary>
    public bool Bloqueado { get; set; }

    /// <summary>Pontos apurados no fim da Copa (25/20/15).</summary>
    public int PontosObtidos { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
