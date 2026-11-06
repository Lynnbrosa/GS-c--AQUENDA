using Aquenda.Application.Catalogo;
using Aquenda.Application.Chat;
using Aquenda.Application.Curadoria;
using Aquenda.Application.Metricas;
using Aquenda.Application.Ocupacao;
using Aquenda.Application.Rodas;
using Microsoft.Extensions.DependencyInjection;

namespace Aquenda.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAquendaApplication(this IServiceCollection services)
    {
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IOcupacaoService, OcupacaoService>();
        services.AddScoped<IRodaService, RodaService>();
        services.AddScoped<IMetricaService, MetricaService>();
        services.AddScoped<ICuradoriaService, CuradoriaService>();
        services.AddScoped<IIngestaoOsmService, IngestaoOsmService>();
        services.AddScoped<IChatService>(sp => new ChatService(
            sp.GetRequiredService<Aquenda.Domain.Chat.IChatRepository>(),
            sp.GetRequiredService<Aquenda.Domain.Repositorios.IUnitOfWork>(),
            sp.GetRequiredService<Aquenda.Application.Comum.IRelogio>(),
            sp.GetRequiredService<INotificadorChat>()));
        return services;
    }
}
