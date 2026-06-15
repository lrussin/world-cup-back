using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WorldCup.Domain.Entities;
using WorldCup.Domain.Enums;

namespace WorldCup.Infrastructure;

/// <summary>
/// Importa times, grupos, jogos (com kickoff real em UTC) e elencos da fonte publica e gratuita
/// openfootball/worldcup.json (dominio publico, sem chave de API).
///
/// Substitui completamente os dados de times/jogadores/jogos por dados reais. Como isso invalida
/// referencias, tambem limpa palpites/apostas/resultados antes de recarregar.
/// </summary>
public class OpenFootballImporter
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _baseUrl;

    public OpenFootballImporter(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _baseUrl = config["OpenFootball:BaseUrl"]?.TrimEnd('/')
                   ?? "https://raw.githubusercontent.com/openfootball/worldcup.json/master/2026";
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<ImportResult> ImportAsync(CancellationToken ct = default)
    {
        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(25);

        var teams = await GetJson<List<OfTeam>>(http, $"{_baseUrl}/worldcup.teams.json", ct);
        var matchesFile = await GetJson<OfMatchesFile>(http, $"{_baseUrl}/worldcup.json", ct);
        var squads = await GetJson<List<OfSquad>>(http, $"{_baseUrl}/worldcup.squads.json", ct);

        if (teams.Count == 0 || matchesFile.Matches.Count == 0)
            throw new InvalidOperationException("Dados do openfootball vieram vazios.");

        // ----- Limpa dados dependentes (ordem respeita as FKs) -----
        await _db.Predictions.ExecuteDeleteAsync(ct);
        await _db.GroupQualifierBets.ExecuteDeleteAsync(ct);
        await _db.SpecialBets.ExecuteDeleteAsync(ct);
        await _db.GroupResults.ExecuteDeleteAsync(ct);
        await _db.TournamentResults.ExecuteDeleteAsync(ct);
        await _db.Matches.ExecuteDeleteAsync(ct);
        await _db.Players.ExecuteDeleteAsync(ct);
        await _db.Teams.ExecuteDeleteAsync(ct);

        // ----- Times -----
        var byName = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
        var byFifa = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in teams)
        {
            var team = new Team
            {
                Nome = Traduzir(t.Name),
                Grupo = t.Group.Trim(),
                CodigoBandeira = FlagCodeFromEmoji(t.FlagIcon),
                FifaCode = t.FifaCode,
            };
            _db.Teams.Add(team);
            byName[t.Name] = team;
            if (!string.IsNullOrWhiteSpace(t.FifaCode)) byFifa[t.FifaCode] = team;
        }
        await _db.SaveChangesAsync(ct);

        // ----- Jogadores (elencos reais) -----
        var totalPlayers = 0;
        foreach (var sq in squads)
        {
            if (!byFifa.TryGetValue(sq.FifaCode, out var team)) continue;
            foreach (var p in sq.Players)
            {
                _db.Players.Add(new Player { Nome = FormatPlayer(p), TeamId = team.Id });
                totalPlayers++;
            }
        }
        await _db.SaveChangesAsync(ct);

        // ----- Jogos da fase de grupos -----
        var totalMatches = 0;
        var encerrados = 0;
        foreach (var m in matchesFile.Matches)
        {
            if (string.IsNullOrWhiteSpace(m.Group) || !m.Group.StartsWith("Group ", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!byName.TryGetValue(m.Team1, out var home) || !byName.TryGetValue(m.Team2, out var away))
                continue;

            var match = new Match
            {
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                Fase = Fase.Grupos,
                Grupo = m.Group.Replace("Group ", "", StringComparison.OrdinalIgnoreCase).Trim(),
                DataHoraUtc = ParseUtc(m.Date, m.Time),
            };
            if (m.Score?.Ft is { Length: 2 } ft)
            {
                match.GolsMandante = ft[0];
                match.GolsVisitante = ft[1];
                match.Encerrado = true;
                encerrados++;
            }
            _db.Matches.Add(match);
            totalMatches++;
        }
        await _db.SaveChangesAsync(ct);

        // ----- Trava global = kickoff do primeiro jogo -----
        var settings = await _db.Settings.FirstOrDefaultAsync(ct);
        if (settings is not null && await _db.Matches.AnyAsync(ct))
        {
            settings.LockBetsAtUtc = await _db.Matches.MinAsync(x => x.DataHoraUtc, ct);
            await _db.SaveChangesAsync(ct);
        }

        return new ImportResult(teams.Count, totalPlayers, totalMatches, encerrados);
    }

    private static async Task<T> GetJson<T>(HttpClient http, string url, CancellationToken ct)
    {
        var json = await http.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<T>(json, JsonOpts)
               ?? throw new InvalidOperationException($"JSON nulo de {url}");
    }

    /// <summary>"2026-06-11" + "13:00 UTC-6" => DateTime UTC (Kind=Utc).</summary>
    private static DateTime ParseUtc(string date, string time)
    {
        var d = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var parts = time.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hm = parts.Length > 0 ? TimeOnly.Parse(parts[0], CultureInfo.InvariantCulture) : new TimeOnly(12, 0);

        var offsetHours = 0;
        if (parts.Length > 1)
        {
            var tz = parts[1].Replace("UTC", "", StringComparison.OrdinalIgnoreCase);
            int.TryParse(tz, NumberStyles.Integer, CultureInfo.InvariantCulture, out offsetHours);
        }

        // hora local (no fuso UTC{offset}) -> UTC: UTC = local - offset
        var local = d.ToDateTime(hm);
        return DateTime.SpecifyKind(local.AddHours(-offsetHours), DateTimeKind.Utc);
    }

    /// <summary>
    /// Deriva o codigo de bandeira (flagcdn) a partir do emoji:
    ///  - emoji de pais = 2 "regional indicators" => ISO-3166 alpha-2 (ex.: 🇲🇽 -> "mx").
    ///  - emoji de subdivisao (Escocia/Pais de Gales/Inglaterra) = bandeira preta + tags => "gb-sct" etc.
    /// </summary>
    private static string FlagCodeFromEmoji(string emoji)
    {
        if (string.IsNullOrEmpty(emoji)) return "";
        var runes = emoji.EnumerateRunes().ToList();

        if (runes.Count > 0 && runes[0].Value == 0x1F3F4) // 🏴 + tag sequence
        {
            var sb = new StringBuilder();
            foreach (var r in runes.Skip(1))
                if (r.Value >= 0xE0061 && r.Value <= 0xE007A) // tag latin small a-z
                    sb.Append((char)(r.Value - 0xE0000));
            var s = sb.ToString(); // ex.: "gbsct"
            return s.Length >= 4 && s.StartsWith("gb") ? "gb-" + s[2..] : s;
        }

        var iso = new StringBuilder();
        foreach (var r in runes)
            if (r.Value >= 0x1F1E6 && r.Value <= 0x1F1FF) // regional indicator A-Z
                iso.Append((char)('a' + (r.Value - 0x1F1E6)));
        return iso.ToString();
    }

    private static string FormatPlayer(OfPlayer p)
    {
        var pos = string.IsNullOrWhiteSpace(p.Pos) ? "" : $" · {p.Pos}";
        return $"{p.Name}{pos}";
    }

    /// <summary>Nome da selecao em portugues (os nomes de jogadores nao sao traduzidos).</summary>
    private static string Traduzir(string nomeIngles) =>
        Traducoes.TryGetValue(nomeIngles, out var pt) ? pt : nomeIngles;

    private static readonly Dictionary<string, string> Traducoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["South Korea"] = "Coreia do Sul",
        ["Czech Republic"] = "República Tcheca",
        ["Mexico"] = "México",
        ["South Africa"] = "África do Sul",
        ["Qatar"] = "Catar",
        ["Switzerland"] = "Suíça",
        ["Canada"] = "Canadá",
        ["Bosnia & Herzegovina"] = "Bósnia e Herzegovina",
        ["Haiti"] = "Haiti",
        ["Scotland"] = "Escócia",
        ["Brazil"] = "Brasil",
        ["Morocco"] = "Marrocos",
        ["Australia"] = "Austrália",
        ["Turkey"] = "Turquia",
        ["USA"] = "Estados Unidos",
        ["Paraguay"] = "Paraguai",
        ["Ivory Coast"] = "Costa do Marfim",
        ["Ecuador"] = "Equador",
        ["Germany"] = "Alemanha",
        ["Curaçao"] = "Curaçao",
        ["Sweden"] = "Suécia",
        ["Tunisia"] = "Tunísia",
        ["Netherlands"] = "Holanda",
        ["Japan"] = "Japão",
        ["Iran"] = "Irã",
        ["New Zealand"] = "Nova Zelândia",
        ["Belgium"] = "Bélgica",
        ["Egypt"] = "Egito",
        ["Saudi Arabia"] = "Arábia Saudita",
        ["Uruguay"] = "Uruguai",
        ["Spain"] = "Espanha",
        ["Cape Verde"] = "Cabo Verde",
        ["Iraq"] = "Iraque",
        ["Norway"] = "Noruega",
        ["France"] = "França",
        ["Senegal"] = "Senegal",
        ["Austria"] = "Áustria",
        ["Jordan"] = "Jordânia",
        ["Argentina"] = "Argentina",
        ["Algeria"] = "Argélia",
        ["Uzbekistan"] = "Uzbequistão",
        ["Colombia"] = "Colômbia",
        ["Portugal"] = "Portugal",
        ["DR Congo"] = "RD Congo",
        ["Ghana"] = "Gana",
        ["Panama"] = "Panamá",
        ["England"] = "Inglaterra",
        ["Croatia"] = "Croácia",
    };

    // ----- DTOs de desserializacao -----
    public record ImportResult(int Teams, int Players, int Matches, int MatchesEncerrados);

    private sealed class OfTeam
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("fifa_code")] public string FifaCode { get; set; } = "";
        [JsonPropertyName("group")] public string Group { get; set; } = "";
        [JsonPropertyName("flag_icon")] public string FlagIcon { get; set; } = "";
    }

    private sealed class OfMatchesFile
    {
        [JsonPropertyName("matches")] public List<OfMatch> Matches { get; set; } = new();
    }

    private sealed class OfMatch
    {
        [JsonPropertyName("date")] public string Date { get; set; } = "";
        [JsonPropertyName("time")] public string Time { get; set; } = "";
        [JsonPropertyName("team1")] public string Team1 { get; set; } = "";
        [JsonPropertyName("team2")] public string Team2 { get; set; } = "";
        [JsonPropertyName("group")] public string? Group { get; set; }
        [JsonPropertyName("score")] public OfScore? Score { get; set; }
    }

    private sealed class OfScore
    {
        [JsonPropertyName("ft")] public int[]? Ft { get; set; }
    }

    private sealed class OfSquad
    {
        [JsonPropertyName("fifa_code")] public string FifaCode { get; set; } = "";
        [JsonPropertyName("players")] public List<OfPlayer> Players { get; set; } = new();
    }

    private sealed class OfPlayer
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("pos")] public string Pos { get; set; } = "";
        [JsonPropertyName("number")] public int Number { get; set; }
    }
}
