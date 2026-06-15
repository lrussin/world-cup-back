namespace WorldCup.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public Team? Team { get; set; }
}
