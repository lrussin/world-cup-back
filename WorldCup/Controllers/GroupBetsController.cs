using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCup.Auth;
using WorldCup.Domain.Entities;
using WorldCup.Dtos;
using WorldCup.Infrastructure;
using WorldCup.Services;

namespace WorldCup.Controllers;

[ApiController]
[Authorize]
[Route("api/group-bets")]
public class GroupBetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILockService _lock;

    public GroupBetsController(AppDbContext db, ILockService lockService)
    {
        _db = db;
        _lock = lockService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var bets = await _db.GroupQualifierBets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => new GroupBetDto(b.Grupo, b.PrimeiroTeamId, b.SegundoTeamId, b.PontosObtidos))
            .ToListAsync();
        return Ok(bets);
    }

    /// <summary>Salva os palpites de 1o/2o de cada grupo. Travado a partir do prazo global.</summary>
    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> Save(SaveGroupBetsRequest req)
    {
        if (await _lock.ApostasGeraisTravadasAsync())
            return StatusCode(403, new { message = "Apostas de classificacao bloqueadas: prazo encerrado." });

        if (req.Itens is null || req.Itens.Count == 0)
            return BadRequest(new { message = "Envie ao menos um palpite de grupo." });

        // Mapa TeamId -> Grupo para validar que os times pertencem ao grupo informado.
        var teamGroup = await _db.Teams.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Grupo);
        var userId = User.GetUserId();
        var existentes = await _db.GroupQualifierBets
            .Where(b => b.UserId == userId)
            .ToDictionaryAsync(b => b.Grupo);

        foreach (var item in req.Itens)
        {
            var grupo = (item.Grupo ?? string.Empty).Trim().ToUpperInvariant();
            if (item.PrimeiroTeamId == item.SegundoTeamId)
                return BadRequest(new { message = $"Grupo {grupo}: 1o e 2o devem ser times diferentes." });
            if (!teamGroup.TryGetValue(item.PrimeiroTeamId, out var g1) || g1 != grupo)
                return BadRequest(new { message = $"Grupo {grupo}: time do 1o lugar invalido." });
            if (!teamGroup.TryGetValue(item.SegundoTeamId, out var g2) || g2 != grupo)
                return BadRequest(new { message = $"Grupo {grupo}: time do 2o lugar invalido." });

            if (existentes.TryGetValue(grupo, out var bet))
            {
                bet.PrimeiroTeamId = item.PrimeiroTeamId;
                bet.SegundoTeamId = item.SegundoTeamId;
            }
            else
            {
                _db.GroupQualifierBets.Add(new GroupQualifierBet
                {
                    UserId = userId,
                    Grupo = grupo,
                    PrimeiroTeamId = item.PrimeiroTeamId,
                    SegundoTeamId = item.SegundoTeamId
                });
            }
        }

        await _db.SaveChangesAsync();
        return await Me();
    }

    /// <summary>Palpites de classificacao de todos os participantes pagos. Visivel apenas apos a trava global.</summary>
    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        if (!await _lock.ApostasGeraisTravadasAsync())
            return StatusCode(403, new { message = "Os palpites de classificacao so ficam visiveis apos o inicio da Copa." });

        var bets = await _db.GroupQualifierBets.AsNoTracking()
            .Include(b => b.User)
            .Include(b => b.PrimeiroTeam)
            .Include(b => b.SegundoTeam)
            .Where(b => b.User!.Pago)
            .OrderBy(b => b.Grupo).ThenBy(b => b.User!.Nome)
            .Select(b => new ParticipantGroupBetDto(
                b.User!.Nome,
                b.Grupo,
                b.PrimeiroTeam != null ? b.PrimeiroTeam.Nome : null,
                b.SegundoTeam != null ? b.SegundoTeam.Nome : null))
            .ToListAsync();

        return Ok(bets);
    }
}
