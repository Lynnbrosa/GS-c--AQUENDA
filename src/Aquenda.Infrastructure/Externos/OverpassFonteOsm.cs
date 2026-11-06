using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aquenda.Application.Curadoria;
using Aquenda.Domain;
using Microsoft.Extensions.Options;

namespace Aquenda.Infrastructure.Externos;

/// <summary>
/// Consulta a Overpass API (OpenStreetMap) atrás de lugares com a tag lgbtq em São Paulo.
/// Não precisa de chave.
/// </summary>
public sealed class OverpassFonteOsm : IFonteOsm
{
    private readonly HttpClient _http;
    private readonly FontesExternasOptions _opcoes;

    public OverpassFonteOsm(HttpClient http, IOptions<FontesExternasOptions> opcoes)
    {
        _http = http;
        _opcoes = opcoes.Value;
    }

    public string Consulta =>
        $"[out:json][timeout:25];" +
        $"area[\"name\"=\"{_opcoes.AreaOsm}\"][\"boundary\"=\"administrative\"][\"admin_level\"=\"8\"]->.sp;" +
        "nwr[\"lgbtq\"~\"^(welcome|primary|only)$\"](area.sp);" +
        "out center tags;";

    public async Task<IReadOnlyList<LugarOsm>> BuscarLugaresLgbtqAsync(CancellationToken ct = default)
    {
        using var corpo = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", Consulta) });
        using var resposta = await _http.PostAsync(_opcoes.OverpassUrl, corpo, ct);
        resposta.EnsureSuccessStatusCode();

        var dados = await resposta.Content.ReadFromJsonAsync<RespostaOverpass>(cancellationToken: ct)
            ?? throw new HttpRequestException("Resposta vazia da Overpass.");
        return Converter(dados);
    }

    public static IReadOnlyList<LugarOsm> Converter(RespostaOverpass dados) =>
        dados.Elements
            .Select(e => (Elemento: e, Lat: e.Lat ?? e.Center?.Lat, Lon: e.Lon ?? e.Center?.Lon))
            .Where(x => x.Lat.HasValue && x.Lon.HasValue)
            .Select(x =>
            {
                var t = x.Elemento.Tags ?? new Dictionary<string, string>();
                return new LugarOsm(
                    $"osm:{x.Elemento.Type}/{x.Elemento.Id}",
                    t.GetValueOrDefault("name"),
                    TipoPorTags(t),
                    t.GetValueOrDefault("lgbtq"),
                    x.Lat!.Value,
                    x.Lon!.Value,
                    t.GetValueOrDefault("addr:street"),
                    t.GetValueOrDefault("addr:housenumber"),
                    t.GetValueOrDefault("addr:suburb") ?? t.GetValueOrDefault("addr:neighbourhood"),
                    t.GetValueOrDefault("website") ?? t.GetValueOrDefault("contact:website"),
                    t.GetValueOrDefault("phone") ?? t.GetValueOrDefault("contact:phone"),
                    t.GetValueOrDefault("wheelchair") == "yes",
                    $"https://www.openstreetmap.org/{x.Elemento.Type}/{x.Elemento.Id.ToString(CultureInfo.InvariantCulture)}");
            })
            .ToList();

    public static TipoRole TipoPorTags(IReadOnlyDictionary<string, string> tags) =>
        tags.GetValueOrDefault("amenity") switch
        {
            "nightclub" => TipoRole.Balada,
            "bar" or "pub" or "biergarten" => TipoRole.Bar,
            "arts_centre" or "theatre" or "community_centre" or "cinema" or "library" => TipoRole.Cultural,
            _ when tags.ContainsKey("leisure") || tags.ContainsKey("place") => TipoRole.EspacoPublico,
            _ => TipoRole.Bar
        };

    public sealed record RespostaOverpass([property: JsonPropertyName("elements")] IReadOnlyList<ElementoOsm> Elements);

    public sealed record ElementoOsm(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("lat")] double? Lat,
        [property: JsonPropertyName("lon")] double? Lon,
        [property: JsonPropertyName("center")] CentroOsm? Center,
        [property: JsonPropertyName("tags")] Dictionary<string, string>? Tags);

    public sealed record CentroOsm(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lon")] double Lon);
}
