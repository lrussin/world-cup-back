using System.Globalization;
using System.Text;
using WorldCup.Domain.Entities;
using WorldCup.Domain.Enums;

namespace WorldCup.Infrastructure;

/// <summary>
/// Seed idempotente. Dividido em:
///  - <see cref="SeedBaseline"/>: configuracao global + usuarios demo (sempre).
///  - <see cref="SeedPlaceholderFixtures"/>: times/jogadores/jogos PLACEHOLDER, usado como
///    fallback quando nao da para importar os dados reais (openfootball) — ex.: sem internet.
///
/// No startup o app tenta importar os dados reais (ver OpenFootballImporter); so cai no
/// placeholder se a importacao falhar.
/// </summary>
public static class DbInitializer
{
    // 12 grupos x 4 selecoes (placeholder organizado por pote). Codigo = bandeira (flagcdn).
    private static readonly (string Grupo, (string Nome, string Code)[] Times)[] Grupos =
    {
        ("A", new[] { ("Brasil", "br"), ("Colombia", "co"), ("Ira", "ir"), ("Argelia", "dz") }),
        ("B", new[] { ("Argentina", "ar"), ("Mexico", "mx"), ("Arabia Saudita", "sa"), ("Costa do Marfim", "ci") }),
        ("C", new[] { ("Franca", "fr"), ("Estados Unidos", "us"), ("Catar", "qa"), ("Costa Rica", "cr") }),
        ("D", new[] { ("Inglaterra", "gb-eng"), ("Dinamarca", "dk"), ("Canada", "ca"), ("Pais de Gales", "gb-wls") }),
        ("E", new[] { ("Espanha", "es"), ("Suica", "ch"), ("Equador", "ec"), ("Escocia", "gb-sct") }),
        ("F", new[] { ("Belgica", "be"), ("Servia", "rs"), ("Peru", "pe"), ("Suecia", "se") }),
        ("G", new[] { ("Portugal", "pt"), ("Polonia", "pl"), ("Chile", "cl"), ("Noruega", "no") }),
        ("H", new[] { ("Holanda", "nl"), ("Senegal", "sn"), ("Nigeria", "ng"), ("Austria", "at") }),
        ("I", new[] { ("Italia", "it"), ("Marrocos", "ma"), ("Gana", "gh"), ("Ucrania", "ua") }),
        ("J", new[] { ("Alemanha", "de"), ("Japao", "jp"), ("Camaroes", "cm"), ("Turquia", "tr") }),
        ("K", new[] { ("Croacia", "hr"), ("Coreia do Sul", "kr"), ("Egito", "eg"), ("Grecia", "gr") }),
        ("L", new[] { ("Uruguai", "uy"), ("Australia", "au"), ("Tunisia", "tn"), ("Paraguai", "py") }),
    };

    // Pares por rodada para 4 times [0,1,2,3]: cada time joga 1x por rodada (3 rodadas = 6 jogos/grupo).
    private static readonly (int Home, int Away)[][] RodadasPorGrupo =
    {
        new[] { (0, 1), (2, 3) },
        new[] { (0, 2), (3, 1) },
        new[] { (3, 0), (1, 2) },
    };

    /// <summary>Senha padrao dos participantes do bolao (cadastro fechado).</summary>
    public const string SenhaPadraoParticipante = "Bolao@2026";

    /// <summary>Participantes reais do bolao (nome, email). Apenas estes acessam.</summary>
    private static readonly (string Nome, string Email)[] Participantes =
    {
        ("Toninho",   "toniind15330203@gmail.com"),
        ("Maicon",    "maiconmix1103@gmail.com"),
        ("Jefferson", "jefferson.vieiraaa27@gmail.com"),
        ("Ariel",     "arialsharon@gmail.com"),
        ("Guilherme", "guilhermeflaviano95@gmail.com"),
        ("Maike",     "alvesmaike019@gmail.com"),
        ("Russin",    "lucas.henri.sfs@hotmail.com"),
        ("Arthur",    "arthuralexandre451@gmail.com"),
        ("Gabriel",   "juliogabriel1581@gmail.com"),
        ("Paulo",     "paulovini669@gmail.com"),
        ("Leiz",      "leizcarvalho@gmail.com"),
        ("Pedro",     "pedro.silva.r12@gmail.com"),
    };

