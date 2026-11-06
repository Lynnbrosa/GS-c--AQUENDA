namespace Aquenda.Infrastructure.Externos;

/// <summary>Seção "FontesExternas" do appsettings.</summary>
public sealed class FontesExternasOptions
{
    public const string Secao = "FontesExternas";

    public string OverpassUrl { get; set; } = "https://overpass-api.de/api/interpreter";
    public string BrasilApiUrl { get; set; } = "https://brasilapi.com.br/api/";
    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>Área do OSM onde procurar: o município de São Paulo.</summary>
    public string AreaOsm { get; set; } = "São Paulo";
}
