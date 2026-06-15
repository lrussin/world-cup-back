using Microsoft.EntityFrameworkCore;
using WorldCup.Domain.Entities;
using WorldCup.Infrastructure;

namespace WorldCup.Services;

/// <summary>
/// Regras de bloqueio (lock):
///  - Palpite de jogo: travado a partir do kickoff (ou quando o jogo e encerrado).
///  - Apostas de classificacao e especiais: travadas no prazo global (Settings.LockBetsAtUtc),
///    ou, se nao definido, no kickoff do primeiro jogo do torneio.
/// </summary>
public interface ILockService
{
    Task<DateTime> GlobalLockUtcAsync();
    Task<bool> ApostasGeraisTravadasAsync();
    bool JogoTravado(Match match);
}

public class LockService : ILockService
{
    private readonly AppDbContext _db;

    public LockService(AppDbContext db) => _db = db;

    public bool JogoTravado(Match match) =>
        match.Encerrado || DateTime.UtcNow >= match.DataHoraUtc;

    public async Task<DateTime> GlobalLockUtcAsync()
    {
        var settings = await _db.Settings.AsNoTracking().FirstOrDefaultAsync();
        if (settings?.LockBetsAtUtc is { } lockAt)
            return lockAt;

        // Sem prazo definido: usa o kickoff do primeiro jogo. Se nao ha jogos, libera (DateTime.MaxValue).
        var hasMatches = await _db.Matches.AnyAsync();
        if (!hasMatches)
            return DateTime.MaxValue;

        return await _db.Matches.MinAsync(m => m.DataHoraUtc);
    }

    public async Task<bool> ApostasGeraisTravadasAsync() =>
        DateTime.UtcNow >= await GlobalLockUtcAsync();
}
