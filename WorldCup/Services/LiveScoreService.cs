using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorldCup.Dtos;
using WorldCup.Infrastructure;

namespace WorldCup.Services;

/// <summary>
/// Placar ao vivo via football-data.org. Casa o jogo da API com o nosso pelo codigo FIFA (TLA)
/// das duas selecoes. Usa cache curto para respeitar o limite de requisicoes da API.
///
/// Sem chave configurada (FootballData:ApiKey), retorna vazio — o app funciona normalmente,
/// apenas sem placar ao vivo.
/// </summary>
public interface ILiveScoreService
{
    Task<List<LiveScoreDto>> GetLiveAsync(CancellationToken ct = default);
}

public class LiveScoreService : ILiveScoreService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;
    private readonly string _competition;
    private readonly string _apiKey;

    private const string CacheKey = "livescores";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public LiveScoreService(AppDbContext db, IHttpClientFactory httpFactory, IMemoryCache cache, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _cache = cache;
        _baseUrl = config["FootballData:BaseUrl"]?.TrimEnd('/') ?? "https://api.football-data.org/v4";
        _competition = config["FootballData:Competition"] ?? "WC";
        _apiKey = config["FootballData:ApiKey"] ?? "";
    }

    public async Task<List<LiveScoreDto>> GetLiveAsync(CancellationToken ct = default)
    {
        // Sem chave: nao chama a API.
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new List<LiveScoreDto>();

        if (_cache.TryGetValue(CacheKey, out List<LiveScoreDto>? cached) && cached is not null)
            return cached;

        var result = new List<LiveScoreDto>();
        try
        {
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var req = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_baseUrl}/competitions/{_competition}/matches?status=IN_PLAY,PAUSED");
            req.Headers.Add("X-Auth-Token", _apiKey);

            using var resp = await client.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                var data = JsonSerializer.Deserialize<FdResponse>(json, JsonOpts);

                if (data?.Matches is { Count: > 0 })
                {
                    var matches = await _db.Matches.AsNoTracking()
                        .Include(m => m.HomeTeam)
                        .Include(m => m.AwayTeam)
                        .Where(m => m.HomeTeam!.FifaCode != null && m.AwayTeam!.FifaCode != null)
                        .ToListAsync(ct);

                    foreach (var fd in data.Matches)
                    {
                        var home = fd.HomeTeam?.Tla;
                        var away = fd.AwayTeam?.Tla;
                        if (string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away))
                            continue;

                        var match = matches.FirstOrDefault(m =>
                            string.Equals(m.HomeTeam!.FifaCode, home, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(m.AwayTeam!.FifaCode, away, StringComparison.OrdinalIgnoreCase));
                        if (match is null)
                            continue;

                        result.Add(new LiveScoreDto(
                            match.Id,
                            fd.Score?.FullTime?.Home ?? 0,
                            fd.Score?.FullTime?.Away ?? 0,
                            fd.Minute,
                            fd.Status ?? "IN_PLAY"));
                    }
                }
            }
        }
        catch
        {
            // Degrada em silencio: sem placar ao vivo, app segue normal.
        }

        _cache.Set(CacheKey, result, TimeSpan.FromSeconds(20));
        return result;
    }

    // ----- DTOs da football-data.org -----
    private sealed class FdResponse
    {
        [JsonPropertyName("matches")] public List<FdMatch>? Matches { get; set; }
    }

    private sealed class FdMatch
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("minute")] public int? Minute { get; set; }
        [JsonPropertyName("homeTeam")] public FdTeam? HomeTeam { get; set; }
        [JsonPropertyName("awayTeam")] public FdTeam? AwayTeam { get; set; }
        [JsonPropertyName("score")] public FdScore? Score { get; set; }
    }

    private sealed class FdTeam
    {
        [JsonPropertyName("tla")] public string? Tla { get; set; }
    }

    private sealed class FdScore
    {
        [JsonPropertyName("fullTime")] public FdGoals? FullTime { get; set; }
    }

    private sealed class FdGoals
    {
        [JsonPropertyName("home")] public int? Home { get; set; }
        [JsonPropertyName("away")] public int? Away { get; set; }
    }
}
