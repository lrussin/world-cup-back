using System.Globalization;
using WorldCup.Domain.Enums;

namespace WorldCup.Infrastructure;

/// <summary>
/// Estrutura ESTATICA do mata-mata 2026 (32 jogos, num 73..104), derivada do openfootball.
/// Define fase, kickoff e a "fiacao" de avanco (de onde vem cada lado nas fases >= Oitavas).
/// Os times da Rodada de 32 sao definidos manualmente pelo admin (Home/Away = null aqui).
/// </summary>
public static class BracketConfig
{
    /// <summary>Origem de um lado do confronto: vencedor (ou perdedor) de outro jogo.</summary>
    public record Source(int Num, bool Loser = false);

    /// <summary>Vaga da Rodada de 32: posicao de grupo (1o/2o de X) ou um dos melhores terceiros.</summary>
    public record R32Slot(int? Pos, string? Grupo, string[]? ThirdGroups)
    {
        public static R32Slot P(int pos, string grupo) => new(pos, grupo, null);
        public static R32Slot T(params string[] grupos) => new(null, null, grupos);
        public bool IsThird => ThirdGroups != null;
        public string Label => IsThird ? $"3º {string.Join("/", ThirdGroups!)}" : $"{Pos}º {Grupo}";
    }

    public record Game(int Num, Fase Fase, string Date, string Time, Source? Home, Source? Away,
        R32Slot? HomeR32 = null, R32Slot? AwayR32 = null);