    /// <summary>Configuracao global + admin + participantes. Sempre roda (idempotente por email).</summary>
    public static void SeedBaseline(AppDbContext db)
    {
        if (!db.Settings.Any())
        {
            db.Settings.Add(new Settings
            {
                // Prazo das apostas de classificacao/especiais. O importador ajusta para o
                // kickoff do primeiro jogo.
                LockBetsAtUtc = DateTime.UtcNow.AddDays(5)
            });
            db.SaveChanges();
        }

        // Admin com acesso total. Fica como NAO pago para nao aparecer no ranking (conta de gestao).
        SeedUser(db, "Administrador", "admin@bolao.com", "Admin@2026", isAdmin: true, pago: false);

        // Participantes (nao-admin), todos ja marcados como PAGOS.
        foreach (var (nome, email) in Participantes)
            SeedUser(db, nome, email, SenhaPadraoParticipante, isAdmin: false, pago: true);
    }

    /// <summary>Times/jogadores/jogos PLACEHOLDER (fallback offline). So roda se nao houver times.</summary>
    public static void SeedPlaceholderFixtures(AppDbContext db)
    {
        if (db.Teams.Any())
            return;

        foreach (var (grupo, times) in Grupos)
        {
            foreach (var (nome, code) in times)
            {
                var team = new Team { Nome = nome, Grupo = grupo, CodigoBandeira = code };
                team.Players.Add(new Player { Nome = $"{nome} - Jogador 9" });
                team.Players.Add(new Player { Nome = $"{nome} - Jogador 10" });
                team.Players.Add(new Player { Nome = $"{nome} - Jogador 7" });
                db.Teams.Add(team);
            }
        }
        db.SaveChanges();

        // Agenda relativa ao seed: ~16 jogos com kickoff passado (travados) e o restante aberto.
        var baseUtc = DateTime.UtcNow.AddDays(-2);
        var slot = 0;
        for (var rodada = 0; rodada < RodadasPorGrupo.Length; rodada++)
        {
            foreach (var (grupo, _) in Grupos)
            {
                var teams = db.Teams.Where(t => t.Grupo == grupo).OrderBy(t => t.Id).ToList();
                foreach (var (home, away) in RodadasPorGrupo[rodada])
                {
                    db.Matches.Add(new Match
                    {
                        HomeTeamId = teams[home].Id,
                        AwayTeamId = teams[away].Id,
                        Fase = Fase.Grupos,
                        Grupo = grupo,
                        DataHoraUtc = baseUtc.AddHours(slot * 3),
                        Encerrado = false
                    });
                    slot++;
                }
            }
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Importa as escolhas de ARTILHEIRO do formulario (por email -> jogador). Idempotente:
    /// so preenche quando o usuario ainda nao tem artilheiro (nao sobrescreve ajustes posteriores).
    /// Conflito do formulario: guilhermeflaviano95 votou em Mbappe e em "Duoe" (jogador inexistente);
    /// gravamos Mbappe, a opcao valida.
    /// </summary>
    public static void SeedSpecialBetsArtilheiro(AppDbContext db)
    {
        // email -> termo (sem acento, minusculo) que identifica o jogador escolhido.
        var picks = new (string Email, string Termo)[]
        {
            ("maiconmix1103@gmail.com",        "kylian mbappe"),
            ("arialsharon@gmail.com",          "kylian mbappe"),
            ("toniind15330203@gmail.com",      "kylian mbappe"),
            ("juliogabriel1581@gmail.com",     "kylian mbappe"),
            ("guilhermeflaviano95@gmail.com",  "desire doue"),
            ("arthuralexandre451@gmail.com",   "kylian mbappe"),
            ("lucas.henri.sfs@hotmail.com",    "kylian mbappe"),
            ("paulovini669@gmail.com",         "harry kane"),
            ("alvesmaike019@gmail.com",        "vinicius junior"),
            ("jefferson.vieiraaa27@gmail.com", "endrick"),
            ("leizcarvalho@gmail.com",         "harry kane"),
            ("pedro.silva.r12@gmail.com",      "kylian mbappe"),
        };

        var players = db.Players.ToList();
        foreach (var (email, termo) in picks)
        {
            var alvo = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email == alvo);
            if (user is null) continue;

            var player = players.FirstOrDefault(p => Normalizar(p.Nome).Contains(termo));
            if (player is null) continue;

            var bet = db.SpecialBets.FirstOrDefault(s => s.UserId == user.Id);
            if (bet is null)
            {
                bet = new SpecialBet { UserId = user.Id, CriadoEm = DateTime.UtcNow };
                db.SpecialBets.Add(bet);
            }
            if (bet.ArtilheiroPlayerId is null)
                bet.ArtilheiroPlayerId = player.Id;
        }
        db.SaveChanges();
    }

    /// <summary>Importa as escolhas de MELHOR JOGADOR do formulario (por email -> jogador). Idempotente.</summary>
    public static void SeedSpecialBetsMelhorJogador(AppDbContext db)
    {
        var picks = new (string Email, string Termo)[]
        {
            ("maiconmix1103@gmail.com",        "kylian mbappe"),
            ("lucas.henri.sfs@hotmail.com",    "kylian mbappe"),
            ("juliogabriel1581@gmail.com",     "kylian mbappe"),
            ("guilhermeflaviano95@gmail.com",  "kylian mbappe"),
            ("paulovini669@gmail.com",         "lamine yamal"),
            ("arialsharon@gmail.com",          "lamine yamal"),
            ("toniind15330203@gmail.com",      "lamine yamal"),
            ("arthuralexandre451@gmail.com",   "lamine yamal"),
            ("alvesmaike019@gmail.com",        "vinicius junior"),
            ("jefferson.vieiraaa27@gmail.com", "neymar"),
            ("leizcarvalho@gmail.com",         "cristiano ronaldo"),
            ("pedro.silva.r12@gmail.com",      "lamine yamal"),
        };

        var players = db.Players.ToList();
        foreach (var (email, termo) in picks)
        {
            var alvo = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email == alvo);
            if (user is null) continue;

            var player = players.FirstOrDefault(p => Normalizar(p.Nome).Contains(termo));
            if (player is null) continue;

            var bet = db.SpecialBets.FirstOrDefault(s => s.UserId == user.Id);
            if (bet is null)
            {
                bet = new SpecialBet { UserId = user.Id, CriadoEm = DateTime.UtcNow };
                db.SpecialBets.Add(bet);
            }
            if (bet.MelhorJogadorPlayerId is null)
                bet.MelhorJogadorPlayerId = player.Id;
        }
        db.SaveChanges();
    }

