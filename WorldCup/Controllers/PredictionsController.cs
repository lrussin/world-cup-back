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
[Route("api/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILockService _lock;

    public PredictionsController(AppDbContext db, ILockService lockService)
    {
        _db = db;
        _lock = lockService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var preds = await _db.Predictions.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new PredictionDto(p.MatchId, p.GolsMandantePalpite, p.GolsVisitantePalpite, p.PontosObtidos))
            .ToListAsync();
        return Ok(preds);
    }

    /// <summary>Cria ou atualiza o palpite de um jogo. Respeita a trava por kickoff.</summary>
    [HttpPost]
    [HttpPut]
    public async Task<IActionResult> Save(SavePredictionRequest req)
    {
        if (req.GolsMandante < 0 || req.GolsVisitante < 0)
            return BadRequest(new { message = "Placar nao pode ser negativo." });
        if (req.GolsMandante > 99 || req.GolsVisitante > 99)
            return BadRequest(new { message = "Placar invalido." });

        var match = await _db.Matches.FindAsync(req.MatchId);
        if (match is null)
            return NotFound(new { message = "Jogo nao encontrado." });

        if (_lock.JogoTravado(match))
            return StatusCode(403, new { message = "Jogo travado: o palpite so pode ser enviado antes do inicio da partida." });

        var userId = User.GetUserId();
        var now = DateTime.UtcNow;
        var pred = await _db.Predictions.FirstOrDefaultAsync(p => p.UserId == userId && p.MatchId == req.MatchId);
        if (pred is null)
        {
            pred = new Prediction { UserId = userId, MatchId = req.MatchId, CriadoEm = now };
            _db.Predictions.Add(pred);
        }
        pred.GolsMandantePalpite = req.GolsMandante;
        pred.GolsVisitantePalpite = req.GolsVisitante;
        pred.AtualizadoEm = now;

        await _db.SaveChangesAsync();
        return Ok(new PredictionDto(pred.MatchId, pred.GolsMandantePalpite, pred.GolsVisitantePalpite, pred.PontosObtidos));
    }
}
