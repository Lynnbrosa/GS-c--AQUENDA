using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aquenda.Application.Curadoria;
using Aquenda.Domain;
using Aquenda.Tests.Infra;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aquenda.Tests.Api;

public class ChatApiTests : IClassFixture<AquendaApiFactory>
{
    private readonly AquendaApiFactory _factory;
    private readonly HttpClient _http;

    public ChatApiTests(AquendaApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    private async Task<int> SalaAsync(string slug)
    {
        var salas = await _http.GetFromJsonAsync<JsonElement>("/api/chat/salas");
        return salas.EnumerateArray().Single(s => s.GetProperty("slug").GetString() == slug).GetProperty("id").GetInt32();
    }

    private async Task<string> EntrarAsync(string apelido)
    {
        var r = await _http.PostAsJsonAsync("/api/chat/participantes", new { apelido, aceitoRegras = true, tenhoMaisDe18 = true });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private HttpRequestMessage Mensagem(int sala, string texto, string? token)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/chat/salas/{sala}/mensagens") { Content = JsonContent.Create(new { texto }) };
        if (token is not null) req.Headers.Add("X-Token-Chat", token);
        return req;
    }

    [Fact]
    public async Task Fluxo_completo_entrar_enviar_ler()
    {
        var sala = await SalaAsync("primeiro-emprego");
        var token = await EntrarAsync("Lu_api");

        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.SendAsync(Mensagem(sala, "oi", null))).StatusCode);

        var enviada = await _http.SendAsync(Mensagem(sala, "Alguém tem dica de currículo com nome social?", token));
        Assert.Equal(HttpStatusCode.Created, enviada.StatusCode);

        var bloqueada = await _http.SendAsync(Mensagem(sala, "me chama no 11 98765-4321", token));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bloqueada.StatusCode);

        var msgs = await _http.GetFromJsonAsync<JsonElement>($"/api/chat/salas/{sala}/mensagens");
        var ultima = msgs.EnumerateArray().Last();
        Assert.Equal("Lu_api", ultima.GetProperty("apelido").GetString());
        Assert.False(ultima.TryGetProperty("participanteId", out _)); // público não vê o id de quem escreveu
    }

    [Fact]
    public async Task Entrada_sem_aceite_ou_menor_de_idade_e_recusada()
    {
        var r = await _http.PostAsJsonAsync("/api/chat/participantes", new { apelido = "Teen", aceitoRegras = true, tenhoMaisDe18 = false });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Moderacao_exige_chave_e_ve_a_fila()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _http.GetAsync("/api/chat/moderacao/fila")).StatusCode);

        var ong = _factory.CreateClient();
        ong.DefaultRequestHeaders.Add("X-Chave-Operador", AquendaApiFactory.Chave);
        Assert.Equal(HttpStatusCode.OK, (await ong.GetAsync("/api/chat/moderacao/fila")).StatusCode);
    }

    [Fact]
    public async Task Tempo_real_entrega_mensagem_para_quem_esta_na_sala()
    {
        var sala = await SalaAsync("recem-chegades");
        var token = await EntrarAsync("Rafa_rt");

        await using var conexao = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/chat"), o =>
            {
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        var recebida = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexao.On<JsonElement>("mensagem", m => recebida.TrySetResult(m));
        await conexao.StartAsync();
        await conexao.InvokeAsync("EntrarNaSala", sala);

        await _http.SendAsync(Mensagem(sala, "Cheguei ontem de Recife!", token));

        var m = await recebida.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Cheguei ontem de Recife!", m.GetProperty("texto").GetString());
        Assert.Equal("Rafa_rt", m.GetProperty("apelido").GetString());
    }
}

public class CuradoriaApiTests : IClassFixture<AquendaApiFactory>
{
    private readonly AquendaApiFactory _factory;

    public CuradoriaApiTests(AquendaApiFactory factory) => _factory = factory;

    private HttpClient Ong(Action<IServiceCollection>? trocar = null)
    {
        var f = trocar is null ? _factory : _factory.WithWebHostBuilder(b => b.ConfigureTestServices(trocar));
        var http = f.CreateClient();
        http.DefaultRequestHeaders.Add("X-Chave-Operador", AquendaApiFactory.Chave);
        return http;
    }

    [Fact]
    public async Task Cadastra_servico_e_ele_aparece_na_busca()
    {
        var geo = new GeocodificadorFalso();
        var http = Ong(s => s.AddScoped<IGeocodificador>(_ => geo));

        var r = await http.PostAsJsonAsync("/api/curadoria/servicos", new
        {
            categoria = "Juridico",
            nome = "Núcleo de Prática Jurídica Exemplo",
            bairro = "Centro",
            logradouro = "R. Exemplo, 1",
            cep = "01049-000",
            fonte = "https://exemplo.org",
            orgao = "Faculdade Exemplo",
            atendeRetificacao = true,
            horarios = new[] { new { dia = "Monday", abre = "09:00", fecha = "18:00" } }
        });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        Assert.NotNull(r.Headers.Location);
        var criado = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(criado.GetProperty("juridico").GetProperty("atendeRetificacao").GetBoolean());
        Assert.Equal(-23.5475, criado.GetProperty("resumo").GetProperty("endereco").GetProperty("latitude").GetDouble());
        Assert.Equal(new[] { "01049000" }, geo.Consultas);

        var busca = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/servicos?texto=Prática Jurídica");
        Assert.Equal(1, busca.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Curadoria_sem_chave_e_recusada() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/curadoria/revisao")).StatusCode);

    [Fact]
    public async Task Ingestao_osm_importa_pela_api()
    {
        var osm = new FonteOsmFalsa();
        osm.Lugares.Add(new LugarOsm("osm:node/999", "Bar Importado", TipoRole.Bar, "primary", -23.55, -46.65,
            null, null, "Consolação", null, null, false, "https://www.openstreetmap.org/node/999"));
        var http = Ong(s => s.AddScoped<IFonteOsm>(_ => osm));

        var r = await http.PostAsync("/api/curadoria/ingestao/osm", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1, (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("novos").GetInt32());
    }

    [Fact]
    public async Task Overpass_fora_do_ar_vira_502()
    {
        var http = Ong(s => s.AddScoped<IFonteOsm>(_ => new FonteQueFalha()));
        var r = await http.PostAsync("/api/curadoria/ingestao/osm", null);
        Assert.Equal(HttpStatusCode.BadGateway, r.StatusCode);
    }

    private sealed class FonteQueFalha : IFonteOsm
    {
        public Task<IReadOnlyList<LugarOsm>> BuscarLugaresLgbtqAsync(CancellationToken ct = default) =>
            throw new HttpRequestException("Overpass fora do ar");
    }
}
