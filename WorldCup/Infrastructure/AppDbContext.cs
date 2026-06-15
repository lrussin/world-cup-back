using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WorldCup.Domain.Entities;

namespace WorldCup.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<GroupQualifierBet> GroupQualifierBets => Set<GroupQualifierBet>();
    public DbSet<SpecialBet> SpecialBets => Set<SpecialBet>();
    public DbSet<GroupResult> GroupResults => Set<GroupResult>();
    public DbSet<TournamentResult> TournamentResults => Set<TournamentResult>();
    public DbSet<Settings> Settings => Set<Settings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---------- User ----------
        b.Entity<User>(e =>
        {
            e.Property(u => u.Nome).IsRequired().HasMaxLength(120);
            e.Property(u => u.Email).IsRequired().HasMaxLength(180);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ---------- Team ----------
        b.Entity<Team>(e =>
        {
            e.Property(t => t.Nome).IsRequired().HasMaxLength(80);
            e.Property(t => t.Grupo).IsRequired().HasMaxLength(2);
            e.Property(t => t.CodigoBandeira).HasMaxLength(10);
            e.Property(t => t.FifaCode).HasMaxLength(8);
            e.HasIndex(t => t.Grupo);
        });

        // ---------- Player ----------
        b.Entity<Player>(e =>
        {
            e.Property(p => p.Nome).IsRequired().HasMaxLength(120);
            e.HasOne(p => p.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Match ----------
        b.Entity<Match>(e =>
        {
            e.Property(m => m.Grupo).HasMaxLength(2);
            // Duas FKs para Team na mesma tabela -> Restrict para evitar multiplos caminhos de cascade.
            e.HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => m.DataHoraUtc);
        });

        // ---------- Prediction ----------
        b.Entity<Prediction>(e =>
        {
            // Um palpite por usuario por jogo.
            e.HasIndex(p => new { p.UserId, p.MatchId }).IsUnique();
            e.HasOne(p => p.User)
                .WithMany(u => u.Predictions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Match)
                .WithMany(m => m.Predictions)
                .HasForeignKey(p => p.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- GroupQualifierBet ----------
        b.Entity<GroupQualifierBet>(e =>
        {
            e.Property(g => g.Grupo).IsRequired().HasMaxLength(2);
            // Um palpite por usuario por grupo.
            e.HasIndex(g => new { g.UserId, g.Grupo }).IsUnique();
            e.HasOne(g => g.User)
                .WithMany(u => u.GroupQualifierBets)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.PrimeiroTeam)
                .WithMany()
                .HasForeignKey(g => g.PrimeiroTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.SegundoTeam)
                .WithMany()
                .HasForeignKey(g => g.SegundoTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- SpecialBet ----------
        b.Entity<SpecialBet>(e =>
        {
            // Uma aposta especial por usuario.
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasOne(s => s.User)
                .WithOne(u => u.SpecialBet)
                .HasForeignKey<SpecialBet>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.CampeaoTeam)
                .WithMany()
                .HasForeignKey(s => s.CampeaoTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ArtilheiroPlayer)
                .WithMany()
                .HasForeignKey(s => s.ArtilheiroPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.MelhorJogadorPlayer)
                .WithMany()
                .HasForeignKey(s => s.MelhorJogadorPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- GroupResult ----------
        b.Entity<GroupResult>(e =>
        {
            e.HasKey(g => g.Grupo);
            e.Property(g => g.Grupo).HasMaxLength(2);
            e.HasOne(g => g.PrimeiroTeam)
                .WithMany()
                .HasForeignKey(g => g.PrimeiroTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.SegundoTeam)
                .WithMany()
                .HasForeignKey(g => g.SegundoTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- TournamentResult (singleton, Id = 1) ----------
        b.Entity<TournamentResult>(e =>
        {
            e.HasOne(t => t.CampeaoTeam).WithMany().HasForeignKey(t => t.CampeaoTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ArtilheiroPlayer).WithMany().HasForeignKey(t => t.ArtilheiroPlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.MelhorJogadorPlayer).WithMany().HasForeignKey(t => t.MelhorJogadorPlayerId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Settings (singleton, Id = 1) ----------
        b.Entity<Settings>(e =>
        {
            e.Property(s => s.RegraDesempate).HasMaxLength(400);
        });

        base.OnModelCreating(b);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Trata todo DateTime gravado/lido como UTC. Assim a API serializa com sufixo "Z"
        // e o frontend converte corretamente para o fuso local (ex.: 19:00Z -> 16:00 em Brasilia).
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter() : base(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        { }
    }
}
