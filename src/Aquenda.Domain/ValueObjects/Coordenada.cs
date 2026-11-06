using Aquenda.Domain.Comum;

namespace Aquenda.Domain.ValueObjects;

/// <summary>
/// Ponto geográfico (WGS84). Imutável e comparado por valor.
/// </summary>
public readonly record struct Coordenada
{
    private const double RaioTerraKm = 6371.0088;

    public double Latitude { get; }
    public double Longitude { get; }

    public Coordenada(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude deve estar entre -90 e 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude deve estar entre -180 e 180.");
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Distância em linha reta (fórmula de Haversine), em km.</summary>
    public double DistanciaKmAte(Coordenada outra)
    {
        var dLat = Radianos(outra.Latitude - Latitude);
        var dLon = Radianos(outra.Longitude - Longitude);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(Radianos(Latitude)) * Math.Cos(Radianos(outra.Latitude)) *
                Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * RaioTerraKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>
    /// Retângulo que contém o círculo de raio <paramref name="raioKm"/>.
    /// Serve para filtrar no banco antes de calcular a distância exata em memória.
    /// </summary>
    public (double LatMin, double LatMax, double LonMin, double LonMax) CaixaEnvolvente(double raioKm)
    {
        if (raioKm <= 0) throw new DomainException("Raio deve ser positivo.");
        var dLat = raioKm / 111.32;
        var cosLat = Math.Max(Math.Cos(Radianos(Latitude)), 0.01);
        var dLon = raioKm / (111.32 * cosLat);
        return (Latitude - dLat, Latitude + dLat, Longitude - dLon, Longitude + dLon);
    }

    private static double Radianos(double graus) => graus * Math.PI / 180.0;
}
