using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Tests.Dominio;

public class EnderecoTests
{
    [Fact]
    public void Endereco_sigiloso_mostra_so_o_bairro()
    {
        var e = new Endereco(null, "Centro", sigiloso: true);
        Assert.Equal("Centro, São Paulo", e.TextoPublico());
    }

    [Fact]
    public void Endereco_sigiloso_nao_aceita_logradouro() =>
        Assert.Throws<DomainException>(() => new Endereco("R. Secreta, 10", "Centro", sigiloso: true));

    [Fact]
    public void Endereco_publico_mostra_rua_e_bairro() =>
        Assert.Equal("R. Bela Cintra, 569 · Consolação, São Paulo",
            new Endereco("R. Bela Cintra, 569", "Consolação").TextoPublico());

    [Fact]
    public void Bairro_e_obrigatorio() =>
        Assert.Throws<DomainException>(() => new Endereco("Rua", " "));

    [Fact]
    public void Coordenada_so_existe_com_latitude_e_longitude()
    {
        Assert.Null(new Endereco(null, "Centro").Coordenada);
        Assert.Equal(new Coordenada(-23.5, -46.6), new Endereco(null, "Centro", coordenada: new Coordenada(-23.5, -46.6)).Coordenada);
    }
}
