using Aquenda.Infrastructure.Dados;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aquenda.Api.Infra;

/// <summary>Usado pelo /health, que o monitoramento da VM consulta.</summary>
public sealed class BancoHealthCheck : IHealthCheck
{
    private readonly AquendaDbContext _db;

    public BancoHealthCheck(AquendaDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await _db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("SQLite acessível")
            : HealthCheckResult.Unhealthy("SQLite inacessível");
}
