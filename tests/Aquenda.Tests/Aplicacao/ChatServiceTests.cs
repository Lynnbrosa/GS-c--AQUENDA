using Aquenda.Application.Chat;
using Aquenda.Application.Comum;
using Aquenda.Domain.Comum;
using Aquenda.Infrastructure.Chat;
using Aquenda.Infrastructure.Dados;
using Aquenda.Domain.Chat;
using Aquenda.Infrastructure.Repositorios;
using Aquenda.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aquenda.Tests.Aplicacao;

public class ChatServiceTests
{
    private static async Task<string> EntrarAsync(Cenario c, string apelido)
    {
        await using var db = c.Contexto();
        return (await c.Chat(db).EntrarAsync(apelido, true, true)).Token;
    }

    private static async Task<int> EnviarAsync(Cenario c, string token, int sala, string texto)
    {
        await using var db = c.Contexto();
        return (await c.Chat(db).EnviarAsync(token, sala, texto)).Id;
    }

    [Fact]
    public async Task Seed_cria_as_quatro_salas_do_pitch()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        var salas = await c.Chat(db).ListarSalasAsync();
        Assert.Equal(new[] { "primeiro-emprego", "transicao", "familia", "recem-chegades" }, salas.Select(s => s.Sala.Slug));
    }

    [Fact]
    public async Task Apelido_e_unico_sem_diferenciar_maiusculas()
    {
        using var c = await Cenario.ComSeedAsync();
        await EntrarAsync(c, "Nina");
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() => c.Chat(db).EntrarAsync("nINA", true, true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("token-inventado")]
    public async Task Sem_token_valido_nao_envia(string? token)
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<NaoAutorizadoException>(() => c.Chat(db).EnviarAsync(token, sala, "oi"));
    }

    [Fact]
    public async Task Mensagem_enviada_aparece_na_sala_e_avisa_o_tempo_real()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var token = await EntrarAsync(c, "Nina");

        var id = await EnviarAsync(c, token, sala, "Contei pra minha mãe hoje");

        await using var db = c.Contexto();
        var msgs = await c.Chat(db).ListarMensagensAsync(sala, null, 50);
        var m = Assert.Single(msgs);
        Assert.Equal("Nina", m.Participante.Apelido);
        Assert.Equal(new[] { id }, c.Notificador.Novas);
        Assert.Equal(1, (await c.Chat(db).ListarSalasAsync()).Single(s => s.Sala.Id == sala).MensagensUltimas24h);
    }

    [Fact]
    public async Task Paginacao_volta_em_ordem_de_leitura()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("transicao");
        var token = await EntrarAsync(c, "Nina");
        var ids = new List<int>();
        for (var i = 1; i <= 5; i++)
        {
            ids.Add(await EnviarAsync(c, token, sala, $"msg {i}"));
            c.Relogio.Avancar(TimeSpan.FromMinutes(1));
        }

        await using var db = c.Contexto();
        var ultimas = await c.Chat(db).ListarMensagensAsync(sala, null, 2);
        Assert.Equal(ids.TakeLast(2), ultimas.Select(m => m.Id));
        var anteriores = await c.Chat(db).ListarMensagensAsync(sala, ultimas[0].Id, 2);
        Assert.Equal(ids.Skip(1).Take(2), anteriores.Select(m => m.Id));
    }

    [Fact]
    public async Task Limita_mensagens_por_minuto()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var token = await EntrarAsync(c, "Nina");
        for (var i = 0; i < ChatService.MensagensPorMinuto; i++)
            await EnviarAsync(c, token, sala, $"msg {i}");

        await using (var db = c.Contexto())
            await Assert.ThrowsAsync<DomainException>(() => c.Chat(db).EnviarAsync(token, sala, "mais uma"));

        c.Relogio.Avancar(TimeSpan.FromMinutes(1));
        await EnviarAsync(c, token, sala, "agora pode");
    }

    [Fact]
    public async Task Tres_denuncias_tiram_da_sala_e_moderacao_decide()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var autora = await EntrarAsync(c, "Autora");
        var id = await EnviarAsync(c, autora, sala, "mensagem ofensiva");

        foreach (var apelido in new[] { "Bia", "Cris", "Duda" })
        {
            var token = await EntrarAsync(c, apelido);
            await using var db = c.Contexto();
            await c.Chat(db).DenunciarAsync(token, id);
        }

        await using (var db = c.Contexto())
        {
            Assert.Empty(await c.Chat(db).ListarMensagensAsync(sala, null, 50));
            Assert.Equal(new[] { id }, c.Notificador.Removidas);
            var fila = await c.Chat(db).FilaModeracaoAsync();
            Assert.Equal(3, Assert.Single(fila).Denuncias.Count);
        }

        await using (var db = c.Contexto())
            await c.Chat(db).RestaurarAsync(id);

        await using var leitura = c.Contexto();
        Assert.Single(await c.Chat(leitura).ListarMensagensAsync(sala, null, 50));
        Assert.Empty(await c.Chat(leitura).FilaModeracaoAsync());
        Assert.Equal(new[] { id }, c.Notificador.Restauradas);
    }

    [Fact]
    public async Task Banir_some_com_as_mensagens_e_bloqueia_envio()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var token = await EntrarAsync(c, "Troll");
        await EnviarAsync(c, token, sala, "spam");

        await using (var db = c.Contexto())
        {
            var participante = db.ParticipantesChat.Single(p => p.Apelido == "Troll");
            await c.Chat(db).BanirAsync(participante.Id);
        }

        await using var leitura = c.Contexto();
        Assert.Empty(await c.Chat(leitura).ListarMensagensAsync(sala, null, 50));
        await Assert.ThrowsAsync<DomainException>(() => c.Chat(leitura).EnviarAsync(token, sala, "voltei"));
    }

    [Fact]
    public async Task Sala_fechada_pela_ong_nao_recebe_mensagem()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var token = await EntrarAsync(c, "Nina");
        await using (var db = c.Contexto())
            await c.Chat(db).DefinirSalaAbertaAsync(sala, false);

        await using var db2 = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() => c.Chat(db2).EnviarAsync(token, sala, "oi"));
    }

    [Fact]
    public async Task Limpeza_apaga_mensagens_e_denuncias_antigas()
    {
        using var c = await Cenario.ComSeedAsync();
        var sala = await c.SalaAsync("familia");
        var autora = await EntrarAsync(c, "Autora");
        var velha = await EnviarAsync(c, autora, sala, "mensagem velha");
        var outra = await EntrarAsync(c, "Bia");
        await using (var db = c.Contexto())
            await c.Chat(db).DenunciarAsync(outra, velha);

        c.Relogio.Avancar(TimeSpan.FromDays(8));
        await EnviarAsync(c, autora, sala, "mensagem nova");

        var services = new ServiceCollection();
        services.AddDbContext<AquendaDbContext>(o => o.UseSqlite(c.Banco.Conexao));
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddSingleton<IRelogio>(c.Relogio);
        await using var provider = services.BuildServiceProvider();
        var limpeza = new LimpezaChatService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ChatOptions { DiasRetencao = 7 }), NullLogger<LimpezaChatService>.Instance);

        Assert.Equal(1, await limpeza.LimparAsync(default));

        await using var leitura = c.Contexto();
        Assert.Equal("mensagem nova", Assert.Single(leitura.MensagensChat).Texto);
        Assert.Empty(leitura.DenunciasMensagem);
    }
}
