using System.Net;
using System.Text;
using System.Text.Json;
using Aquenda.Domain;
using Aquenda.Infrastructure.Externos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aquenda.Tests.Infra;

/// <summary>HttpMessageHandler que devolve uma resposta gravada e guarda a requisição.</summary>
public sealed class HandlerGravado : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _corpo;
    public HttpRequestMessage? Requisicao { get; private set; }
    public string? CorpoEnviado { get; private set; }

    public HandlerGravado(HttpStatusCode status, string corpo)
    {
        _status = status;
        _corpo = corpo;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requisicao = request;
        CorpoEnviado = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(_status) { Content = new StringContent(_corpo, Encoding.UTF8, "application/json") };
    }
}

public class OverpassFonteOsmTests
{
    // Formato real da Overpass: nó com lat/lon e "way" com center.
    private const string RespostaGravada = """
    {"elements":[
      {"type":"node","id":111,"lat":-23.5527,"lon":-46.6550,
       "tags":{"name":"Bar Exemplo","amenity":"bar","lgbtq":"primary","addr:street":"Rua Frei Caneca","addr:housenumber":"384","addr:suburb":"Consolação","wheelchair":"yes"}},
      {"type":"way","id":222,"center":{"lat":-23.5440,"lon":-46.6430},
       "tags":{"name":"Clube Exemplo","amenity":"nightclub","lgbtq":"welcome","website":"https://clube.example"}},
      {"type":"relation","id":333,"tags":{"name":"Sem coordenada","lgbtq":"only"}}
    ]}
    """;

    private static OverpassFonteOsm Fonte(HandlerGravado h) =>
        new(new HttpClient(h), Options.Create(new FontesExternasOptions()));

    [Fact]
    public async Task Converte_nos_e_vias_e_descarta_sem_coordenada()
    {
        var h = new HandlerGravado(HttpStatusCode.OK, RespostaGravada);
        var lugares = await Fonte(h).BuscarLugaresLgbtqAsync();

        Assert.Equal(2, lugares.Count);
        var bar = lugares[0];
        Assert.Equal(("osm:node/111", "Bar Exemplo", TipoRole.Bar, "primary"), (bar.IdExterno, bar.Nome, bar.Tipo, bar.TagLgbtq));
        Assert.Equal(("Rua Frei Caneca", "384", "Consolação", true), (bar.Rua, bar.Numero, bar.Bairro, bar.Acessivel));
        Assert.Equal("https://www.openstreetmap.org/node/111", bar.UrlFonte);

        var clube = lugares[1];
        Assert.Equal((TipoRole.Balada, -23.5440, "https://clube.example"), (clube.Tipo, clube.Latitude, clube.Site));
    }

    [Fact]
    public async Task Envia_a_consulta_da_tag_lgbtq_por_post()
    {
        var h = new HandlerGravado(HttpStatusCode.OK, """{"elements":[]}""");
        await Fonte(h).BuscarLugaresLgbtqAsync();

        Assert.Equal(HttpMethod.Post, h.Requisicao!.Method);
        var consulta = Uri.UnescapeDataString(h.CorpoEnviado!.Replace('+', ' '));
        Assert.Contains("lgbtq", consulta);
        Assert.Contains("São Paulo", consulta);
    }

    [Fact]
    public async Task Erro_da_overpass_sobe_como_HttpRequestException()
    {
        var h = new HandlerGravado(HttpStatusCode.TooManyRequests, "");
        await Assert.ThrowsAsync<HttpRequestException>(() => Fonte(h).BuscarLugaresLgbtqAsync());
    }

    [Theory]
    [InlineData("theatre", TipoRole.Cultural)]
    [InlineData("pub", TipoRole.Bar)]
    [InlineData(null, TipoRole.EspacoPublico)]
    public void Tipo_vem_das_tags(string? amenity, TipoRole esperado)
    {
        var tags = new Dictionary<string, string> { ["leisure"] = "park" };
        if (amenity is not null) tags["amenity"] = amenity;
        Assert.Equal(esperado, OverpassFonteOsm.TipoPorTags(tags));
    }
}

public class BrasilApiGeocodificadorTests
{
    // Resposta real da BrasilAPI para 01049-000 (coordenadas vêm como texto).
    private const string Cep01049000 = """
    {"cep":"01049000","state":"SP","city":"São Paulo","neighborhood":"Centro","street":"Rua Formosa","service":"open-cep",
     "location":{"type":"Point","coordinates":{"longitude":"-46.63611","latitude":"-23.5475"}}}
    """;

    private static BrasilApiGeocodificador Geo(HandlerGravado h) =>
        new(new HttpClient(h), Options.Create(new FontesExternasOptions()), NullLogger<BrasilApiGeocodificador>.Instance);

    [Fact]
    public async Task Le_a_coordenada_do_cep()
    {
        var h = new HandlerGravado(HttpStatusCode.OK, Cep01049000);
        var c = await Geo(h).CoordenadaDoCepAsync("01049000");
        Assert.Equal((-23.5475, -46.63611), (c!.Value.Latitude, c.Value.Longitude));
        Assert.Equal("https://brasilapi.com.br/api/cep/v2/01049000", h.Requisicao!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "{}")]
    [InlineData(HttpStatusCode.InternalServerError, "erro")]
    [InlineData(HttpStatusCode.OK, """{"location":{"type":"Point","coordinates":{}}}""")]
    [InlineData(HttpStatusCode.OK, "não é json")]
    public async Task Sem_coordenada_ou_com_falha_devolve_null(HttpStatusCode status, string corpo) =>
        Assert.Null(await Geo(new HandlerGravado(status, corpo)).CoordenadaDoCepAsync("01049000"));

    [Fact]
    public void Coordenada_numerica_tambem_e_aceita()
    {
        using var json = JsonDocument.Parse("""{"location":{"coordinates":{"latitude":-23.5,"longitude":-46.6}}}""");
        Assert.Equal(-23.5, BrasilApiGeocodificador.LerCoordenada(json.RootElement)!.Value.Latitude);
    }
}
