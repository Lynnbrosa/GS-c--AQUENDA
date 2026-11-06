using System.Globalization;
using System.Net;
using System.Text.Json;
using Aquenda.Application.Curadoria;
using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aquenda.Infrastructure.Externos;

/// <summary>
/// CEP → coordenada pela BrasilAPI (v2). Se a API falhar, o cadastro segue sem coordenada:
/// a curadoria completa depois. O serviço só fica fora da busca por distância.
/// </summary>
public sealed class BrasilApiGeocodificador : IGeocodificador
{
    private readonly HttpClient _http;
    private readonly FontesExternasOptions _opcoes;
    private readonly ILogger<BrasilApiGeocodificador> _log;

    public BrasilApiGeocodificador(HttpClient http, IOptions<FontesExternasOptions> opcoes, ILogger<BrasilApiGeocodificador> log)
    {
        _http = http;
        _opcoes = opcoes.Value;
        _log = log;
    }

    public async Task<Coordenada?> CoordenadaDoCepAsync(string cep, CancellationToken ct = default)
    {
        try
        {
            using var resposta = await _http.GetAsync(new Uri(new Uri(_opcoes.BrasilApiUrl), $"cep/v2/{cep}"), ct);
            if (resposta.StatusCode == HttpStatusCode.NotFound) return null;
            resposta.EnsureSuccessStatusCode();

            await using var fluxo = await resposta.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(fluxo, cancellationToken: ct);
            return LerCoordenada(json.RootElement);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (ct.IsCancellationRequested) throw;
            _log.LogWarning("BrasilAPI indisponível para o CEP {Cep}: {Motivo}", cep, e.Message);
            return null;
        }
    }

    /// <summary>A BrasilAPI manda as coordenadas como texto e às vezes vazias.</summary>
    public static Coordenada? LerCoordenada(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("location", out var local) ||
            !local.TryGetProperty("coordinates", out var coords) ||
            coords.ValueKind != JsonValueKind.Object)
            return null;

        if (!Ler(coords, "latitude", out var lat) || !Ler(coords, "longitude", out var lon))
            return null;

        try { return new Coordenada(lat, lon); }
        catch (DomainException) { return null; }
    }

    private static bool Ler(JsonElement coords, string campo, out double valor)
    {
        valor = 0;
        if (!coords.TryGetProperty(campo, out var p)) return false;
        return p.ValueKind switch
        {
            JsonValueKind.Number => p.TryGetDouble(out valor),
            JsonValueKind.String => double.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out valor),
            _ => false
        };
    }
}
