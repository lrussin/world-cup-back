using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCup.Dtos;
using WorldCup.Infrastructure;
using WorldCup.Services;

namespace WorldCup.Controllers;

/// <summary>Dados de referencia (times, jogadores) e configuracao publica do bolao.</summary>
[ApiController]
[Authorize]
public class ReferenceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILockService _lock;

    public ReferenceController(AppDbContext db, ILockService lockService)
    {
        _db = db;
        _lock = lockService;
    }

    [HttpGet("api/teams")]
    public async Task<IActionResult> Teams()
    {
        var teams = await _db.Teams.AsNoTracking()
            .OrderBy(t => t.Grupo).ThenBy(t => t.Nome)
            .Select(t => new TeamDto(t.Id, t.Nome, t.Grupo, t.CodigoBandeira))
            .ToListAsync();
        return Ok(teams);
    }

    [HttpGet("api/players")]
    public async Task<IActionResult> Players()
    {
        var players = await _db.Players.AsNoTracking()
            .Include(p => p.Team)
            .OrderBy(p => p.Team!.Nome).ThenBy(p => p.Nome)
            .Select(p => new PlayerDto(p.Id, p.Nome, p.TeamId, p.Team!.Nome))
            .ToListAsync();
        return Ok(players);
    }

    [HttpGet("api/settings")]
    public async Task<IActionResult> GetSettings()
    {
        var s = await _db.Settings.AsNoTracking().FirstOrDefaultAsync() ?? new Domain.Entities.Settings();
        var globalLock = await _lock.GlobalLockUtcAsync();
        var travado = await _lock.ApostasGeraisTravadasAsync();
        return Ok(new SettingsDto(
            globalLock,
            travado,
            s.RegraDesempate,
            s.PontosPlacarExato,
            s.PontosResultado,
            s.PontosClassificacaoPorAcerto,
            s.PontosCampeao,
            s.PontosArtilheiro,
            s.PontosMelhorJogador));
    }
}
