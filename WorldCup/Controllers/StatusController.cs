using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCup.Domain.Enums;
using WorldCup.Dtos;
using WorldCup.Infrastructure;

namespace WorldCup.Controllers;

/// <summary>Status de preenchimento das apostas/palpites — visivel a todos (quem ja fez e quem falta).</summary>
[ApiController]
[Authorize]
public class StatusController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatusController(AppDbContext db) => _db = db;

    /// <summary>Por participante: se fez especiais, classificacao, e quantos palpites de jogo registrou.</summary>
    [HttpGet("api/status")]
    public async Task<IActionResult> Get()
    {
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Pago)
            .OrderBy(u => u.Nome)
            .Select(u => new { u.Id, u.Nome })
            .ToListAsync();

        var especiais = (await _db.SpecialBets.AsNoTracking()
            .Where(s => s.CampeaoTeamId != null && s.ArtilheiroPlayerId != null && s.MelhorJogadorPlayerId != null)
            .Select(s => s.UserId)
            .ToListAsync()).ToHashSet();

        var classificacao = (await _db.GroupQualifierBets.AsNoTracking()
            .Select(b => b.UserId)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var palpites = (await _db.Predictions.AsNoTracking()
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Qtd = g.Count() })
            .ToListAsync()).ToDictionary(x => x.UserId, x => x.Qtd);

        var result = users
            .Select(u => new BetStatusDto(
                u.Nome,
                especiais.Contains(u.Id),
                classificacao.Contains(u.Id),
                palpites.TryGetValue(u.Id, out var q) ? q : 0))
            .ToList();

        return Ok(result);
    }

    /// <summary>Por jogo (apenas os que ja tem ao menos 1 palpite): quem palpitou e quem falta.</summary>
    [HttpGet("api/status/predictions")]
    public async Task<IActionResult> Predictions()
    {
        var paid = await _db.Users.AsNoTracking()
            .Where(u => u.Pago)
            .OrderBy(u => u.Nome)
            .Select(u => new { u.Id, u.Nome })
            .ToListAsync();
        var total = paid.Count;

        var preds = await _db.Predictions.AsNoTracking()
            .Where(p => p.User!.Pago)
            .Select(p => new { p.MatchId, p.UserId })
            .ToListAsync();
        var doneByMatch = preds
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToHashSet());

        var games = await _db.Matches.AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.Fase == Fase.Grupos)
            .OrderBy(m => m.DataHoraUtc)
            .ToListAsync();

        var result = new List<MatchPredStatusDto>();
        foreach (var m in games)
        {
            doneByMatch.TryGetValue(m.Id, out var done);
            var feitoIds = done ?? new HashSet<int>();
            var faltam = paid.Where(p => !feitoIds.Contains(p.Id)).Select(p => p.Nome).ToList();
            result.Add(new MatchPredStatusDto(
                m.Id, m.Grupo, m.HomeTeam!.Nome, m.AwayTeam!.Nome, m.DataHoraUtc,
                m.Encerrado, feitoIds.Count, total, faltam));
        }

        return Ok(result);
    }
}
