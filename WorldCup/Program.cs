using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorldCup.Auth;
using WorldCup.Infrastructure;
using WorldCup.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---------- JWT ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantem os claims como emitidos (sem mapeamento legado de "sub" etc.).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS para o Angular ----------
const string CorsPolicy = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---------- Servicos da aplicacao ----------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<OpenFootballImporter>();
builder.Services.AddScoped<ILiveScoreService, LiveScoreService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---------- Migrations + seed no startup ----------
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Configuracao global + usuarios demo (sempre).
    DbInitializer.SeedBaseline(db);

    // Na primeira carga, importa os dados REAIS (openfootball). Sem internet, cai no placeholder.
    if (!db.Teams.Any())
    {
        try
        {
            var result = await sp.GetRequiredService<OpenFootballImporter>().ImportAsync();
            app.Logger.LogInformation(
                "Seed: dados reais importados do openfootball ({Teams} times, {Players} jogadores, {Matches} jogos, {Fechados} encerrados).",
                result.Teams, result.Players, result.Matches, result.MatchesEncerrados);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Seed: falha ao importar do openfootball; usando dados placeholder.");
            DbInitializer.SeedPlaceholderFixtures(db);
        }
    }

    // Importa as escolhas de artilheiro e melhor jogador do formulario (idempotente; nao sobrescreve).
    DbInitializer.SeedSpecialBetsArtilheiro(db);
    DbInitializer.SeedSpecialBetsMelhorJogador(db);
    DbInitializer.SeedSpecialBetsCampeao(db);
    DbInitializer.SeedJogos(db);
    DbInitializer.SeedGroupBets(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // JSON OpenAPI em /openapi/v1.json
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
