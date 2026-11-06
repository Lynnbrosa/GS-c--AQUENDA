using Aquenda.Application.Catalogo;
using Aquenda.Application.Comum;
using Aquenda.Domain;
using Aquenda.Tests.Infra;

namespace Aquenda.Tests.Aplicacao;

public class CatalogoServiceTests
{
    private const double LatRepublica = -23.5447, LonRepublica = -46.6459;

    [Fact]
    public async Task Busca_por_raio_ordena_do_mais_perto_e_corta_quem_esta_fora()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();

        var pagina = await c.Catalogo(db).BuscarAsync(new FiltroBusca { Latitude = LatRepublica, Longitude = LonRepublica, RaioKm = 0.5 });

        Assert.NotEmpty(pagina.Itens);
        Assert.All(pagina.Itens, r => Assert.InRange(r.DistanciaKm!.Value, 0, 0.5));
        Assert.Equal(pagina.Itens.OrderBy(r => r.DistanciaKm).Select(r => r.Servico.Id), pagina.Itens.Select(r => r.Servico.Id));
        Assert.DoesNotContain(pagina.Itens, r => r.Servico.Nome == "Casa Fluida"); // ~1,3 km
    }

    [Fact]
    public async Task Sem_raio_quem_nao_tem_coordenada_vai_para_o_fim()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();

        var itens = (await c.Catalogo(db).BuscarAsync(new FiltroBusca { Latitude = LatRepublica, Longitude = LonRepublica, TamanhoPagina = 50 })).Itens;

        var primeiroSemDistancia = itens.ToList().FindIndex(r => r.DistanciaKm is null);
        Assert.True(primeiroSemDistancia > 0);
        Assert.All(itens.Skip(primeiroSemDistancia), r => Assert.Null(r.DistanciaKm));
    }

    [Fact]
    public async Task Filtro_de_categoria_e_atendimento_vai_para_o_SQL()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();

        var itens = (await c.Catalogo(db).BuscarAsync(new FiltroBusca
        {
            Categoria = CategoriaServico.Saude,
            Atendimento = Atendimento.Hormonizacao | Atendimento.SaudeMental
        })).Itens;

        var unico = Assert.Single(itens);
        Assert.Equal("CR POP TT Janaína Lima", unico.Servico.Nome);
    }

    [Fact]
    public async Task Texto_com_curinga_nao_vira_coringa_do_LIKE()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        var catalogo = c.Catalogo(db);

        Assert.Empty((await catalogo.BuscarAsync(new FiltroBusca { Texto = "%" })).Itens);
        Assert.Equal(3, (await catalogo.BuscarAsync(new FiltroBusca { Texto = "casa" })).Total);
    }

    [Fact]
    public async Task Somente_abertos_usa_o_relogio()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();

        // Segunda 12h em SP: Centro de Referência (9–17h) aberto. Casa Florescer é 24h.
        var abertos = (await c.Catalogo(db).BuscarAsync(new FiltroBusca { SomenteAbertos = true })).Itens.Select(r => r.Servico.Nome).ToList();
        Assert.Contains("Centro de Referência LGBTI+ Brunna Valin", abertos);
        Assert.Contains("Casa Florescer", abertos);

        // Segunda 23h em SP: o Centro fecha, a casa 24h continua.
        c.Relogio.AgoraUtc = new DateTime(2026, 11, 17, 2, 0, 0, DateTimeKind.Utc);
        abertos = (await c.Catalogo(db).BuscarAsync(new FiltroBusca { SomenteAbertos = true })).Itens.Select(r => r.Servico.Nome).ToList();
        Assert.DoesNotContain("Centro de Referência LGBTI+ Brunna Valin", abertos);
        Assert.Contains("Casa Florescer", abertos);
    }

    [Fact]
    public async Task Paginacao_mantem_o_total_e_so_a_primeira_pagina_conta_como_busca()
    {
        using var c = await Cenario.ComSeedAsync();
        await using (var db = c.Contexto())
        {
            var catalogo = c.Catalogo(db);
            var p1 = await catalogo.BuscarAsync(new FiltroBusca { TamanhoPagina = 4 });
            var p3 = await catalogo.BuscarAsync(new FiltroBusca { TamanhoPagina = 4, Pagina = 3 });

            Assert.Equal(10, p1.Total);
            Assert.Equal(3, p1.TotalPaginas);
            Assert.Equal(2, p3.Itens.Count);
            Assert.Empty(p1.Itens.Select(r => r.Servico.Id).Intersect(p3.Itens.Select(r => r.Servico.Id)));
        }

        await using var leitura = c.Contexto();
        var impacto = await c.Metricas(leitura).ResumoAsync(null, null);
        Assert.Equal(1, impacto.Buscas);
    }

    [Theory]
    [InlineData(-23.5, null, null)]
    [InlineData(null, null, 5.0)]
    [InlineData(-23.5, -46.6, 51.0)]
    [InlineData(-95.0, -46.6, null)]
    public async Task Parametros_invalidos_viram_ValidacaoException(double? lat, double? lon, double? raio)
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<ValidacaoException>(() =>
            c.Catalogo(db).BuscarAsync(new FiltroBusca { Latitude = lat, Longitude = lon, RaioKm = raio }));
    }

    [Fact]
    public async Task Servico_inexistente_vira_nao_encontrado()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => c.Catalogo(db).ObterAsync(9999));
    }

    [Fact]
    public async Task Tres_avisos_colocam_o_servico_em_revisao_no_resumo()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa Fluida");

        for (var i = 0; i < 3; i++)
        {
            await using var db = c.Contexto();
            await c.Catalogo(db).AvisarFechamentoAsync(id);
        }

        await using var leitura = c.Contexto();
        var resumo = await c.Catalogo(leitura).ResumoAsync();
        Assert.Equal(1, resumo.ParaRevisao);
        Assert.Equal(4, resumo.PorCategoria[CategoriaServico.Role]);
        Assert.Equal(10, resumo.Total);
    }
}
