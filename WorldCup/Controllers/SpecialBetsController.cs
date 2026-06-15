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
[Route("api/special-bets")]
public class SpecialBetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILockService _lock;

    public SpecialBetsController(AppDbContext db, ILockService lockService)
    {
        _db = db;
        _lock = lockService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var bet = await _db.SpecialBets.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
        var travado = await _lock.ApostasGeraisTravadasAsync();
        if (bet is null)
            return Ok(new SpecialBetDto(null, null, null, travado, 0));
        return Ok(new SpecialBetDto(bet.CampeaoTeamId, bet.ArtilheiroPlayerId, bet.MelhorJogadorPlayerId, travado, bet.PontosObtidos));
    }

    /// <summary>Cria/atualiza campeao, artilheiro e melhor jogador. Imutavel apos o bloqueio global.</summary>
    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> Save(SaveSpecialBetRequest req)
    {
        if (await _lock.ApostasGeraisTravadasAsync())
            return StatusCode(403, new { message = "Apostas especiais bloqueadas: prazo encerrado." });

        if (!await _db.Teams.AnyAsync(t => t.Id == req.CampeaoTeamId))
            return BadRequest(new { message = "Selecao campea invalida." });
        if (!await _db.Players.AnyAsync(p => p.Id == req.ArtilheiroPlayerId))
            return BadRequest(new { message = "Artilheiro invalido." });
        if (!await _db.Players.AnyAsync(p => p.Id == req.MelhorJogadorPlayerId))
            return BadRequest(new { message = "Melhor jogador invalido." });

        var userId = User.GetUserId();
        var bet = await _db.SpecialBets.FirstOrDefaultAsync(s => s.UserId == userId);
        if (bet is null)
        {
            bet = new SpecialBet { UserId = userId, CriadoEm = DateTime.UtcNow };
            _db.SpecialBets.Add(bet);
        }
        bet.CampeaoTeamId = req.CampeaoTeamId;
        bet.ArtilheiroPlayerId = req.ArtilheiroPlayerId;
        bet.MelhorJogadorPlayerId = req.MelhorJogadorPlayerId;

        await _db.SaveChangesAsync();
        return Ok(new SpecialBetDto(bet.CampeaoTeamId, bet.ArtilheiroPlayerId, bet.MelhorJogadorPlayerId, false, bet.PontosObtidos));
    }

    /// <summary>Apostas especiais de todos os participantes pagos. Visivel apenas apos a trava global.</summary>
    [HttpGet("all")]
    public async Task<IActionResult> All()
    {
        if (!await _lock.ApostasGeraisTravadasAsync())
            return StatusCode(403, new { message = "As apostas dos participantes so ficam visiveis apos o inicio da Copa." });

        var bets = await _db.SpecialBets.AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.CampeaoTeam)
            .Include(s => s.ArtilheiroPlayer)
            .Include(s => s.MelhorJogadorPlayer)
            .Where(s => s.User!.Pago)
            .OrderBy(s => s.User!.Nome)
            .Select(s => new ParticipantBetsDto(
                s.User!.Nome,
                s.CampeaoTeam != null ? s.CampeaoTeam.Nome : null,
                s.ArtilheiroPlayer != null ? s.ArtilheiroPlayer.Nome : null,
                s.MelhorJogadorPlayer != null ? s.MelhorJogadorPlayer.Nome : null))
            .ToListAsync();

        return Ok(bets);
    }
}