    /// <summary>Importa as escolhas de CAMPEAO (selecao) do formulario. Idempotente.</summary>
    public static void SeedSpecialBetsCampeao(AppDbContext db)
    {
        // email -> nome do time (em ingles, como vem do openfootball; normalizado).
        var picks = new (string Email, string Time)[]
        {
            ("lucas.henri.sfs@hotmail.com",    "franca"),
            ("arialsharon@gmail.com",          "franca"),
            ("toniind15330203@gmail.com",      "franca"),
            ("juliogabriel1581@gmail.com",     "franca"),
            ("guilhermeflaviano95@gmail.com",  "franca"),
            ("alvesmaike019@gmail.com",        "brasil"),
            ("jefferson.vieiraaa27@gmail.com", "brasil"),
            ("maiconmix1103@gmail.com",        "portugal"),
            ("paulovini669@gmail.com",         "portugal"),
            ("arthuralexandre451@gmail.com",   "espanha"),
            ("leizcarvalho@gmail.com",         "brasil"),
            ("pedro.silva.r12@gmail.com",      "espanha"),
        };

        var teams = db.Teams.ToList();
        foreach (var (email, time) in picks)
        {
            var alvo = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email == alvo);
            if (user is null) continue;

            var team = teams.FirstOrDefault(t => Normalizar(t.Nome) == time);
            if (team is null) continue;

            var bet = db.SpecialBets.FirstOrDefault(s => s.UserId == user.Id);
            if (bet is null)
            {
                bet = new SpecialBet { UserId = user.Id, CriadoEm = DateTime.UtcNow };
                db.SpecialBets.Add(bet);
            }
            if (bet.CampeaoTeamId is null)
                bet.CampeaoTeamId = team.Id;
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Resultados oficiais lancados manualmente (sobrescreve o que veio do openfootball).
    /// Roda ANTES de SeedMatchPredictions, para que os palpites desses jogos sejam apurados.
    /// </summary>
    public static void SeedMatchResults(AppDbContext db)
    {
        // (mandante, visitante, golsMandante, golsVisitante)
        var resultados = new (string Home, string Away, int Gh, int Ga)[]
        {
            ("Catar",  "Suíça",    1, 1),
            ("Brasil", "Marrocos", 1, 1),
            ("Haiti",  "Escócia",  0, 1),
        };

        var teams = db.Teams.ToList();
        var matches = db.Matches.ToList();
        foreach (var (home, away, gh, ga) in resultados)
        {
            var homeTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == Normalizar(home));
            var awayTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == Normalizar(away));
            if (homeTeam is null || awayTeam is null) continue;

            var match = matches.FirstOrDefault(m => m.HomeTeamId == homeTeam.Id && m.AwayTeamId == awayTeam.Id);
            // Nao sobrescreve resultado ja lancado (ex.: pelo admin na plataforma).
            if (match is null || match.Encerrado) continue;

            match.GolsMandante = gh;
            match.GolsVisitante = ga;
            match.Encerrado = true;
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Importa palpites de placar de jogos especificos (por email). Se o jogo ja tem resultado
    /// oficial, apura os pontos (5 exato / 3 resultado / 0). Upsert idempotente.
    /// </summary>
    public static void SeedMatchPredictions(AppDbContext db)
    {
        var settings = db.Settings.FirstOrDefault();
        var ptExato = settings?.PontosPlacarExato ?? 5;
        var ptResultado = settings?.PontosResultado ?? 3;

        // (mandante, visitante, [(email, golsMandante, golsVisitante)])
        var jogos = new (string Home, string Away, (string Email, int Gh, int Ga)[] Picks)[]
        {
            ("México", "África do Sul", new (string, int, int)[]
            {
                ("arialsharon@gmail.com",          2, 1),
                ("paulovini669@gmail.com",         1, 1),
                ("juliogabriel1581@gmail.com",     2, 0),
                ("maiconmix1103@gmail.com",        2, 1),
                ("alvesmaike019@gmail.com",        2, 0),
                ("jefferson.vieiraaa27@gmail.com", 2, 0),
                ("guilhermeflaviano95@gmail.com",  2, 1),
                ("lucas.henri.sfs@hotmail.com",    2, 1),
                ("toniind15330203@gmail.com",      2, 1),
                ("arthuralexandre451@gmail.com",   2, 0),
            }),
            ("Coreia do Sul", "República Tcheca", new (string, int, int)[]
            {
                ("arialsharon@gmail.com",          1, 1),
                ("paulovini669@gmail.com",         1, 1),
                ("juliogabriel1581@gmail.com",     1, 1),
                ("maiconmix1103@gmail.com",        0, 1),
                ("alvesmaike019@gmail.com",        1, 0),
                ("jefferson.vieiraaa27@gmail.com", 1, 2),
                ("guilhermeflaviano95@gmail.com",  1, 1),
                ("lucas.henri.sfs@hotmail.com",    3, 1),
                ("toniind15330203@gmail.com",      1, 0),
                ("arthuralexandre451@gmail.com",   1, 1),
            }),
            ("Canadá", "Bósnia e Herzegovina", new (string, int, int)[]
            {
                ("toniind15330203@gmail.com",      2, 1),
                ("maiconmix1103@gmail.com",        1, 0),
                ("jefferson.vieiraaa27@gmail.com", 1, 1),
                ("arialsharon@gmail.com",          1, 0),
                ("guilhermeflaviano95@gmail.com",  1, 1),
                ("alvesmaike019@gmail.com",        2, 0),
                ("lucas.henri.sfs@hotmail.com",    2, 0),
                ("arthuralexandre451@gmail.com",   1, 1),
                ("juliogabriel1581@gmail.com",     2, 1),
                ("paulovini669@gmail.com",         2, 1),
            }),
            ("Estados Unidos", "Paraguai", new (string, int, int)[]
            {
                ("toniind15330203@gmail.com",      1, 1),
                ("maiconmix1103@gmail.com",        2, 1),
                ("jefferson.vieiraaa27@gmail.com", 2, 1),
                ("arialsharon@gmail.com",          2, 0),
                ("guilhermeflaviano95@gmail.com",  2, 2),
                ("alvesmaike019@gmail.com",        1, 2),
                ("lucas.henri.sfs@hotmail.com",    2, 1),
                ("arthuralexandre451@gmail.com",   1, 2),
                ("juliogabriel1581@gmail.com",     2, 0),
                ("paulovini669@gmail.com",         2, 1),
            }),
            ("Alemanha", "Curacao", new (string, int, int)[]
            {
                ("lucas.henri.sfs@hotmail.com",    3, 0),
            }),
        };

        var teams = db.Teams.ToList();
        var matches = db.Matches.ToList();
        foreach (var (home, away, picks) in jogos)
        {
            var homeNorm = Normalizar(home);
            var awayNorm = Normalizar(away);
            var homeTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == homeNorm);
            var awayTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == awayNorm);
            if (homeTeam is null || awayTeam is null) continue;

            var match = matches.FirstOrDefault(m => m.HomeTeamId == homeTeam.Id && m.AwayTeamId == awayTeam.Id);
            if (match is null) continue;

            foreach (var (email, gh, ga) in picks)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant());
                if (user is null) continue;

