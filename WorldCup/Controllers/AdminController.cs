using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCup.Domain.Entities;
using WorldCup.Dtos;
using WorldCup.Infrastructure;
using WorldCup.Services;

namespace WorldCup.Controllers;

/// <summary>
/// Area do admin: pagamentos, lancamento de resultados e apuracao automatica dos pontos.
/// Todos os endpoints exigem o papel "Admin".
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IScoringService _scoring;
    private readonly IRankingService _ranking;

    public AdminController(AppDbContext db, IScoringService scoring, IRankingService ranking)
    {
        _db = db;
        _scoring = scoring;
        _ranking = ranking;
    }

    // ---------------- Usuarios / pagamento ----------------

    [HttpGet("api/users")]
    public async Task<IActionResult> Users()
    {
        var totais = await _ranking.GetTotaisPorUsuarioAsync();
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();
        var dtos = users.Select(u => new AdminUserDto(
            u.Id, u.Nome, u.Email, u.IsAdmin, u.Pago,
            totais.TryGetValue(u.Id, out var t) ? t : 0)).ToList();
        return Ok(dtos);
    }

    [HttpPut("api/users/{id:int}/payment")]
    public async Task<IActionResult> SetPayment(int id, SetPaymentRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "Usuario nao encontrado." });
        user.Pago = req.Pago;
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.Pago });
    }

    // ---------------- Resultado de jogo ----------------

    /// <summary>Lanca o placar oficial e dispara o calculo automatico dos pontos do jogo.</summary>
    [HttpPut("api/matches/{id:int}/result")]
    public async Task<IActionResult> SetMatchResult(int id, SetMatchResultRequest req)
    {
        if (req.GolsMandante < 0 || req.GolsVisitante < 0)
            return BadRequest(new { message = "Placar nao pode ser negativo." });

        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound(new { message = "Jogo nao encontrado." });

        var cfg = await GetSettingsAsync();
        match.GolsMandante = req.GolsMandante;
        match.GolsVisitante = req.GolsVisitante;
        match.Encerrado = true;
        match.ResultadoManual = true;

        // Apuracao: recalcula os pontos de todos os palpites deste jogo (5 / 3 / 0).
        var preds = await _db.Predictions.Where(p => p.MatchId == id).ToListAsync();
        foreach (var p in preds)
            p.PontosObtidos = _scoring.PontosDoPalpite(req.GolsMandante, req.GolsVisitante, p.GolsMandantePalpite, p.GolsVisitantePalpite, cfg);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Resultado lancado. {preds.Count} palpite(s) apurado(s)." });
    }

    /// <summary>Reverte o resultado: marca o jogo como "em andamento" (sem placar) e zera os pontos apurados.</summary>
    [HttpPut("api/matches/{id:int}/reopen")]
    public async Task<IActionResult> ReopenMatch(int id)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match is null) return NotFound(new { message = "Jogo nao encontrado." });

        match.Encerrado = false;
        match.GolsMandante = null;
        match.GolsVisitante = null;
        match.ResultadoManual = false;

        var preds = await _db.Predictions.Where(p => p.MatchId == id).ToListAsync();
        foreach (var p in preds) p.PontosObtidos = 0;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Resultado revertido; jogo marcado como em andamento." });
    }

    // ---------------- Resultado de grupo (1o/2o) ----------------

    [HttpGet("api/groups/results")]
    public async Task<IActionResult> GroupResults()
    {
        var results = await _db.GroupResults.AsNoTracking()
            .OrderBy(g => g.Grupo)
            .Select(g => new { g.Grupo, g.PrimeiroTeamId, g.SegundoTeamId })
            .ToListAsync();
        return Ok(results);
    }

    /// <summary>Lanca o 1o e 2o oficiais de um grupo e apura os pontos de classificacao (3 por acerto).</summary>
    [HttpPut("api/groups/{grupo}/result")]
    public async Task<IActionResult> SetGroupResult(string grupo, SetGroupResultRequest req)
    {
        grupo = (grupo ?? string.Empty).Trim().ToUpperInvariant();
        if (req.PrimeiroTeamId == req.SegundoTeamId)
            return BadRequest(new { message = "1o e 2o devem ser times diferentes." });

        var teamGroup = await _db.Teams.AsNoTracking()
            .Where(t => t.Id == req.PrimeiroTeamId || t.Id == req.SegundoTeamId)
            .ToDictionaryAsync(t => t.Id, t => t.Grupo);
        if (!teamGroup.TryGetValue(req.PrimeiroTeamId, out var g1) || g1 != grupo)
            return BadRequest(new { message = "Time do 1o lugar nao pertence ao grupo." });
        if (!teamGroup.TryGetValue(req.SegundoTeamId, out var g2) || g2 != grupo)
            return BadRequest(new { message = "Time do 2o lugar nao pertence ao grupo." });

        var result = await _db.GroupResults.FindAsync(grupo);
        if (result is null)
        {
            result = new GroupResult { Grupo = grupo };
            _db.GroupResults.Add(result);
        }
        result.PrimeiroTeamId = req.PrimeiroTeamId;
        result.SegundoTeamId = req.SegundoTeamId;

        var cfg = await GetSettingsAsync();
        var bets = await _db.GroupQualifierBets.Where(b => b.Grupo == grupo).ToListAsync();
        foreach (var bet in bets)
            bet.PontosObtidos = _scoring.PontosClassificacao(bet, result, cfg);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Grupo {grupo} apurado. {bets.Count} palpite(s) de classificacao atualizados." });
    }

    // ---------------- Resultado final do torneio (especiais) ----------------

    [HttpGet("api/tournament/result")]
    public async Task<IActionResult> GetTournamentResult()
    {
        var tr = await _db.TournamentResults.AsNoTracking().FirstOrDefaultAsync();
        return Ok(new SetTournamentResultRequest(tr?.CampeaoTeamId, tr?.ArtilheiroPlayerId, tr?.MelhorJogadorPlayerId));
    }

    /// <summary>Lanca campeao/artilheiro/melhor jogador e apura as apostas especiais (25/20/15).</summary>
    [HttpPut("api/tournament/result")]
    public async Task<IActionResult> SetTournamentResult(SetTournamentResultRequest req)
    {
        if (req.CampeaoTeamId is { } c && !await _db.Teams.AnyAsync(t => t.Id == c))
            return BadRequest(new { message = "Selecao campea invalida." });
        if (req.ArtilheiroPlayerId is { } a && !await _db.Players.AnyAsync(p => p.Id == a))
            return BadRequest(new { message = "Artilheiro invalido." });
        if (req.MelhorJogadorPlayerId is { } m && !await _db.Players.AnyAsync(p => p.Id == m))
            return BadRequest(new { message = "Melhor jogador invalido." });

        var tr = await _db.TournamentResults.FirstOrDefaultAsync();
        if (tr is null)
        {
            tr = new TournamentResult();
            _db.TournamentResults.Add(tr);
        }
        tr.CampeaoTeamId = req.CampeaoTeamId;
        tr.ArtilheiroPlayerId = req.ArtilheiroPlayerId;
        tr.MelhorJogadorPlayerId = req.MelhorJogadorPlayerId;

        var cfg = await GetSettingsAsync();
        var bets = await _db.SpecialBets.ToListAsync();
        foreach (var bet in bets)
            bet.PontosObtidos = _scoring.PontosEspeciais(bet, tr, cfg);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Resultado final lancado. {bets.Count} aposta(s) especial(is) apuradas." });
    }

    // ---------------- Configuracao ----------------

    [HttpPut("api/settings")]
    public async Task<IActionResult> UpdateSettings(UpdateSettingsRequest req)
    {
        var s = await GetSettingsAsync();
        if (req.LockBetsAtUtc.HasValue) s.LockBetsAtUtc = req.LockBetsAtUtc;
        if (!string.IsNullOrWhiteSpace(req.RegraDesempate)) s.RegraDesempate = req.RegraDesempate!;
        if (req.PontosPlacarExato is { } pe) s.PontosPlacarExato = pe;
        if (req.PontosResultado is { } pr) s.PontosResultado = pr;
        if (req.PontosClassificacaoPorAcerto is { } pc) s.PontosClassificacaoPorAcerto = pc;
        if (req.PontosCampeao is { } pca) s.PontosCampeao = pca;
        if (req.PontosArtilheiro is { } par) s.PontosArtilheiro = par;
        if (req.PontosMelhorJogador is { } pmj) s.PontosMelhorJogador = pmj;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Configuracao atualizada." });
    }

    // ---------------- Import de dados reais (openfootball) ----------------

    /// <summary>
    /// Reimporta times/grupos/jogos/elencos REAIS do openfootball (dominio publico, sem chave).
    /// Substitui todos os dados de times/jogadores/jogos e zera palpites/apostas/resultados.
    /// </summary>
    [HttpPost("api/admin/import-openfootball")]
    public async Task<IActionResult> ImportOpenFootball([FromServices] OpenFootballImporter importer)
    {
        try
        {
            var r = await importer.ImportAsync();
            return Ok(new
            {
                message = $"Importados {r.Teams} times, {r.Players} jogadores e {r.Matches} jogos ({r.MatchesEncerrados} ja encerrados).",
                r.Teams,
                r.Players,
                r.Matches,
                r.MatchesEncerrados
            });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Falha ao importar do openfootball: " + ex.Message });
        }
    }

    private async Task<Settings> GetSettingsAsync()
    {
        var s = await _db.Settings.FirstOrDefaultAsync();
        if (s is null)
        {
            s = new Settings();
            _db.Settings.Add(s);
        }
        return s;
    }
}
