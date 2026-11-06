using Aquenda.Application.Comum;
using Aquenda.Application.Rodas;
using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Tests.Infra;

namespace Aquenda.Tests.Aplicacao;

public class MetricaServiceTests
{
    [Fact]
    public async Task Taxa_de_conversao_e_acoes_sobre_buscas()
    {
        using var c = await Cenario.ComSeedAsync();
        var ubs = await c.IdPorNomeAsync("UBS República");
        var casa = await c.IdPorNomeAsync("Casa Florescer");

        await using (var db = c.Contexto())
        {
            var m = c.Metricas(db);
            for (var i = 0; i < 4; i++) await m.RegistrarBuscaAsync(CategoriaServico.Saude);
            await m.RegistrarBuscaAsync(null);
            await m.RegistrarAcaoAsync(TipoInteracao.ComoChegar, ubs);
            await m.RegistrarAcaoAsync(TipoInteracao.Ligar, ubs);
            await m.RegistrarAcaoAsync(TipoInteracao.Ligar, casa);
            await m.RegistrarAcaoAsync(TipoInteracao.AbrirSite, casa); // não conta na conversão
        }

        await using var leitura = c.Contexto();
        var r = await c.Metricas(leitura).ResumoAsync(null, null);

        Assert.Equal(5, r.Buscas);
        Assert.Equal(1, r.ComoChegar);
        Assert.Equal(2, r.Ligar);
        Assert.Equal(1, r.AbrirSite);
        Assert.Equal(0.6, r.TaxaConversao);
        Assert.Equal(ubs, r.ServicosMaisProcurados[0].ServicoId);
        var saude = r.PorCategoria.Single(x => x.Categoria == CategoriaServico.Saude);
        Assert.Equal((4, 2), (saude.Buscas, saude.Acoes));
        Assert.Equal(DateOnly.FromDateTime(Cenario.Agora), Assert.Single(r.PorDia).Dia);
    }

    [Fact]
    public async Task Buscas_de_categorias_diferentes_ficam_em_contadores_diferentes()
    {
        using var c = await Cenario.ComSeedAsync();
        await using (var db = c.Contexto())
        {
            var m = c.Metricas(db);
            await m.RegistrarBuscaAsync(CategoriaServico.Saude);
            await m.RegistrarBuscaAsync(CategoriaServico.Juridico);
            await m.RegistrarBuscaAsync(CategoriaServico.Saude);
        }

        await using var leitura = c.Contexto();
        Assert.Equal(2, leitura.Interacoes.Count());
        Assert.Equal(2, leitura.Interacoes.Single(i => i.Categoria == CategoriaServico.Saude).Quantidade);
    }

    [Fact]
    public async Task Sem_buscas_a_taxa_e_nula()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        Assert.Null((await c.Metricas(db).ResumoAsync(null, null)).TaxaConversao);
    }

    [Fact]
    public async Task Entradas_invalidas_sao_recusadas()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        var m = c.Metricas(db);
        await Assert.ThrowsAsync<ValidacaoException>(() => m.RegistrarAcaoAsync(TipoInteracao.Busca, 1));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => m.RegistrarAcaoAsync(TipoInteracao.Ligar, 9999));
        await Assert.ThrowsAsync<ValidacaoException>(() => m.ResumoAsync(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 1)));
    }
}

public class RodaServiceTests
{
    private static NovaRoda Presencial(int localId) => new("Roda: família", "Conversa sobre família", "ONG Parceira",
        Cenario.Agora.AddDays(3), 90, Presencial: true, LocalId: localId, LinkGrupo: null, IdadeMinima: 0);

    [Fact]
    public async Task Cria_e_lista_roda_presencial()
    {
        using var c = await Cenario.ComSeedAsync();
        var centro = await c.IdPorNomeAsync("Centro de Referência LGBTI+ Brunna Valin");

        await using (var db = c.Contexto())
        {
            var criada = await c.Rodas(db).CriarAsync(Presencial(centro));
            Assert.Equal(centro, criada.Local!.Id);
            Assert.False(criada.Ilustrativa);
        }

        await using var leitura = c.Contexto();
        var proximas = await c.Rodas(leitura).ProximasAsync(7, presencial: true);
        Assert.Contains(proximas, r => r.Titulo == "Roda: família");
        Assert.Equal(proximas.OrderBy(r => r.InicioUtc), proximas);
    }

    [Fact]
    public async Task Roda_nao_pode_ser_em_endereco_sigiloso()
    {
        using var c = await Cenario.ComSeedAsync();
        var casa = await c.IdPorNomeAsync("Casa Florescer");
        await using var db = c.Contexto();
        var erro = await Assert.ThrowsAsync<DomainException>(() => c.Rodas(db).CriarAsync(Presencial(casa)));
        Assert.Contains("sigiloso", erro.Message);
    }

    [Fact]
    public async Task Local_inexistente_e_recusado()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() => c.Rodas(db).CriarAsync(Presencial(9999)));
    }

    [Fact]
    public async Task Periodo_de_busca_tem_limite()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<ValidacaoException>(() => c.Rodas(db).ProximasAsync(0, null));
        await Assert.ThrowsAsync<ValidacaoException>(() => c.Rodas(db).ProximasAsync(91, null));
    }
}
