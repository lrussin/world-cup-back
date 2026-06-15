using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCup.Auth;
using WorldCup.Domain.Entities;
using WorldCup.Domain.Enums;
using WorldCup.Dtos;
using WorldCup.Infrastructure;
using WorldCup.Services;

namespace WorldCup.Controllers;

[ApiController]
[Authorize]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MatchesController(AppDbContext db) => _db = db;

    /// <summary>Jogos da fase de grupos com o palpite do usuario e o estado de bloqueio.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetUserId();

        var matches = await _db.Matches.AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.Fase == Fase.Grupos)
            .OrderBy(m => m.DataHoraUtc)
            .ToListAsync();

        var myPreds = await _db.Predictions.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.MatchId);

        var now = DateTime.UtcNow;
        var dtos = matches.Select(m =>
        {
            myPreds.TryGetValue(m.Id, out var p);
            return new MatchDto(
                m.Id,
                m.Grupo,
                m.DataHoraUtc,
                ToDto(m.HomeTeam!),
                ToDto(m.AwayTeam!),
                m.Encerrado ? m.GolsMandante : null,
                m.Encerrado ? m.GolsVisitante : null,
                m.Encerrado,
                m.Encerrado || now >= m.DataHoraUtc,
                p is null ? null : new PredictionDto(p.MatchId, p.GolsMandantePalpite, p.GolsVisitantePalpite, p.PontosObtidos));
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>Palpites dos demais participantes — visiveis apenas depois do inicio da partida.</summary>
    [HttpGet("{id:int}/predictions")]
    public async Task<IActionResult> GetOthers(int id)
    {
        var match = await _db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (match is null)
            return NotFound(new { message = "Jogo nao encontrado." });

        if (DateTime.UtcNow < match.DataHoraUtc)
            return StatusCode(403, new { message = "Os palpites dos outros so aparecem depois do inicio do jogo." });

        var preds = await _db.Predictions.AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.MatchId == id)
            .OrderByDescending(p => p.PontosObtidos)
            .Select(p => new OtherPredictionDto(
                p.User!.Nome, p.GolsMandantePalpite, p.GolsVisitantePalpite, p.PontosObtidos,
                p.CriadoEm, p.AtualizadoEm))
            .ToListAsync();

        return Ok(preds);
    }

    /// <summary>Placar ao vivo dos jogos em andamento (football-data.org). Vazio se nao configurado.</summary>
    [HttpGet("live")]
    public async Task<IActionResult> Live([FromServices] ILiveScoreService live)
        => Ok(await live.GetLiveAsync());

    private static TeamDto ToDto(Team t) => new(t.Id, t.Nome, t.Grupo, t.CodigoBandeira);
}
