namespace WorldCup.Domain.Entities;

/// <summary>Resultado oficial de classificacao de um grupo (1o e 2o), lancado pelo admin.</summary>
public class GroupResult
{
    /// <summary>Letra do grupo (A..L) — chave primaria.</summary>
    public string Grupo { get; set; } = string.Empty;

    public int PrimeiroTeamId { get; set; }
    public Team? PrimeiroTeam { get; set; }

    public int SegundoTeamId { get; set; }
    public Team? SegundoTeam { get; set; }
}