    public static readonly IReadOnlyList<Game> Games = new[]
    {
        // ---- Rodada de 32 (vagas posicionais: 1o/2o resolvem pelos grupos; 3os = manual) ----
        new Game(73, Fase.RodadaDe32, "2026-06-28", "12:00 UTC-7", null, null, R32Slot.P(2, "A"), R32Slot.P(2, "B")),
        new Game(74, Fase.RodadaDe32, "2026-06-29", "16:30 UTC-4", null, null, R32Slot.P(1, "E"), R32Slot.T("A", "B", "C", "D", "F")),
        new Game(75, Fase.RodadaDe32, "2026-06-29", "19:00 UTC-6", null, null, R32Slot.P(1, "F"), R32Slot.P(2, "C")),
        new Game(76, Fase.RodadaDe32, "2026-06-29", "12:00 UTC-5", null, null, R32Slot.P(1, "C"), R32Slot.P(2, "F")),
        new Game(77, Fase.RodadaDe32, "2026-06-30", "17:00 UTC-4", null, null, R32Slot.P(1, "I"), R32Slot.T("C", "D", "F", "G", "H")),
        new Game(78, Fase.RodadaDe32, "2026-06-30", "12:00 UTC-5", null, null, R32Slot.P(2, "E"), R32Slot.P(2, "I")),
        new Game(79, Fase.RodadaDe32, "2026-06-30", "19:00 UTC-6", null, null, R32Slot.P(1, "A"), R32Slot.T("C", "E", "F", "H", "I")),
        new Game(80, Fase.RodadaDe32, "2026-07-01", "12:00 UTC-4", null, null, R32Slot.P(1, "L"), R32Slot.T("E", "H", "I", "J", "K")),
        new Game(81, Fase.RodadaDe32, "2026-07-01", "17:00 UTC-7", null, null, R32Slot.P(1, "D"), R32Slot.T("B", "E", "F", "I", "J")),
        new Game(82, Fase.RodadaDe32, "2026-07-01", "13:00 UTC-7", null, null, R32Slot.P(1, "G"), R32Slot.T("A", "E", "H", "I", "J")),
        new Game(83, Fase.RodadaDe32, "2026-07-02", "19:00 UTC-4", null, null, R32Slot.P(2, "K"), R32Slot.P(2, "L")),
        new Game(84, Fase.RodadaDe32, "2026-07-02", "12:00 UTC-7", null, null, R32Slot.P(1, "H"), R32Slot.P(2, "J")),
        new Game(85, Fase.RodadaDe32, "2026-07-02", "20:00 UTC-7", null, null, R32Slot.P(1, "B"), R32Slot.T("E", "F", "G", "I", "J")),
        new Game(86, Fase.RodadaDe32, "2026-07-03", "18:00 UTC-4", null, null, R32Slot.P(1, "J"), R32Slot.P(2, "H")),
        new Game(87, Fase.RodadaDe32, "2026-07-03", "20:30 UTC-5", null, null, R32Slot.P(1, "K"), R32Slot.T("D", "E", "I", "J", "L")),
        new Game(88, Fase.RodadaDe32, "2026-07-03", "13:00 UTC-5", null, null, R32Slot.P(2, "D"), R32Slot.P(2, "G")),

        // ---- Oitavas ----
        new Game(89, Fase.Oitavas, "2026-07-04", "17:00 UTC-4", new Source(74), new Source(77)),
        new Game(90, Fase.Oitavas, "2026-07-04", "12:00 UTC-5", new Source(73), new Source(75)),
        new Game(91, Fase.Oitavas, "2026-07-05", "16:00 UTC-4", new Source(76), new Source(78)),
        new Game(92, Fase.Oitavas, "2026-07-05", "18:00 UTC-6", new Source(79), new Source(80)),
        new Game(93, Fase.Oitavas, "2026-07-06", "14:00 UTC-5", new Source(83), new Source(84)),
        new Game(94, Fase.Oitavas, "2026-07-06", "17:00 UTC-7", new Source(81), new Source(82)),
        new Game(95, Fase.Oitavas, "2026-07-07", "12:00 UTC-4", new Source(86), new Source(88)),
        new Game(96, Fase.Oitavas, "2026-07-07", "13:00 UTC-7", new Source(85), new Source(87)),

        // ---- Quartas ----
        new Game(97, Fase.Quartas, "2026-07-09", "16:00 UTC-4", new Source(89), new Source(90)),
        new Game(98, Fase.Quartas, "2026-07-10", "12:00 UTC-7", new Source(93), new Source(94)),
        new Game(99, Fase.Quartas, "2026-07-11", "17:00 UTC-4", new Source(91), new Source(92)),
        new Game(100, Fase.Quartas, "2026-07-11", "20:00 UTC-5", new Source(95), new Source(96)),

        // ---- Semifinais ----
        new Game(101, Fase.Semifinais, "2026-07-14", "14:00 UTC-5", new Source(97), new Source(98)),
        new Game(102, Fase.Semifinais, "2026-07-15", "15:00 UTC-4", new Source(99), new Source(100)),

        // ---- 3o lugar (perdedores das semis) ----
        new Game(103, Fase.TerceiroLugar, "2026-07-18", "17:00 UTC-4", new Source(101, Loser: true), new Source(102, Loser: true)),

        // ---- Final ----
        new Game(104, Fase.Final, "2026-07-19", "15:00 UTC-4", new Source(101), new Source(102)),
    };

    public static readonly IReadOnlyDictionary<int, Game> ByNum = Games.ToDictionary(g => g.Num);

    public static string RoundLabel(Fase fase) => fase switch
    {
        Fase.RodadaDe32 => "Rodada de 32",
        Fase.Oitavas => "Oitavas",
        Fase.Quartas => "Quartas",
        Fase.Semifinais => "Semifinais",
        Fase.TerceiroLugar => "3o lugar",
        Fase.Final => "Final",
        _ => "",
    };

    /// <summary>"2026-06-28" + "12:00 UTC-7" => DateTime UTC.</summary>
    public static DateTime ParseUtc(string date, string time)
    {
        var d = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var parts = time.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hm = TimeOnly.Parse(parts[0], CultureInfo.InvariantCulture);
        var offset = 0;
        if (parts.Length > 1)
            int.TryParse(parts[1].Replace("UTC", "", StringComparison.OrdinalIgnoreCase),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out offset);
        var local = d.ToDateTime(hm);
        return DateTime.SpecifyKind(local.AddHours(-offset), DateTimeKind.Utc);
    }
}
