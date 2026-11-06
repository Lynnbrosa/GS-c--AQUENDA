using Aquenda.Domain.Comum;

namespace Aquenda.Domain.ValueObjects;

/// <summary>
/// Endereço de um serviço. Casas de acolhimento costumam ter endereço sigiloso:
/// nesse caso só o bairro é mostrado e a coordenada guardada é a do bairro.
/// </summary>
public sealed class Endereco
{
    public string? Logradouro { get; private set; }
    public string Bairro { get; private set; } = null!;
    public string Cidade { get; private set; } = null!;
    public string? Cep { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public bool Sigiloso { get; private set; }

    private Endereco() { } // EF Core

    public Endereco(string? logradouro, string bairro, string cidade = "São Paulo",
        string? cep = null, Coordenada? coordenada = null, bool sigiloso = false)
    {
        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("Bairro é obrigatório.");
        if (string.IsNullOrWhiteSpace(cidade))
            throw new DomainException("Cidade é obrigatória.");
        if (sigiloso && !string.IsNullOrWhiteSpace(logradouro))
            throw new DomainException("Endereço sigiloso não pode ter logradouro salvo.");

        Logradouro = string.IsNullOrWhiteSpace(logradouro) ? null : logradouro.Trim();
        Bairro = bairro.Trim();
        Cidade = cidade.Trim();
        Cep = cep;
        Latitude = coordenada?.Latitude;
        Longitude = coordenada?.Longitude;
        Sigiloso = sigiloso;
    }

    public Coordenada? Coordenada =>
        Latitude is double lat && Longitude is double lon ? new Coordenada(lat, lon) : null;

    /// <summary>Texto que pode aparecer na tela. Nunca expõe o logradouro de um endereço sigiloso.</summary>
    public string TextoPublico()
    {
        if (Sigiloso || Logradouro is null)
            return $"{Bairro}, {Cidade}";
        return $"{Logradouro} · {Bairro}, {Cidade}";
    }
}
