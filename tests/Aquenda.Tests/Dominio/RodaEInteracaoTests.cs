using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;

namespace Aquenda.Tests.Dominio;

public class RodaEInteracaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static RodaConversa Online(int idade, string? link = "https://chat.ong.org/sala") =>
        new("Primeiro emprego", "", "ONG", Agora.AddDays(2), 90, presencial: false, localId: null,
            linkGrupo: link, idadeMinima: idade, ilustrativa: false, agoraUtc: Agora);

    [Fact]
    public void Roda_online_e_so_para_maiores_de_18()
    {
        Assert.Throws<DomainException>(() => Online(16));
        Assert.Equal(18, Online(18).IdadeMinima);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://inseguro.org")]
    [InlineData("nao e link")]
    public void Roda_online_precisa_de_link_https(string? link) =>
        Assert.Throws<DomainException>(() => Online(18, link));

    [Fact]
    public void Roda_presencial_precisa_de_local() =>
        Assert.Throws<DomainException>(() => new RodaConversa("Roda", "", "ONG", Agora.AddDays(1), 60, true, null, null, 0, false, Agora));

    [Fact]
    public void Roda_no_passado_e_recusada() =>
        Assert.Throws<DomainException>(() => new RodaConversa("Roda", "", "ONG", Agora.AddMinutes(-1), 60, true, 1, null, 0, false, Agora));

    [Fact]
    public void Roda_acontece_entre_quando_ha_sobreposicao()
    {
        var roda = Online(18); // começa em Agora+2d, dura 90 min
        Assert.True(roda.AconteceEntre(Agora, Agora.AddDays(3)));
        Assert.True(roda.AconteceEntre(roda.InicioUtc.AddMinutes(30), roda.InicioUtc.AddMinutes(40)));
        Assert.False(roda.AconteceEntre(roda.FimUtc, roda.FimUtc.AddHours(1)));
    }

    [Fact]
    public void Acao_sem_servico_e_recusada() =>
        Assert.Throws<DomainException>(() => new InteracaoDiaria(new DateOnly(2026, 11, 6), TipoInteracao.Ligar, null, null));

    [Fact]
    public void Contador_comeca_em_zero_e_incrementa()
    {
        var c = new InteracaoDiaria(new DateOnly(2026, 11, 6), TipoInteracao.Busca, null, null);
        c.Incrementar();
        c.Incrementar();
        Assert.Equal(2, c.Quantidade);
    }
}
