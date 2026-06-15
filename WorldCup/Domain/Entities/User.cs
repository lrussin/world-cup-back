namespace WorldCup.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash seguro da senha (BCrypt). Nunca armazenamos a senha em texto puro.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    /// <summary>Controlado pelo admin. So participantes pagos entram no ranking e concorrem ao premio.</summary>
    public bool Pago { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    public ICollection<GroupQualifierBet> GroupQualifierBets { get; set; } = new List<GroupQualifierBet>();
    public SpecialBet? SpecialBet { get; set; }
}
