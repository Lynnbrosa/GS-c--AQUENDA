using Aquenda.Application.Comum;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Aquenda.Infrastructure.Dados.Seed;
using Aquenda.Infrastructure.Mqtt;
using Aquenda.Infrastructure.Repositorios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aquenda.Infrastructure;

public static class DependencyInjection
{
    public const string ConexaoPadrao = "Data Source=aquenda.db";

    public static IServiceCollection AddAquendaInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conexao = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(conexao)) conexao = ConexaoPadrao;
        CriarPastaDoBanco(conexao);

        services.AddDbContext<AquendaDbContext>(o => o.UseSqlite(conexao));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AquendaDbContext>());
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<ICasaAcolhimentoRepository, CasaAcolhimentoRepository>();
        services.AddScoped<IRodaConversaRepository, RodaConversaRepository>();
        services.AddScoped<IInteracaoRepository, InteracaoRepository>();
        services.AddSingleton<IRelogio, RelogioSistema>();

        services.Configure<MqttOptions>(config.GetSection(MqttOptions.Secao));
        services.AddSingleton<MqttOcupacaoListener>();
        services.AddHostedService(sp => sp.GetRequiredService<MqttOcupacaoListener>());

        return services;
    }

    /// <summary>Aplica as migrações pendentes e semeia o catálogo se estiver vazio.</summary>
    public static async Task InicializarBancoAsync(this IServiceProvider provider, bool semear = true, CancellationToken ct = default)
    {
        await using var escopo = provider.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<AquendaDbContext>();
        var log = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Aquenda.Banco");

        await db.Database.MigrateAsync(ct);
        log.LogInformation("Migrações aplicadas em {Banco}", db.Database.GetDbConnection().DataSource);

        if (semear)
        {
            var relogio = escopo.ServiceProvider.GetRequiredService<IRelogio>();
            var inseridos = await CatalogoSeed.SemearAsync(db, relogio, ct);
            if (inseridos > 0) log.LogInformation("Seed: {Quantidade} serviços inseridos", inseridos);
        }
    }

    /// <summary>O SQLite não cria pastas. Na VM o banco fica em /var/lib/aquenda/aquenda.db.</summary>
    private static void CriarPastaDoBanco(string conexao)
    {
        var arquivo = new SqliteConnectionStringBuilder(conexao).DataSource;
        if (string.IsNullOrWhiteSpace(arquivo) || arquivo == ":memory:") return;
        var pasta = Path.GetDirectoryName(Path.GetFullPath(arquivo));
        if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
    }
}
