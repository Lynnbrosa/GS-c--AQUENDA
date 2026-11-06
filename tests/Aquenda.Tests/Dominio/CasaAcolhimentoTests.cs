using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Tests.Dominio;

public class CasaAcolhimentoTests
{
    private static readonly DateTime Agora = new(2026, 11, 14, 21, 0, 0, DateTimeKind.Utc);

    private static CasaAcolhimento NovaCasa() => new("Casa Florescer", "", new Endereco(null, "Centro", sigiloso: true),
        "https://fonte", new DateOnly(2026, 11, 5), "Mulheres trans e travestis", exigeEncaminhamento: true);

    [Fact]
    public void Leitura_valida_calcula_vagas_livres()
    {
        var leitura = NovaCasa().RegistrarLeitura(10, 7, Agora, OrigemLeitura.Sensor, Agora);
        Assert.Equal(3, leitura.Livres);
        Assert.Equal(0.7, leitura.TaxaOcupacao, 6);
        Assert.False(leitura.Lotada);
    }

    [Fact]
    public void Casa_cheia_fica_lotada() =>
        Assert.True(NovaCasa().RegistrarLeitura(10, 10, Agora, OrigemLeitura.Sensor, Agora).Lotada);

    [Theory]
    [InlineData(10, 11)]
    [InlineData(10, -1)]
    [InlineData(0, 0)]
    [InlineData(501, 1)]
    public void Leitura_impossivel_e_recusada(int total, int ocupadas) =>
        Assert.Throws<DomainException>(() => NovaCasa().RegistrarLeitura(total, ocupadas, Agora, OrigemLeitura.Sensor, Agora));

    [Fact]
    public void Leitura_do_futuro_e_recusada_mas_tolera_relogio_um_pouco_adiantado()
    {
        var casa = NovaCasa();
        casa.RegistrarLeitura(10, 1, Agora.AddMinutes(4), OrigemLeitura.Sensor, Agora);
        Assert.Throws<DomainException>(() => casa.RegistrarLeitura(10, 1, Agora.AddMinutes(6), OrigemLeitura.Sensor, Agora));
    }

    [Fact]
    public void Casa_inativa_nao_recebe_leitura()
    {
        var casa = NovaCasa();
        casa.Desativar();
        Assert.Throws<DomainException>(() => casa.RegistrarLeitura(10, 1, Agora, OrigemLeitura.Sensor, Agora));
    }

    [Fact]
    public void Leitura_fica_desatualizada_depois_de_duas_horas()
    {
        var leitura = NovaCasa().RegistrarLeitura(10, 1, Agora, OrigemLeitura.Sensor, Agora);
        Assert.False(leitura.EstaDesatualizada(Agora + LeituraOcupacao.Validade));
        Assert.True(leitura.EstaDesatualizada(Agora + LeituraOcupacao.Validade + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Casa_sempre_oferece_acolhida_e_guarda_leituras()
    {
        var casa = NovaCasa();
        casa.RegistrarLeitura(10, 1, Agora, OrigemLeitura.Sensor, Agora);
        casa.RegistrarLeitura(10, 2, Agora, OrigemLeitura.Manual, Agora);
        Assert.True(casa.Oferece(Atendimento.Acolhida));
        Assert.Equal(2, casa.Leituras.Count);
    }
}
