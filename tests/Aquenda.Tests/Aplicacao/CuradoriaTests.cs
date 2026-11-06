using Aquenda.Application.Catalogo;
using Aquenda.Application.Curadoria;
using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Tests.Infra;

namespace Aquenda.Tests.Aplicacao;

public class CuradoriaServiceTests
{
    private static NovoServico Ubs(string? cep = "01049-000", bool sigiloso = false) => new()
    {
        Categoria = CategoriaServico.Saude,
        Nome = "UBS Santa Cecília",
        Bairro = "Santa Cecília",
        Logradouro = sigiloso ? null : "R. Exemplo, 10",
        Cep = cep,
        EnderecoSigiloso = sigiloso,
        Fonte = "https://cnes.datasus.gov.br",
        Atendimentos = Atendimento.Hormonizacao,
        TipoAtencao = TipoAtencao.Primaria,
        RedeSampaTrans = true,
        Horarios = new[] { new NovoHorario(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(19, 0)) }
    };

    [Fact]
    public async Task Cria_o_tipo_certo_e_busca_coordenada_pelo_cep()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();

        var criado = await c.Curadoria(db).CriarAsync(Ubs());

        var ubs = Assert.IsType<UnidadeSaude>(criado);
        Assert.Equal(new[] { "01049000" }, c.Geo.Consultas);
        Assert.Equal(-23.5475, ubs.Endereco.Latitude);
        Assert.Equal("01049000", ubs.Endereco.Cep);
        Assert.Single(ubs.Horarios);
        Assert.Equal(DateOnly.FromDateTime(Cenario.Agora), ubs.VerificadoEm);
    }

    [Fact]
    public async Task Coordenada_informada_ou_endereco_sigiloso_nao_consultam_cep()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        var curadoria = c.Curadoria(db);

        await curadoria.CriarAsync(Ubs() with { Nome = "Com coordenada", Latitude = -23.5, Longitude = -46.6 });
        await curadoria.CriarAsync(new NovoServico
        {
            Categoria = CategoriaServico.Acolhimento, Nome = "Casa nova", Bairro = "Centro", Cep = "01049000",
            EnderecoSigiloso = true, Fonte = "https://smads", PublicoAlvo = "Pessoas trans", ExigeEncaminhamento = true
        });

        Assert.Empty(c.Geo.Consultas);
    }

    [Fact]
    public async Task Fonte_fora_do_ar_nao_impede_o_cadastro()
    {
        using var c = await Cenario.ComSeedAsync();
        c.Geo.Resposta = null;
        await using var db = c.Contexto();
        var criado = await c.Curadoria(db).CriarAsync(Ubs());
        Assert.Null(criado.Endereco.Coordenada);
    }

    [Theory]
    [InlineData(CategoriaServico.Saude)]
    [InlineData(CategoriaServico.Role)]
    [InlineData(CategoriaServico.Acolhimento)]
    public async Task Campo_obrigatorio_da_categoria_faltando_e_recusado(CategoriaServico categoria)
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() => c.Curadoria(db).CriarAsync(new NovoServico
        {
            Categoria = categoria, Nome = "X", Bairro = "Centro", Fonte = "https://f"
        }));
    }

    [Fact]
    public async Task Cep_invalido_e_recusado()
    {
        using var c = await Cenario.ComSeedAsync();
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() => c.Curadoria(db).CriarAsync(Ubs("123")));
    }

    [Fact]
    public async Task Desativar_tira_da_busca_e_reativar_devolve()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa Fluida");

        await using (var db = c.Contexto()) await c.Curadoria(db).DesativarAsync(id);
        await using (var db = c.Contexto())
            Assert.DoesNotContain((await c.Catalogo(db).BuscarAsync(new FiltroBusca { TamanhoPagina = 50 })).Itens, r => r.Servico.Id == id);

        await using (var db = c.Contexto()) await c.Curadoria(db).ReativarAsync(id);
        await using (var db = c.Contexto())
            Assert.Contains((await c.Catalogo(db).BuscarAsync(new FiltroBusca { TamanhoPagina = 50 })).Itens, r => r.Servico.Id == id);
    }

    [Fact]
    public async Task Fila_de_revisao_junta_avisos_e_verificacao_vencida_sem_repetir()
    {
        using var c = await Cenario.ComSeedAsync();
        var fluida = await c.IdPorNomeAsync("Casa Fluida");
        for (var i = 0; i < 3; i++)
        {
            await using var db = c.Contexto();
            await c.Catalogo(db).AvisarFechamentoAsync(fluida);
        }

        await using (var db = c.Contexto())
            Assert.Equal(fluida, Assert.Single(await c.Curadoria(db).FilaRevisaoAsync()).Servico.Id);

        // Sete meses depois, todo o seed (verificado em 05/11/2026) venceu.
        c.Relogio.AgoraUtc = new DateTime(2027, 6, 10, 12, 0, 0, DateTimeKind.Utc);
        await using (var db = c.Contexto())
        {
            var fila = await c.Curadoria(db).FilaRevisaoAsync();
            Assert.Equal(10, fila.Count);
            Assert.Contains("avisos", fila.Single(i => i.Servico.Id == fluida).Motivo);
        }

        await using (var db = c.Contexto()) await c.Curadoria(db).MarcarVerificadoAsync(fluida, null);
        await using (var db = c.Contexto())
            Assert.DoesNotContain(await c.Curadoria(db).FilaRevisaoAsync(), i => i.Servico.Id == fluida);
    }

    [Fact]
    public async Task Verificacao_no_futuro_e_recusada()
    {
        using var c = await Cenario.ComSeedAsync();
        var id = await c.IdPorNomeAsync("Casa Fluida");
        await using var db = c.Contexto();
        await Assert.ThrowsAsync<DomainException>(() =>
            c.Curadoria(db).MarcarVerificadoAsync(id, DateOnly.FromDateTime(Cenario.Agora).AddDays(1)));
    }
}

