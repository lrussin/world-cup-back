using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorldCup.Infrastructure;

/// <summary>Health check que verifica se o banco esta acessivel.</summary>
public class DbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DbHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Banco acessivel.")
                : HealthCheckResult.Unhealthy("Banco inacessivel.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao acessar o banco.", ex);
        }
    }
}
