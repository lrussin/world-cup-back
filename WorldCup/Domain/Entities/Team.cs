namespace WorldCup.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Letra do grupo: A..L.</summary>
    public string Grupo { get; set; } = string.Empty;

    /// <summary>Codigo de bandeira (ex.: br, ar, gb-eng) usado pelo frontend para exibir a bandeira.</summary>
    public string CodigoBandeira { get; set; } = string.Empty;

    /// <summary>Codigo FIFA/TLA de 3 letras (ex.: BRA, MEX) — usado para casar com a API de placar ao vivo.</summary>
    public string? FifaCode { get; set; }

    public ICollection<Player> Players { get; set; } = new List<Player>();
}
