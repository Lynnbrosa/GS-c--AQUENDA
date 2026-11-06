using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Tests.Dominio;

public class CoordenadaTests
{
    private static readonly Coordenada Republica = new(-23.5447, -46.6459);

    [Fact]
    public void Distancia_ate_o_mesmo_ponto_e_zero() =>
        Assert.Equal(0, Republica.DistanciaKmAte(Republica), 6);

    [Fact]
    public void Distancia_e_simetrica()
    {
        var paulista = new Coordenada(-23.5614, -46.6559);
        Assert.Equal(Republica.DistanciaKmAte(paulista), paulista.DistanciaKmAte(Republica), 9);
    }

    [Fact]
    public void Distancia_Sao_Paulo_Rio_fica_perto_de_360_km()
    {
        var rio = new Coordenada(-22.9068, -43.1729);
        Assert.InRange(new Coordenada(-23.5505, -46.6333).DistanciaKmAte(rio), 355, 365);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Recusa_coordenada_fora_do_globo(double lat, double lon) =>
        Assert.Throws<DomainException>(() => new Coordenada(lat, lon));

    [Fact]
    public void Caixa_envolvente_contem_pontos_dentro_do_raio()
    {
        var caixa = Republica.CaixaEnvolvente(2);
        var pertinho = new Coordenada(-23.5530, -46.6545); // ~1,3 km
        Assert.True(Republica.DistanciaKmAte(pertinho) < 2);
        Assert.InRange(pertinho.Latitude, caixa.LatMin, caixa.LatMax);
        Assert.InRange(pertinho.Longitude, caixa.LonMin, caixa.LonMax);
    }
}
