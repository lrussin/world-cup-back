using WorldCup.Domain.Entities;

namespace WorldCup.Services;

/// <summary>
/// Calculo de pontos. Todas as regras de pontuacao ficam centralizadas aqui (funcoes puras)
/// para facilitar a leitura e o ajuste via Settings (ScoringConfig).
/// </summary>
public interface IScoringService
{
    int PontosDoPalpite(int golsMandante, int golsVisitante, int palpiteMandante, int palpiteVisitante, Settings cfg);
    int PontosClassificacao(GroupQualifierBet bet, GroupResult result, Settings cfg);
    int PontosEspeciais(SpecialBet bet, TournamentResult result, Settings cfg);
}

public class ScoringService : IScoringService
{
    /// <summary>
    /// Pontos de um palpite de placar contra o resultado oficial:
    ///  - Placar exato (gols mandante e visitante iguais): 5 pontos.
    ///  - Acertou so o resultado (vitoria/empate/derrota): 3 pontos.
    ///  - Errou: 0. (Quem nao palpitou nao tem Prediction, logo soma 0 naquele jogo.)
    /// </summary>
    public int PontosDoPalpite(int golsMandante, int golsVisitante, int palpiteMandante, int palpiteVisitante, Settings cfg)
    {
        if (palpiteMandante == golsMandante && palpiteVisitante == golsVisitante)
            return cfg.PontosPlacarExato;

        // Compara o "sinal" do placar (1 = mandante venceu, 0 = empate, -1 = visitante venceu).
        if (Math.Sign(palpiteMandante - palpiteVisitante) == Math.Sign(golsMandante - golsVisitante))
            return cfg.PontosResultado;

        return 0;
    }

    /// <summary>
    /// Classificacao do grupo: 3 pontos por acertar o time que terminou em 1o e 3 por acertar o 2o
    /// (posicional — o palpite de 1o precisa bater com o 1o oficial e idem para o 2o).
    /// </summary>
    public int PontosClassificacao(GroupQualifierBet bet, GroupResult result, Settings cfg)
    {
        var pontos = 0;
        if (bet.PrimeiroTeamId == result.PrimeiroTeamId) pontos += cfg.PontosClassificacaoPorAcerto;
        if (bet.SegundoTeamId == result.SegundoTeamId) pontos += cfg.PontosClassificacaoPorAcerto;
        return pontos;
    }

    /// <summary>Apostas especiais apuradas no fim: campeao 25, artilheiro 20, melhor jogador 15.</summary>
    public int PontosEspeciais(SpecialBet bet, TournamentResult result, Settings cfg)
    {
        var pontos = 0;
        if (result.CampeaoTeamId is { } c && bet.CampeaoTeamId == c) pontos += cfg.PontosCampeao;
        if (result.ArtilheiroPlayerId is { } a && bet.ArtilheiroPlayerId == a) pontos += cfg.PontosArtilheiro;
        if (result.MelhorJogadorPlayerId is { } m && bet.MelhorJogadorPlayerId == m) pontos += cfg.PontosMelhorJogador;
        return pontos;
    }
}
