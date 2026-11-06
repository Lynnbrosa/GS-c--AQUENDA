using System.Text.Json;
using System.Text.Json.Serialization;
using Aquenda.Application.Ocupacao;

namespace Aquenda.Infrastructure.Mqtt;

public sealed record MensagemVagas(int CasaId, NovaLeitura Leitura);

/// <summary>
/// Lê o tópico "aquenda/casa/{casaId}/vagas" e o payload
/// {"total":10,"ocupadas":7,"ts":"2026-11-14T21:00:00Z"}. Função pura, testável sem broker.
/// </summary>
public static class MqttMensagemParser
{
    public const int TamanhoMaximoPayload = 1024;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.Strict
    };

    private sealed record Payload(int? Total, int? Ocupadas, DateTimeOffset? Ts);

    public static MensagemVagas Ler(string topico, ReadOnlySpan<byte> payload)
    {
        var casaId = LerCasaId(topico);

        if (payload.IsEmpty)
            throw new FormatException("Payload vazio.");
        if (payload.Length > TamanhoMaximoPayload)
            throw new FormatException($"Payload maior que {TamanhoMaximoPayload} bytes.");

        Payload? dados;
        try { dados = JsonSerializer.Deserialize<Payload>(payload, Json); }
        catch (JsonException e) { throw new FormatException($"JSON inválido: {e.Message}", e); }

        if (dados?.Total is not int total || dados.Ocupadas is not int ocupadas)
            throw new FormatException("Payload precisa de \"total\" e \"ocupadas\".");

        return new MensagemVagas(casaId, new NovaLeitura(total, ocupadas, dados.Ts?.UtcDateTime));
    }

    public static int LerCasaId(string topico)
    {
        var partes = topico.Split('/');
        if (partes.Length != 4 || partes[0] != "aquenda" || partes[1] != "casa" || partes[3] != "vagas")
            throw new FormatException($"Tópico fora do contrato: {topico}");
        if (!int.TryParse(partes[2], out var casaId) || casaId <= 0)
            throw new FormatException($"Id de casa inválido no tópico: {partes[2]}");
        return casaId;
    }
}
