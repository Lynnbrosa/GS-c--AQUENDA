using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aquenda.Tests.Api;

/// <summary>Sobe a API inteira em memória, com um SQLite temporário e o MQTT desligado.</summary>
public sealed class AquendaApiFactory : WebApplicationFactory<Program>
{
    public const string Chave = "chave-de-teste";
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "aquenda-testes", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(_pasta, "aquenda.db")}");
        builder.UseSetting("Mqtt:Habilitado", "false");
        builder.UseSetting("Seguranca:ChaveOperador", Chave);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_pasta, recursive: true); } catch (IOException) { }
    }
}

public class ApiTests : IClassFixture<AquendaApiFactory>
{
    private readonly AquendaApiFactory _factory;
    private readonly HttpClient _http;

    public ApiTests(AquendaApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private async Task<int> IdAsync(string nome)
    {
        var json = await _http.GetFromJsonAsync<JsonElement>($"/api/servicos?texto={Uri.EscapeDataString(nome)}");
        return json.GetProperty("itens")[0].GetProperty("id").GetInt32();
    }

    private HttpClient ComChave()
    {
        var http = _factory.CreateClient();
        http.DefaultRequestHeaders.Add("X-Chave-Operador", AquendaApiFactory.Chave);
        return http;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/api/servicos")]
    [InlineData("/api/servicos/resumo")]
    [InlineData("/api/casas")]
    [InlineData("/api/rodas")]
    [InlineData("/api/metricas/impacto")]
    [InlineData("/api/chat/salas")]
    [InlineData("/api/chat/regras")]
    public async Task Rotas_publicas_respondem_200(string rota) =>
        Assert.Equal(HttpStatusCode.OK, (await _http.GetAsync(rota)).StatusCode);

    [Fact]
    public async Task Swagger_documenta_o_contrato_de_vagas()
    {
        var json = await _http.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/casas/{id}/vagas", json);
        Assert.Contains("X-Chave-Operador", json);
    }

    [Fact]
    public async Task Busca_aceita_enum_por_nome_e_devolve_json_em_camelCase()
    {
        var json = await _http.GetFromJsonAsync<JsonElement>("/api/servicos?categoria=Saude&atendimento=Hormonizacao");
        Assert.Equal(2, json.GetProperty("total").GetInt32());
        var item = json.GetProperty("itens")[0];
        Assert.Equal("Saude", item.GetProperty("categoria").GetString());
        Assert.False(string.IsNullOrEmpty(item.GetProperty("comoAcessar").GetString()));
    }

    [Fact]
    public async Task Endereco_sigiloso_nunca_sai_com_logradouro()
    {
        var id = await IdAsync("Casa Florescer");
        var json = await _http.GetFromJsonAsync<JsonElement>($"/api/servicos/{id}");
        var endereco = json.GetProperty("resumo").GetProperty("endereco");
        Assert.True(endereco.GetProperty("sigiloso").GetBoolean());
        Assert.Equal(JsonValueKind.Null, endereco.GetProperty("logradouro").ValueKind);
        Assert.Equal("Mulheres trans e travestis", json.GetProperty("acolhimento").GetProperty("publicoAlvo").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("saude").ValueKind);
    }

    [Fact]
    public async Task Erros_viram_ProblemDetails_com_o_status_certo()
    {
        var naoExiste = await _http.GetAsync("/api/servicos/99999");
        Assert.Equal(HttpStatusCode.NotFound, naoExiste.StatusCode);
        Assert.Equal("application/problem+json", naoExiste.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.BadRequest, (await _http.GetAsync("/api/servicos?raioKm=3")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _http.GetAsync("/api/servicos?categoria=Nada")).StatusCode);

        var centro = await IdAsync("Brunna Valin");
        Assert.Equal(HttpStatusCode.NotFound, (await _http.GetAsync($"/api/casas/{centro}/vagas")).StatusCode);
    }

    [Fact]
    public async Task Escrita_de_vagas_exige_chave_valida()
    {
        var casa = await IdAsync("Casa 1");
        var corpo = new { total = 10, ocupadas = 4 };

        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.PostAsJsonAsync($"/api/casas/{casa}/vagas", corpo)).StatusCode);

        var errada = _factory.CreateClient();
        errada.DefaultRequestHeaders.Add("X-Chave-Operador", "chute");
        Assert.Equal(HttpStatusCode.Unauthorized, (await errada.PostAsJsonAsync($"/api/casas/{casa}/vagas", corpo)).StatusCode);

        var ok = await ComChave().PostAsJsonAsync($"/api/casas/{casa}/vagas", corpo);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var vagas = await _http.GetFromJsonAsync<JsonElement>($"/api/casas/{casa}/vagas");
        Assert.Equal(6, vagas.GetProperty("livres").GetInt32());
        Assert.Equal("Manual", vagas.GetProperty("origem").GetString());
        Assert.Equal("6 vagas agora", vagas.GetProperty("mensagem").GetString());
    }

    [Fact]
    public async Task Leitura_invalida_volta_400_ou_422()
    {
        var casa = await IdAsync("Casa 1");
        var http = ComChave();
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync($"/api/casas/{casa}/vagas", new { ocupadas = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await http.PostAsJsonAsync($"/api/casas/{casa}/vagas", new { total = 3, ocupadas = 4 })).StatusCode);
    }

    [Fact]
    public async Task Cria_roda_e_devolve_201_com_Location()
    {
        var centro = await IdAsync("Brunna Valin");
        var resposta = await ComChave().PostAsJsonAsync("/api/rodas", new
        {
            titulo = "Roda: recém-chegades a SP",
            organizador = "ONG Parceira",
            inicio = DateTimeOffset.UtcNow.AddDays(5),
            duracaoMinutos = 90,
            presencial = true,
            localId = centro,
            idadeMinima = 16
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        var criada = await _http.GetFromJsonAsync<JsonElement>(resposta.Headers.Location);
        Assert.Equal("Roda: recém-chegades a SP", criada.GetProperty("titulo").GetString());
        Assert.False(criada.GetProperty("exemplo").GetBoolean());
    }

    [Fact]
    public async Task Interacao_anonima_conta_no_indicador()
    {
        var ubs = await IdAsync("UBS República");
        var antes = await _http.GetFromJsonAsync<JsonElement>("/api/metricas/impacto");

        var r = await _http.PostAsJsonAsync("/api/metricas/interacoes", new { tipo = "ComoChegar", servicoId = ubs });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        var depois = await _http.GetFromJsonAsync<JsonElement>("/api/metricas/impacto");
        Assert.Equal(antes.GetProperty("comoChegar").GetInt32() + 1, depois.GetProperty("comoChegar").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _http.PostAsJsonAsync("/api/metricas/interacoes", new { tipo = "Hackear", servicoId = ubs })).StatusCode);
    }
}