public class IngestaoOsmServiceTests
{
    private static LugarOsm Lugar(long id, string? nome, string tag = "primary", string? bairro = "Consolação") =>
        new($"osm:node/{id}", nome, TipoRole.Bar, tag, -23.553, -46.655, "Rua Frei Caneca", "100", bairro,
            null, null, Acessivel: true, $"https://www.openstreetmap.org/node/{id}");

    [Fact]
    public async Task Importa_novos_e_reimportar_atualiza_sem_duplicar()
    {
        using var c = await Cenario.ComSeedAsync();
        c.Osm.Lugares.AddRange(new[] { Lugar(1, "Bar Arco-íris"), Lugar(2, "Balada X", "welcome", bairro: null), Lugar(3, null) });

        await using (var db = c.Contexto())
        {
            var r = await c.Ingestao(db).ImportarRolesAsync();
            Assert.Equal((3, 2, 0, 1), (r.Lidos, r.Novos, r.Atualizados, r.Ignorados));
        }

        c.Osm.Lugares[0] = Lugar(1, "Bar Arco-íris Novo");
        await using (var db = c.Contexto())
        {
            var r = await c.Ingestao(db).ImportarRolesAsync();
            Assert.Equal((0, 2), (r.Novos, r.Atualizados));
        }

        await using var leitura = c.Contexto();
        var importados = leitura.Servicos.Where(s => s.IdExterno != null).ToList().OfType<LocalRole>().ToList();
        Assert.Equal(2, importados.Count);
        var bar = importados.Single(r => r.IdExterno == "osm:node/1");
        Assert.Equal("Bar Arco-íris Novo", bar.Nome);
        Assert.Equal("primary", bar.TagOsm);
        Assert.Equal("Rua Frei Caneca, 100 · Consolação, São Paulo", bar.Endereco.TextoPublico());
        Assert.Equal("https://www.openstreetmap.org/node/1", bar.Fonte);
        Assert.Equal("a confirmar", importados.Single(r => r.IdExterno == "osm:node/2").Endereco.Bairro);
    }

    [Fact]
    public async Task Item_com_tag_desconhecida_vira_aviso_e_nao_derruba_a_importacao()
    {
        using var c = await Cenario.ComSeedAsync();
        c.Osm.Lugares.AddRange(new[] { Lugar(1, "Ok"), Lugar(2, "Estranho", tag: "talvez") });
        await using var db = c.Contexto();
        var r = await c.Ingestao(db).ImportarRolesAsync();
        Assert.Equal(1, r.Novos);
        Assert.Equal(1, r.Ignorados);
        Assert.Contains("osm:node/2", Assert.Single(r.Avisos));
    }
}
