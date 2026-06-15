namespace WorldCup.Domain.Enums;

/// <summary>
/// Fase do torneio. Por enquanto so a fase de Grupos esta implementada para palpites;
/// as demais ja existem no modelo para evolucao futura (mata-mata).
/// </summary>
public enum Fase
{
    Grupos = 0,
    Oitavas = 1,
    Quartas = 2,
    Semifinais = 3,
    TerceiroLugar = 4,
    Final = 5
}
