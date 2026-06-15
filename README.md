# 🏆 Bolão da Copa do Mundo 2026

Aplicação completa de bolão: participantes fazem login, registram palpites (placar dos jogos da
fase de grupos, classificação 1º/2º de cada grupo e apostas especiais) e disputam um ranking.
**Vencedor único:** quem terminar com a maior pontuação total leva o prêmio.

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core **Code-First + Migrations**, SQL Server, JWT + BCrypt.
- **Frontend:** Angular 20 (standalone components), HttpClient com interceptor de JWT.
- **Banco:** SQL Server (por padrão **LocalDB**).

Repositórios:
- Backend: `world-cup-back/` (projeto em `world-cup-back/WorldCup/`)
- Frontend: `world-cup-front/`

---

## Pré-requisitos

| Ferramenta | Versão usada | Observação |
|---|---|---|
| .NET SDK | 10.x | `dotnet --version` |
| SQL Server | LocalDB (`(localdb)\MSSQLLocalDB`) | vem com o Visual Studio / SQL Server Express |
| Node.js | 24.x | Angular 20 exige Node ≥ 22.12 / 24 |
| npm | 11.x | |

> O backend já inclui um `NuGet.config` que usa **apenas o nuget.org** (ignora feeds privados
> herdados da máquina), para o restore funcionar sem autenticação.

---

## 1) Backend

```powershell
cd world-cup-back/WorldCup

# Restaura e roda. No startup o app APLICA as migrations e faz o SEED automaticamente.
dotnet run --launch-profile http
```

- API: **http://localhost:5145** (HTTPS opcional em https://localhost:7116)
- A connection string fica em `appsettings.json` → `ConnectionStrings:Default`
  (padrão: `(localdb)\MSSQLLocalDB`, banco `WorldCupBolao`). Ajuste se usar outro SQL Server.

### Migrations (Code-First)

A migration inicial já está em `Infrastructure/Migrations/`. Comandos úteis:

```powershell
# Aplicar as migrations manualmente (alternativa ao auto-migrate do startup)
dotnet ef database update

# Gerar o SCRIPT SQL de criação do banco
dotnet ef migrations script -o ../script-criacao-banco.sql

# Recriar do zero (apaga o banco; o próximo "dotnet run" recria e re-semeia)
dotnet ef database drop -f
```

### Seed / Dados reais (openfootball) — automático

Sempre são criados a **configuração global** (pontuação, desempate, prazo) e os **usuários demo**.

No **primeiro start**, o app importa automaticamente os dados **reais** da Copa 2026 da fonte pública
**[openfootball/worldcup.json](https://github.com/openfootball/worldcup.json)** (domínio público,
**sem chave de API**):
- **48 seleções, 12 grupos (A–L) e os 72 jogos** da fase de grupos com **kickoff real (UTC)**;
- **elencos reais** (~1245 jogadores) para artilheiro / melhor jogador;
- jogos já disputados entram como **encerrados com o placar oficial**.

Sem internet, cai num **seed placeholder** (seleções genéricas, em `Infrastructure/DbInitializer.cs`)
só para o app subir offline.

**Reimportar a qualquer momento:** botão *"Importar dados reais"* na tela Admin, ou
`POST /api/admin/import-openfootball`. A fonte é configurável em `appsettings.json` → `OpenFootball:BaseUrl`.

> A importação **substitui** times/jogos e **zera** palpites/apostas — use antes do bolão começar.

### Acesso (cadastro fechado)

O bolão é **fechado**: só os participantes pré-cadastrados acessam (registro desabilitado por padrão).

- **Admin (acesso total):** `admin@bolao.com` / `Admin@2026`
- **Participantes:** os 10 e-mails cadastrados (em `Infrastructure/DbInitializer.cs`), todos com a
  **senha padrão `Bolao@2026`**. Entram como **não pagos** — o admin marca os pagamentos em
  *Admin → Pagamentos*.

> Para reabrir o cadastro, defina `"Auth": { "AllowRegistration": true }` em `appsettings.json`.

---

## 2) Frontend

```powershell
cd world-cup-front
npm install
npm start          # ng serve
```

- App: **http://localhost:4200** (o CORS do backend já libera essa origem).
- A URL da API fica em `src/environments/environment.ts`.

---

## Regras implementadas

### Pontuação (configurável em `Settings`)
- **Jogo:** placar exato **5**, acertou só o resultado **3**, errou/sem palpite **0**.
- **Classificação do grupo:** **3** por acerto de 1º + **3** por acerto de 2º.
- **Especiais:** campeão **25**, artilheiro **20**, melhor jogador **15**.
- **Total** = jogos + classificação + especiais. Desempate: mais placares exatos; depois, cadastro mais antigo.

### Travas (lock)
- **Palpite de jogo:** só pode criar/editar **antes do kickoff**. Depois, fica travado. Sem palpite = 0.
- **Classificação e especiais:** travam no prazo global (`Settings.LockBetsAtUtc`, padrão = início da Copa;
  ajustável pelo admin). O backend **recusa** qualquer alteração após o bloqueio.
- **Palpites dos outros** num jogo só ficam visíveis **depois do início** da partida.

### Admin
- Marcar/desmarcar **pagamento** (só pagos entram no ranking/prêmio).
- Lançar **resultado de cada jogo** → apura os pontos automaticamente.
- Lançar **1º/2º de cada grupo** e o **resultado final** (campeão/artilheiro/melhor jogador) → apura classificação e especiais.

---

## Endpoints principais

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/register`, `/api/auth/login` | público |
| GET | `/api/matches`, `/api/matches/{id}/predictions` | autenticado |
| GET / POST | `/api/predictions/me`, `/api/predictions` | autenticado |
| GET / POST | `/api/group-bets/me`, `/api/group-bets` | autenticado |
| GET / POST | `/api/special-bets/me`, `/api/special-bets` | autenticado |
| GET | `/api/ranking` | autenticado |
| GET | `/api/teams`, `/api/players`, `/api/settings` | autenticado |
| GET | `/api/users` | admin |
| PUT | `/api/users/{id}/payment` | admin |
| PUT | `/api/matches/{id}/result` | admin |
| PUT | `/api/groups/{grupo}/result` | admin |
| GET / PUT | `/api/tournament/result` | admin |
| PUT | `/api/settings` | admin |
| POST | `/api/admin/import-openfootball` | admin |

---

## Estrutura do backend

```
WorldCup/
├─ Domain/            # Entidades + enum Fase
├─ Infrastructure/    # AppDbContext, DbInitializer (seed), OpenFootballImporter, Migrations
├─ Auth/              # JwtSettings, TokenService, extensões de claims
├─ Services/          # ScoringService, LockService, RankingService
├─ Dtos/              # DTOs de request/response
├─ Controllers/       # Auth, Matches, Predictions, GroupBets, SpecialBets, Ranking, Reference, Admin
└─ Program.cs         # DI, JWT, CORS, OpenAPI, migrate+seed no startup
```

> Organizado em camadas dentro de um único projeto (Domain / Infrastructure / Services / Controllers),
> que é o "ou equivalente" pedido — simples de rodar localmente.

---

## Produção (notas de segurança)
- Defina `Jwt:Key` por **variável de ambiente** (não use a chave de exemplo do `appsettings.json`).
- Ajuste `ConnectionStrings:Default` e `Cors:AllowedOrigins` para o ambiente real.
