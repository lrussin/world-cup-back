using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// ---------- CORS liberado ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ---------- Servicos da aplicacao ----------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IScoringService, ScoringService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IRankingService, RankingService>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<OpenFootballImporter>();
builder.Services.AddScoped<ILiveScoreService, LiveScoreService>();

builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

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
                result.Teams,
                result.Players,
                result.Matches,
                result.MatchesEncerrados);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Seed: falha ao importar do openfootball; usando dados placeholder.");
            DbInitializer.SeedPlaceholderFixtures(db);
        }
    }

    // Seeds de dados (palpites/resultados/apostas) NAO rodam mais no startup: os dados ja estao
    // no banco e re-rodar sobrescrevia ajustes (ex.: SeedJogos reescrevia placar). Os metodos
    // continuam em DbInitializer para re-seed manual de um banco novo, se necessario.

    // Knockout: cria os 32 jogos do mata-mata sem times (admin define). Idempotente por Num.
    DbInitializer.SeedKnockout(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // JSON OpenAPI em /openapi/v1.json
}

// ---------- Pipeline HTTP ----------

app.UseHttpsRedirection();

app.UseRouting();

// ---------- CORS ----------
app.UseCors("AllowAllCors");

// ---------- Preflight OPTIONS manual ----------
// Isso evita erro 405 Method Not Allowed no preflight do navegador.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With, Accept, Origin";
        context.Response.Headers["Access-Control-Max-Age"] = "86400";

        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// ---------- Health Checks ----------

// Liveness: a aplicacao esta de pe (nao checa dependencias).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: checa tambem a conexao com o banco.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";

        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
            tempoMs = report.TotalDuration.TotalMilliseconds
        });
    }
});

app.MapControllers();

app.Run();