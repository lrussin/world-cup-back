namespace WorldCup.Dtos;

// ---------- Auth ----------
public record RegisterRequest(string Nome, string Email, string Senha);
public record LoginRequest(string Email, string Senha);
public record UserDto(int Id, string Nome, string Email, bool IsAdmin, bool Pago);
public record AuthResponse(string Token, UserDto User);

// ---------- Referencia ----------
public record TeamDto(int Id, string Nome, string Grupo, string CodigoBandeira);
public record PlayerDto(int Id, string Nome, int TeamId, string TeamNome);

public record SettingsDto(
    DateTime GlobalLockUtc,
    bool ApostasGeraisTravadas,
    string RegraDesempate,
    int PontosPlacarExato,
    int PontosResultado,
    int PontosClassificacaoPorAcerto,
    int PontosCampeao,
    int PontosArtilheiro,
    int PontosMelhorJogador);

// ---------- Jogos / palpites ----------
public record MatchDto(
    int Id,
    string Grupo,
    DateTime DataHoraUtc,
    TeamDto HomeTeam,
    TeamDto AwayTeam,
    int? GolsMandante,
    int? GolsVisitante,
    bool Encerrado,
    bool Travado,
    PredictionDto? MeuPalpite);

public record PredictionDto(int MatchId, int GolsMandante, int GolsVisitante, int PontosObtidos);
public record SavePredictionRequest(int MatchId, int GolsMandante, int GolsVisitante);

/// <summary>Palpite de outro participante — visivel apenas apos o inicio da partida.</summary>
public record OtherPredictionDto(string Nome, int GolsMandante, int GolsVisitante, int PontosObtidos);

// ---------- Classificacao dos grupos ----------
public record GroupBetDto(string Grupo, int? PrimeiroTeamId, int? SegundoTeamId, int PontosObtidos);
public record SaveGroupBetItem(string Grupo, int PrimeiroTeamId, int SegundoTeamId);
public record SaveGroupBetsRequest(List<SaveGroupBetItem> Itens);

// ---------- Apostas especiais ----------
public record SpecialBetDto(
    int? CampeaoTeamId,
    int? ArtilheiroPlayerId,
    int? MelhorJogadorPlayerId,
    bool Bloqueado,
    int PontosObtidos);
public record SaveSpecialBetRequest(int CampeaoTeamId, int ArtilheiroPlayerId, int MelhorJogadorPlayerId);

/// <summary>Apostas especiais de um participante, para revelar aos demais (apos a trava).</summary>
public record ParticipantBetsDto(string Nome, string? Campeao, string? Artilheiro, string? MelhorJogador);

/// <summary>Palpite de classificacao (1o/2o de um grupo) de um participante, para revelar aos demais.</summary>
public record ParticipantGroupBetDto(string Nome, string Grupo, string? Primeiro, string? Segundo);

/// <summary>Placar ao vivo de um jogo em andamento.</summary>
public record LiveScoreDto(int MatchId, int GolsMandante, int GolsVisitante, int? Minuto, string Status);

/// <summary>Status de preenchimento das apostas de um participante (quem ja fez / quem falta).</summary>
public record BetStatusDto(string Nome, bool Especiais, bool Classificacao, int Palpites);

/// <summary>Status de palpites de um jogo: quem ja palpitou e quem falta.</summary>
public record MatchPredStatusDto(
    int MatchId, string Grupo, string Home, string Away, DateTime DataHoraUtc,
    bool Encerrado, int Feito, int Total, List<string> Faltam);

// ---------- Ranking ----------
public record RankingEntryDto(
    int Posicao,
    int UserId,
    string Nome,
    int PontosJogos,
    int PontosClassificacao,
    int PontosEspeciais,
    int PontosTotal,
    int PlacaresExatos,
    bool Lider);
public record RankingDto(string RegraDesempate, List<RankingEntryDto> Entries);

// ---------- Admin ----------
public record AdminUserDto(int Id, string Nome, string Email, bool IsAdmin, bool Pago, int PontosTotal);
public record SetPaymentRequest(bool Pago);
public record SetMatchResultRequest(int GolsMandante, int GolsVisitante);
public record SetGroupResultRequest(int PrimeiroTeamId, int SegundoTeamId);
public record SetTournamentResultRequest(int? CampeaoTeamId, int? ArtilheiroPlayerId, int? MelhorJogadorPlayerId);
public record UpdateSettingsRequest(
    DateTime? LockBetsAtUtc,
    string? RegraDesempate,
    int? PontosPlacarExato,
    int? PontosResultado,
    int? PontosClassificacaoPorAcerto,
    int? PontosCampeao,
    int? PontosArtilheiro,
    int? PontosMelhorJogador);
