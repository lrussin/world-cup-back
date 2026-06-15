using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCup.Services;

namespace WorldCup.Controllers;

[ApiController]
[Authorize]
[Route("api/ranking")]
public class RankingController : ControllerBase
{
    private readonly IRankingService _ranking;

    public RankingController(IRankingService ranking) => _ranking = ranking;

    /// <summary>Ranking ordenado por pontuacao total (somente participantes pagos), com regra de desempate.</summary>
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _ranking.GetRankingAsync());
}
