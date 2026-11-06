using System.Text;
using Aquenda.Application;
using Aquenda.Application.Comum;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Aquenda.Infrastructure.Mqtt;
using Aquenda.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aquenda.Tests.Infra;

public class MqttMensagemParserTests
{
    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Le_o_payload_do_contrato()
    {
        var m = MqttMensagemParser.Ler("aquenda/casa/3/vagas", Bytes("{\"total\":10,\"ocupadas\":7,\"ts\":\"2026-11-14T21:00:00Z\"}"));
        Assert.Equal(3, m.CasaId);
        Assert.Equal(10, m.Leitura.Total);
        Assert.Equal(7, m.Leitura.Ocupadas);
        Assert.Equal(new DateTime(2026, 11, 14, 21, 0, 0, DateTimeKind.Utc), m.Leitura.RegistradaEmUtc);
    }

    [Fact]
    public void Ts_e_opcional_e_fuso_e_convertido_para_utc()
    {
        Assert.Null(MqttMensagemParser.Ler("aquenda/casa/1/vagas", Bytes("{\"total\":4,\"ocupadas\":0}")).Leitura.RegistradaEmUtc);
        var comFuso = MqttMensagemParser.Ler("aquenda/casa/1/vagas", Bytes("{\"total\":4,\"ocupadas\":0,\"ts\":\"2026-11-14T18:00:00-03:00\"}"));
        Assert.Equal(new DateTime(2026, 11, 14, 21, 0, 0), comFuso.Leitura.RegistradaEmUtc);
    }

    [Theory]
    [InlineData("aquenda/casa/abc/vagas")]
    [InlineData("aquenda/casa/0/vagas")]
    [InlineData("aquenda/casa/1")]
    [InlineData("outro/casa/1/vagas")]
    [InlineData("aquenda/casa/1/vagas/extra")]
    public void Topico_fora_do_contrato_e_recusado(string topico) =>
        Assert.Throws<FormatException>(() => MqttMensagemParser.Ler(topico, Bytes("{\"total\":1,\"ocupadas\":0}")));

    [Theory]
    [InlineData("")]
    [InlineData("nao e json")]
    [InlineData("{\"total\":10}")]
    [InlineData("{\"total\":\"10\",\"ocupadas\":1}")]
    [InlineData("{\"total\":10,\"ocupadas\":1,\"ts\":\"ontem\"}")]
    public void Payload_invalido_e_recusado(string payload) =>
        Assert.Throws<FormatException>(() => MqttMensagemParser.Ler("aquenda/casa/1/vagas", Bytes(payload)));

    [Fact]
    public void Payload_grande_demais_e_recusado() =>
        Assert.Throws<FormatException>(() =>
            MqttMensagemParser.Ler("aquenda/casa/1/vagas", new byte[MqttMensagemParser.TamanhoMaximoPayload + 1]));
}

public class MqttOcupacaoListenerTests
{
    [Fact]
    public async Task Mensagem_valida_grava_leitura_e_invalida_e_ignorada_sem_derrubar_o_servico()
    {
        using var c = await Cenario.ComSeedAsync();
        var casa = await c.IdPorNomeAsync("Casa 1");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AquendaDbContext>(o => o.UseSqlite(c.Banco.Conexao));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AquendaDbContext>());
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<ICasaAcolhimentoRepository, CasaAcolhimentoRepository>();
        services.AddScoped<IRodaConversaRepository, RodaConversaRepository>();
        services.AddScoped<IInteracaoRepository, InteracaoRepository>();
        services.AddSingleton<IRelogio>(c.Relogio);
        services.AddAquendaApplication();
        await using var provider = services.BuildServiceProvider();

        var listener = new MqttOcupacaoListener(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MqttOptions()), NullLogger<MqttOcupacaoListener>.Instance);

        await listener.ProcessarAsync($"aquenda/casa/{casa}/vagas", Encoding.UTF8.GetBytes("{\"total\":8,\"ocupadas\":3}"), default);
        await listener.ProcessarAsync($"aquenda/casa/{casa}/vagas", Encoding.UTF8.GetBytes("{\"total\":8,\"ocupadas\":30}"), default);
        await listener.ProcessarAsync("aquenda/casa/9999/vagas", Encoding.UTF8.GetBytes("{\"total\":8,\"ocupadas\":3}"), default);
        await listener.ProcessarAsync("lixo", Encoding.UTF8.GetBytes("{}"), default);

        await using var db = c.Contexto();
        var vagas = await c.Ocupacao(db).ObterVagasAsync(casa);
        Assert.Equal(SituacaoVagas.Disponivel, vagas.Situacao);
        Assert.Equal(5, vagas.Livres);
        Assert.Equal(Aquenda.Domain.OrigemLeitura.Sensor, vagas.Origem);
    }
}
