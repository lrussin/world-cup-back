using Microsoft.EntityFrameworkCore;
using WorldCup.Dtos;
using WorldCup.Infrastructure;

namespace WorldCup.Services;

/// <summary>
/// Monta o ranking somando, por participante PAGO: pontos de jogos + classificacao + especiais.
/// Ordena por pontuacao total e aplica o desempate (maior numero de placares exatos; depois,
/// quem se cadastrou primeiro). O 1o colocado e o vencedor unico.
/// </summary>
public interface IRankingService
{
    Task<RankingDto> GetRankingAsync();
    Task<Dictionary<int, int>> GetTotaisPorUsuarioAsync();
}

public class RankingService : IRankingService
{
    private readonly AppDbContext _db;

    public RankingService(AppDbContext db) => _db = db;

    public async Task<RankingDto> GetRankingAsync()
    {
        var settings = await _db.Settings.AsNoTracking().FirstOrDefaultAsync();
        var pontosPlacarExato = settings?.PontosPlacarExato ?? 5;
        var regra = settings?.RegraDesempate ?? string.Empty;

        // Apenas participantes pagos concorrem.
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Pago)
            .Select(u => new { u.Id, u.Nome, u.CriadoEm })
            .ToListAsync();

        var jogos = await _db.Predictions.AsNoTracking()
            .GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Pontos = g.Sum(x => x.PontosObtidos),
                Exatos = g.Count(x => x.PontosObtidos == pontosPlacarExato)
            })
            .ToDictionaryAsync(x => x.UserId);

        var classificacao = await _db.GroupQualifierBets.AsNoTracking()
            .GroupBy(g => g.UserId)
            .Select(g => new { UserId = g.Key, Pontos = g.Sum(x => x.PontosObtidos) })
            .ToDictionaryAsync(x => x.UserId, x => x.Pontos);

        var especiais = await _db.SpecialBets.AsNoTracking()
            .ToDictionaryAsync(s => s.UserId, s => s.PontosObtidos);

        var linhas = users.Select(u =>
        {
            var pj = jogos.TryGetValue(u.Id, out var j) ? j.Pontos : 0;
            var exatos = jogos.TryGetValue(u.Id, out var j2) ? j2.Exatos : 0;
            var pc = classificacao.TryGetValue(u.Id, out var c) ? c : 0;
            var pe = especiais.TryGetValue(u.Id, out var e) ? e : 0;
            return new
            {
                u.Id,
                u.Nome,
                u.CriadoEm,
                PontosJogos = pj,
                PontosClassificacao = pc,
                PontosEspeciais = pe,
                Total = pj + pc + pe,
                Exatos = exatos
            };
        })
        .OrderByDescending(x => x.Total)
        .ThenByDescending(x => x.Exatos)   // desempate 1: mais placares exatos
        .ThenBy(x => x.CriadoEm)            // desempate 2: cadastro mais antigo
        .ToList();

        var entries = linhas.Select((x, i) => new RankingEntryDto(
            Posicao: i + 1,
            UserId: x.Id,
            Nome: x.Nome,
            PontosJogos: x.PontosJogos,
            PontosClassificacao: x.PontosClassificacao,
            PontosEspeciais: x.PontosEspeciais,
            PontosTotal: x.Total,
            PlacaresExatos: x.Exatos,
            Lider: i == 0)).ToList();

        return new RankingDto(regra, entries);
    }

    /// <summary>Mapa UserId -> pontuacao total (usado na tela admin de usuarios).</summary>
    public async Task<Dictionary<int, int>> GetTotaisPorUsuarioAsync()
    {
        var jogos = await _db.Predictions.AsNoTracking()
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Pontos = g.Sum(x => x.PontosObtidos) })
            .ToDictionaryAsync(x => x.UserId, x => x.Pontos);

        var classificacao = await _db.GroupQualifierBets.AsNoTracking()
            .GroupBy(g => g.UserId)
            .Select(g => new { UserId = g.Key, Pontos = g.Sum(x => x.PontosObtidos) })
            .ToDictionaryAsync(x => x.UserId, x => x.Pontos);

        var especiais = await _db.SpecialBets.AsNoTracking()
            .ToDictionaryAsync(s => s.UserId, s => s.PontosObtidos);

        var ids = jogos.Keys.Union(classificacao.Keys).Union(especiais.Keys).ToHashSet();
        return ids.ToDictionary(
            id => id,
            id => (jogos.TryGetValue(id, out var j) ? j : 0)
                + (classificacao.TryGetValue(id, out var c) ? c : 0)
                + (especiais.TryGetValue(id, out var e) ? e : 0));
    }
}
