using Aquenda.Application.Catalogo;
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
        return services;
    }
}
