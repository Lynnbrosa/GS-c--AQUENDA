using Aquenda.Application.Comum;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Tests.Infra;

namespace Aquenda.Tests.Aplicacao;

public class OcupacaoServiceTests
{
    [Fact]
    public async Task Leitura_registrada_aparece_em_vagas_agora()
    {
        using var c = await Cenario.ComSeedAsync();
        var casa1 = await c.IdPorNomeAsync("Casa 1");

        await using (var db = c.Contexto())
        {
            var antes = await c.Ocupacao(db).ObterVagasAsync(casa1);
            Assert.Equal(SituacaoVagas.SemLeitura, antes.Situacao);
            await c.Ocupacao(db).RegistrarAsync(casa1, new NovaLeitura(12, 9, null), OrigemLeitura.Sensor);
        }

        await using var leitura = c.Contexto();
        var vagas = await c.Ocupacao(leitura).ObterVagasAsync(casa1);
        Assert.Equal(SituacaoVagas.Disponivel, vagas.Situacao);
        Assert.Equal(3, vagas.Livres);
        Assert.Equal(OrigemLeitura.Sensor, vagas.Origem);
        Assert.Equal(Cenario.Agora, vagas.AtualizadoEmUtc);
    }

    [Fact]
    public async Task Leitura_atrasada_nao_substitui_a_mais_nova()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa 1");
        await using var db = c.Contexto();
        var servico = c.Ocupacao(db);

        await servico.RegistrarAsync(id, new NovaLeitura(10, 10, Cenario.Agora), OrigemLeitura.Sensor);
        var depois = await servico.RegistrarAsync(id, new NovaLeitura(10, 2, Cenario.Agora.AddMinutes(-20)), OrigemLeitura.Sensor);

        Assert.Equal(SituacaoVagas.Lotada, depois.Situacao);
        Assert.Equal(0, depois.Livres);
    }

    [Fact]
    public async Task Sem_leitura_nova_por_duas_horas_vira_desatualizada()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa Florescer");
        c.Relogio.Avancar(TimeSpan.FromHours(3));

        await using var db = c.Contexto();
        Assert.Equal(SituacaoVagas.Desatualizada, (await c.Ocupacao(db).ObterVagasAsync(id)).Situacao);
    }

    [Fact]
    public async Task Servico_que_nao_e_casa_nao_tem_vagas()
    {
        using var c = await Cenario.ComSeedAsync();
        var centro = await c.IdPorNomeAsync("Centro de Referência LGBTI+ Brunna Valin");
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => c.Ocupacao(db).ObterVagasAsync(centro));
    }

    [Fact]
    public async Task Leitura_invalida_nao_e_salva()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa 1");
        await using (var db = c.Contexto())
            await Assert.ThrowsAsync<DomainException>(() =>
                c.Ocupacao(db).RegistrarAsync(id, new NovaLeitura(5, 6, null), OrigemLeitura.Sensor));

        await using var leitura = c.Contexto();
        Assert.Equal(SituacaoVagas.SemLeitura, (await c.Ocupacao(leitura).ObterVagasAsync(id)).Situacao);
    }

    [Fact]
    public async Task Painel_mostra_casas_com_vaga_primeiro()
    {
        using var c = await Cenario.ComSeedAsync();
        var casa1 = await c.IdPorNomeAsync("Casa 1");
        await using var db = c.Contexto();
        var servico = c.Ocupacao(db);

        var painel = await servico.PainelAsync();
        Assert.Equal(2, painel.Count);
        Assert.Equal("Casa Florescer", painel[0].Casa);            // tem leitura simulada no seed
        Assert.Equal(SituacaoVagas.SemLeitura, painel[1].Situacao);

        await servico.RegistrarAsync(casa1, new NovaLeitura(10, 10, null), OrigemLeitura.Manual);
        var soComVaga = await servico.PainelAsync(somenteComVaga: true);
        Assert.Equal("Casa Florescer", Assert.Single(soComVaga).Casa);
    }

    [Fact]
    public async Task Historico_calcula_media_pico_e_lotacao()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa 1");
        await using var db = c.Contexto();
        var servico = c.Ocupacao(db);

        await servico.RegistrarAsync(id, new NovaLeitura(10, 5, Cenario.Agora.AddHours(-30)), OrigemLeitura.Sensor); // fora de 24h
        await servico.RegistrarAsync(id, new NovaLeitura(10, 6, Cenario.Agora.AddHours(-3)), OrigemLeitura.Sensor);
        await servico.RegistrarAsync(id, new NovaLeitura(10, 10, Cenario.Agora.AddHours(-2)), OrigemLeitura.Sensor);
        await servico.RegistrarAsync(id, new NovaLeitura(10, 8, Cenario.Agora.AddHours(-1)), OrigemLeitura.Sensor);

        var h = await servico.HistoricoAsync(id, 24);

        Assert.Equal(3, h.Leituras.Count);
        Assert.Equal(0.8, h.OcupacaoMedia);
        Assert.Equal(10, h.PicoOcupadas);
        Assert.Equal(1, h.VezesLotada);
        Assert.True(h.Leituras.Zip(h.Leituras.Skip(1)).All(p => p.First.RegistradaEmUtc <= p.Second.RegistradaEmUtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24 * 31)]
    public async Task Historico_limita_a_janela(int horas)
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa 1");
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<ValidacaoException>(() => c.Ocupacao(db).HistoricoAsync(id, horas));
    }
}
