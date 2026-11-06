using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Tests.Dominio;

public class ServicoTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 5);
    private static readonly Endereco Centro = new("R. Maj. Sertório, 292", "República", coordenada: new Coordenada(-23.5447, -46.6459));

    internal static CentroReferencia NovoCentro() => new("Centro de Referência", "", Centro, "https://fonte", Hoje,
        Atendimento.Psicossocial | Atendimento.Juridico, inscreveTranscidadania: true);

    [Fact]
    public void Servico_sem_fonte_e_recusado() =>
        Assert.Throws<DomainException>(() => new CentroReferencia("X", "", Centro, " ", Hoje, Atendimento.Nenhum, false));

    [Fact]
    public void Servico_sem_nome_e_recusado() =>
        Assert.Throws<DomainException>(() => new CentroReferencia("", "", Centro, "https://f", Hoje, Atendimento.Nenhum, false));

    [Fact]
    public void Atendimentos_funcionam_como_flags()
    {
        var centro = NovoCentro();
        Assert.True(centro.Oferece(Atendimento.Psicossocial));
        Assert.True(centro.Oferece(Atendimento.Juridico));
        Assert.False(centro.Oferece(Atendimento.Hormonizacao));
        Assert.False(centro.Oferece(Atendimento.Nenhum));
    }

    [Fact]
    public void Cada_tipo_explica_seu_proprio_jeito_de_entrar()
    {
        // Polimorfismo: a mesma chamada, uma instrução diferente para cada tipo.
        Servico[] servicos =
        {
            NovoCentro(),
            new UnidadeSaude("UBS", "", Centro, "f", Hoje, Atendimento.Hormonizacao, TipoAtencao.Primaria, redeSampaTrans: true),
            new UnidadeSaude("CR", "", Centro, "f", Hoje, Atendimento.Hormonizacao, TipoAtencao.Secundaria, true, exigeEncaminhamento: true),
            new CasaAcolhimento("Casa", "", new Endereco(null, "Centro", sigiloso: true), "f", Hoje, "Pessoas trans", exigeEncaminhamento: true),
            new ServicoJuridico("Defensoria", "", Centro, "f", Hoje, "Defensoria", atendeRetificacao: true, recebeDenuncia: true),
            new LocalRole("Bar", "", Centro, "f", Hoje, TipoRole.Bar, 1, true)
        };

        var instrucoes = servicos.Select(s => s.ComoAcessar()).ToList();

        Assert.Equal(instrucoes.Count, instrucoes.Distinct().Count());
        Assert.Contains("Transcidadania", instrucoes[0]);
        Assert.Contains("SAMPA Trans", instrucoes[1]);
        Assert.Contains("encaminhamento", instrucoes[2]);
        Assert.Contains("CRAS", instrucoes[3]);
        Assert.Contains("cartório", instrucoes[4]);
    }

    [Fact]
    public void Instrucao_da_curadoria_vence_a_padrao()
    {
        var centro = NovoCentro();
        centro.DefinirInstrucaoAcesso("Chegue antes das 16h.");
        Assert.Equal("Chegue antes das 16h.", centro.ComoAcessar());
    }

    [Fact]
    public void Categoria_vem_do_tipo_concreto()
    {
        Assert.Equal(CategoriaServico.CentroReferencia, NovoCentro().Categoria);
        Assert.Equal(CategoriaServico.Role, new LocalRole("Bar", "", Centro, "f", Hoje, TipoRole.Bar, 1, true).Categoria);
    }

    [Fact]
    public void Aberto_agora_usa_horario_de_Sao_Paulo()
    {
        var centro = NovoCentro();
        centro.DefinirHorarios(HorarioFuncionamento.SegundaASexta(9, 17));

        // Segunda 2026-11-02, 12h UTC = 9h em São Paulo.
        Assert.True(centro.EstaAbertoEm(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc)));
        // Segunda 11h UTC = 8h em São Paulo: ainda fechado.
        Assert.False(centro.EstaAbertoEm(new DateTime(2026, 11, 2, 11, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Sem_horario_cadastrado_a_resposta_e_nao_sei() =>
        Assert.Null(NovoCentro().EstaAbertoEm(DateTime.UtcNow));

    [Fact]
    public void Tres_avisos_de_fechamento_mandam_para_revisao_e_verificacao_zera()
    {
        var centro = NovoCentro();
        var agora = DateTime.UtcNow;
        for (var i = 0; i < Servico.AvisosParaRevisao; i++)
        {
            Assert.False(centro.PrecisaRevisao);
            centro.RegistrarAvisoFechamento(agora);
        }
        Assert.True(centro.PrecisaRevisao);

        centro.MarcarVerificado(Hoje.AddDays(1));
        Assert.False(centro.PrecisaRevisao);
        Assert.Equal(0, centro.AvisosFechamento);
        Assert.Equal(Hoje.AddDays(1), centro.VerificadoEm);
    }

    [Fact]
    public void Unidade_de_saude_valida_cnes_e_porta_de_entrada()
    {
        Assert.Throws<DomainException>(() =>
            new UnidadeSaude("UBS", "", Centro, "f", Hoje, Atendimento.Nenhum, TipoAtencao.Primaria, false, codigoCnes: "123"));
        Assert.Throws<DomainException>(() =>
            new UnidadeSaude("UBS", "", Centro, "f", Hoje, Atendimento.Nenhum, TipoAtencao.Primaria, false, exigeEncaminhamento: true));
        var ok = new UnidadeSaude("UBS", "", Centro, "f", Hoje, Atendimento.Nenhum, TipoAtencao.Primaria, false, codigoCnes: "2077485");
        Assert.Equal("2077485", ok.CodigoCnes);
    }

    [Fact]
    public void Juridico_deriva_atendimentos_dos_dados()
    {
        var j = new ServicoJuridico("D", "", Centro, "f", Hoje, "Defensoria", atendeRetificacao: true, recebeDenuncia: false);
        Assert.True(j.Oferece(Atendimento.Juridico));
        Assert.True(j.Oferece(Atendimento.Retificacao));
        Assert.False(j.Oferece(Atendimento.Denuncia));
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(4, null)]
    [InlineData(1, "talvez")]
    public void Role_valida_preco_e_tag_osm(int preco, string? tag) =>
        Assert.Throws<DomainException>(() => new LocalRole("Bar", "", Centro, "f", Hoje, TipoRole.Bar, preco, true, tag));
}