                var pred = db.Predictions.FirstOrDefault(p => p.UserId == user.Id && p.MatchId == match.Id);
                if (pred is null)
                {
                    pred = new Prediction { UserId = user.Id, MatchId = match.Id, CriadoEm = DateTime.UtcNow };
                    db.Predictions.Add(pred);
                }
                pred.GolsMandantePalpite = gh;
                pred.GolsVisitantePalpite = ga;
                pred.AtualizadoEm = DateTime.UtcNow;

                // Apura se o jogo ja tem resultado oficial.
                if (match.Encerrado && match.GolsMandante is int rh && match.GolsVisitante is int ra)
                {
                    pred.PontosObtidos = gh == rh && ga == ra
                        ? ptExato
                        : Math.Sign(gh - ga) == Math.Sign(rh - ra)
                            ? ptResultado
                            : 0;
                }
            }
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Importa os palpites de classificacao (1o/2o de cada grupo) do formulario (planilha v2).
    /// Apenas grupos preenchidos; "x" no formulario = nao palpitou. Upsert idempotente.
    /// </summary>
    public static void SeedGroupBets(AppDbContext db)
    {
        // email -> [(grupo, primeiro, segundo)] — nomes em PT (sem acento ok, casa via Normalizar).
        var dados = new (string Email, (string Grupo, string Primeiro, string Segundo)[] Picks)[]
        {
            ("arialsharon@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Holanda","Suecia"),
                ("G","Belgica","Egito"), ("H","Espanha","Arabia Saudita"), ("I","Franca","Noruega"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("alvesmaike019@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Japao","Holanda"),
                ("G","Belgica","Ira"), ("H","Espanha","Arabia Saudita"), ("I","Franca","Senegal"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Gana") }),
            ("jefferson.vieiraaa27@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Japao","Holanda"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Noruega"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("maiconmix1103@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Holanda","Japao"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Senegal"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("lucas.henri.sfs@hotmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Costa do Marfim"), ("F","Holanda","Japao"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Senegal"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("paulovini669@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Costa do Marfim"), ("F","Holanda","Japao"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Noruega"),
                ("J","Argentina","Argelia"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("juliogabriel1581@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Holanda","Japao"),
                ("G","Belgica","Ira"), ("H","Espanha","Uruguai"), ("I","Franca","Senegal"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("arthuralexandre451@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Costa do Marfim"), ("F","Holanda","Japao"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Senegal"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("leizcarvalho@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Equador"), ("F","Holanda","Suecia"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Noruega"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Inglaterra","Croacia") }),
            ("pedro.silva.r12@gmail.com", new[] {
                ("C","Brasil","Escocia"), ("E","Equador","Alemanha"), ("F","Holanda","Tunisia"),
                ("G","Egito","Belgica"), ("H","Espanha","Uruguai"), ("I","Franca","Noruega"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Croacia","Inglaterra") }),
            ("guilhermeflaviano95@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Costa do Marfim"), ("F","Japao","Holanda"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Senegal"),
                ("J","Argentina","Argelia"), ("K","Portugal","Colombia"), ("L","Inglaterra","Gana") }),
            ("toniind15330203@gmail.com", new[] {
                ("C","Brasil","Marrocos"), ("E","Alemanha","Costa do Marfim"), ("F","Suecia","Japao"),
                ("G","Belgica","Egito"), ("H","Espanha","Uruguai"), ("I","Franca","Noruega"),
                ("J","Argentina","Austria"), ("K","Portugal","Colombia"), ("L","Croacia","Inglaterra") }),
        };

        var teams = db.Teams.ToList();
        foreach (var (email, picks) in dados)
        {
            var alvo = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email == alvo);
            if (user is null) continue;

            foreach (var (grupo, primeiro, segundo) in picks)
            {
                var pT = teams.FirstOrDefault(t => t.Grupo == grupo && Normalizar(t.Nome) == Normalizar(primeiro));
                var sT = teams.FirstOrDefault(t => t.Grupo == grupo && Normalizar(t.Nome) == Normalizar(segundo));
                if (pT is null || sT is null) continue;

                var bet = db.GroupQualifierBets.FirstOrDefault(b => b.UserId == user.Id && b.Grupo == grupo);
                if (bet is null)
                {
                    bet = new GroupQualifierBet { UserId = user.Id, Grupo = grupo };
                    db.GroupQualifierBets.Add(bet);
                }
                bet.PrimeiroTeamId = pT.Id;
                bet.SegundoTeamId = sT.Id;
            }
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Importa jogos da planilha bolao_jogos: resultado oficial (SOBRESCREVE — planilha e a fonte da
    /// verdade) + palpites de cada participante (upsert, ja apurados 5/3/0). "x" = sem palpite.
    /// </summary>
    public static void SeedJogos(AppDbContext db)
    {
        var settings = db.Settings.FirstOrDefault();
        var ptExato = settings?.PontosPlacarExato ?? 5;
        var ptResultado = settings?.PontosResultado ?? 3;

        // (mandante, visitante, golsCasa, golsFora, [(email, palpiteCasa, palpiteFora)])
        var jogos = new (string Home, string Away, int? Gh, int? Ga, (string Email, int Ph, int Pa)[] P)[]
        {
            ("Mexico", "Africa do Sul", 2, 0, new (string, int, int)[] {
                ("arialsharon@gmail.com",2,1),("alvesmaike019@gmail.com",2,0),("guilhermeflaviano95@gmail.com",2,1),
                ("jefferson.vieiraaa27@gmail.com",2,0),("maiconmix1103@gmail.com",2,1),("toniind15330203@gmail.com",2,1),
                ("lucas.henri.sfs@hotmail.com",2,1),("paulovini669@gmail.com",1,1),("juliogabriel1581@gmail.com",2,0),
                ("arthuralexandre451@gmail.com",2,0) }),
            ("Coreia do Sul", "Republica Tcheca", 2, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",1,1),("alvesmaike019@gmail.com",1,0),("guilhermeflaviano95@gmail.com",1,1),
                ("jefferson.vieiraaa27@gmail.com",1,2),("maiconmix1103@gmail.com",0,1),("toniind15330203@gmail.com",1,0),
                ("lucas.henri.sfs@hotmail.com",3,1),("paulovini669@gmail.com",1,1),("juliogabriel1581@gmail.com",1,1),
                ("arthuralexandre451@gmail.com",1,1) }),
            ("Canada", "Bosnia e Herzegovina", 1, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",1,0),("alvesmaike019@gmail.com",2,0),("guilhermeflaviano95@gmail.com",1,1),
                ("jefferson.vieiraaa27@gmail.com",1,1),("maiconmix1103@gmail.com",1,0),("toniind15330203@gmail.com",2,1),
                ("lucas.henri.sfs@hotmail.com",2,0),("paulovini669@gmail.com",2,1),("juliogabriel1581@gmail.com",2,1),
                ("arthuralexandre451@gmail.com",1,1) }),
            ("Estados Unidos", "Paraguai", 4, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",2,0),("alvesmaike019@gmail.com",1,2),("guilhermeflaviano95@gmail.com",2,2),
                ("jefferson.vieiraaa27@gmail.com",2,1),("maiconmix1103@gmail.com",2,1),("toniind15330203@gmail.com",1,1),
                ("lucas.henri.sfs@hotmail.com",2,1),("paulovini669@gmail.com",2,1),("juliogabriel1581@gmail.com",2,0),
                ("arthuralexandre451@gmail.com",0,1) }),
            ("Catar", "Suica", 1, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",0,2),("alvesmaike019@gmail.com",2,0),("guilhermeflaviano95@gmail.com",2,1),
                ("maiconmix1103@gmail.com",0,3),("toniind15330203@gmail.com",1,3),("lucas.henri.sfs@hotmail.com",1,2),
                ("juliogabriel1581@gmail.com",0,3),("arthuralexandre451@gmail.com",1,2),("leizcarvalho@gmail.com",0,3) }),
            ("Brasil", "Marrocos", 1, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",2,0),("alvesmaike019@gmail.com",2,0),("guilhermeflaviano95@gmail.com",1,1),
                ("jefferson.vieiraaa27@gmail.com",2,1),("maiconmix1103@gmail.com",0,1),("toniind15330203@gmail.com",3,1),
                ("lucas.henri.sfs@hotmail.com",2,0),("paulovini669@gmail.com",2,0),("juliogabriel1581@gmail.com",2,1),
                ("arthuralexandre451@gmail.com",3,1),("leizcarvalho@gmail.com",2,1),("pedro.silva.r12@gmail.com",3,0) }),
            ("Haiti", "Escocia", 0, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",0,3),("alvesmaike019@gmail.com",3,1),("guilhermeflaviano95@gmail.com",1,2),
                ("jefferson.vieiraaa27@gmail.com",0,2),("maiconmix1103@gmail.com",0,3),("toniind15330203@gmail.com",1,1),
                ("lucas.henri.sfs@hotmail.com",0,1),("paulovini669@gmail.com",1,2),("juliogabriel1581@gmail.com",1,2),
                ("arthuralexandre451@gmail.com",0,0),("leizcarvalho@gmail.com",0,3),("pedro.silva.r12@gmail.com",0,1) }),
            ("Australia", "Turquia", 2, 0, new (string, int, int)[] {
                ("arialsharon@gmail.com",0,2),("alvesmaike019@gmail.com",1,0),("guilhermeflaviano95@gmail.com",1,2),
                ("jefferson.vieiraaa27@gmail.com",1,1),("maiconmix1103@gmail.com",1,2),("toniind15330203@gmail.com",1,0),
                ("lucas.henri.sfs@hotmail.com",0,2),("paulovini669@gmail.com",0,1),("juliogabriel1581@gmail.com",2,1),
                ("arthuralexandre451@gmail.com",0,1),("leizcarvalho@gmail.com",0,1),("pedro.silva.r12@gmail.com",2,0) }),
            ("Alemanha", "Curacao", 7, 1, new (string, int, int)[] {
                ("arialsharon@gmail.com",3,0),("alvesmaike019@gmail.com",3,0),("guilhermeflaviano95@gmail.com",5,0),
                ("jefferson.vieiraaa27@gmail.com",5,0),("maiconmix1103@gmail.com",3,0),("toniind15330203@gmail.com",3,0),
                ("lucas.henri.sfs@hotmail.com",3,0),("paulovini669@gmail.com",3,0),("juliogabriel1581@gmail.com",3,0),
                ("arthuralexandre451@gmail.com",3,0),("leizcarvalho@gmail.com",5,0),("pedro.silva.r12@gmail.com",3,0) }),
            ("Holanda", "Japao", 2, 2, new (string, int, int)[] {
                ("arialsharon@gmail.com",3,0),("alvesmaike019@gmail.com",1,2),("guilhermeflaviano95@gmail.com",1,2),
                ("jefferson.vieiraaa27@gmail.com",1,2),("maiconmix1103@gmail.com",1,1),("toniind15330203@gmail.com",1,2),
                ("lucas.henri.sfs@hotmail.com",2,1),("paulovini669@gmail.com",2,1),("juliogabriel1581@gmail.com",2,1),
                ("arthuralexandre451@gmail.com",3,1),("leizcarvalho@gmail.com",3,2),("pedro.silva.r12@gmail.com",3,1) }),
            ("Costa do Marfim", "Equador", 1, 0, new (string, int, int)[] {
                ("arialsharon@gmail.com",0,3),("alvesmaike019@gmail.com",0,2),("guilhermeflaviano95@gmail.com",2,2),
                ("jefferson.vieiraaa27@gmail.com",1,2),("maiconmix1103@gmail.com",2,2),("toniind15330203@gmail.com",2,0),
                ("lucas.henri.sfs@hotmail.com",1,0),("paulovini669@gmail.com",1,1),("juliogabriel1581@gmail.com",1,1),
                ("arthuralexandre451@gmail.com",2,0),("leizcarvalho@gmail.com",0,1),("pedro.silva.r12@gmail.com",0,1) }),
            ("Suecia", "Tunisia", null, null, new (string, int, int)[] {
                ("arialsharon@gmail.com",3,0),("alvesmaike019@gmail.com",2,0),("guilhermeflaviano95@gmail.com",2,1),
                ("jefferson.vieiraaa27@gmail.com",1,2),("maiconmix1103@gmail.com",1,0),("toniind15330203@gmail.com",2,1),
                ("lucas.henri.sfs@hotmail.com",1,0),("paulovini669@gmail.com",2,1),("juliogabriel1581@gmail.com",2,0),
                ("arthuralexandre451@gmail.com",1,0),("leizcarvalho@gmail.com",1,1),("pedro.silva.r12@gmail.com",2,1) }),
        };

        var teams = db.Teams.ToList();
        var matches = db.Matches.ToList();
        foreach (var (home, away, gh, ga, palpites) in jogos)
        {
            var homeTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == Normalizar(home));
            var awayTeam = teams.FirstOrDefault(t => Normalizar(t.Nome) == Normalizar(away));
            if (homeTeam is null || awayTeam is null) continue;

            var match = matches.FirstOrDefault(m => m.HomeTeamId == homeTeam.Id && m.AwayTeamId == awayTeam.Id);
            if (match is null) continue;

            // Resultado oficial (planilha sobrescreve). Sem placar (null) = jogo ainda nao aconteceu.
            var temResultado = gh.HasValue && ga.HasValue;
            match.GolsMandante = gh;
            match.GolsVisitante = ga;
            match.Encerrado = temResultado;

            foreach (var (email, ph, pa) in palpites)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant());
                if (user is null) continue;

                var pred = db.Predictions.FirstOrDefault(p => p.UserId == user.Id && p.MatchId == match.Id);
                if (pred is null)
                {
                    pred = new Prediction { UserId = user.Id, MatchId = match.Id, CriadoEm = DateTime.UtcNow };
                    db.Predictions.Add(pred);
                }
                pred.GolsMandantePalpite = ph;
                pred.GolsVisitantePalpite = pa;
                pred.AtualizadoEm = DateTime.UtcNow;
                pred.PontosObtidos = !temResultado
                    ? 0
                    : ph == gh!.Value && pa == ga!.Value
                        ? ptExato
                        : Math.Sign(ph - pa) == Math.Sign(gh!.Value - ga!.Value)
                            ? ptResultado
                            : 0;
            }
        }
        db.SaveChanges();
    }

    /// <summary>Remove acentos e baixa para minusculo (para casar nomes independente de acentuacao).</summary>
    private static string Normalizar(string s)
    {
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().ToLowerInvariant();
    }

    private static void SeedUser(AppDbContext db, string nome, string email, string senha, bool isAdmin, bool pago)
    {
        email = email.Trim().ToLowerInvariant();
        if (db.Users.Any(u => u.Email == email))
            return;

        db.Users.Add(new User
        {
            Nome = nome,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(senha),
            IsAdmin = isAdmin,
            Pago = pago
        });
        db.SaveChanges();
    }
}
