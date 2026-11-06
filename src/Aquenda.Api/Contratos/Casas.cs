using System.ComponentModel.DataAnnotations;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain;

namespace Aquenda.Api.Contratos;

/// <summary>Mesmo formato do payload MQTT do ESP32.</summary>
public sealed record LeituraRequisicao
{
    [Required, Range(1, 500)]
    public int? Total { get; init; }

    [Required, Range(0, 500)]
    public int? Ocupadas { get; init; }

    /// <summary>Quando a contagem foi feita. Se vier vazio, vale o horário do servidor.</summary>
    public DateTimeOffset? Ts { get; init; }
}

public sealed record VagasResposta(
    int CasaId,
    string Casa,
    SituacaoVagas Situacao,
    string Mensagem,
    int? Total,
    int? Ocupadas,
    int? Livres,
    DateTimeOffset? AtualizadoEm,
    OrigemLeitura? Origem,
    string ComoAcessar);

public sealed record LeituraResposta(int Total, int Ocupadas, int Livres, DateTimeOffset RegistradaEm, OrigemLeitura Origem);

public sealed record HistoricoResposta(
    int CasaId,
    DateTimeOffset Desde,
    double? OcupacaoMedia,
    int? PicoOcupadas,
    int VezesLotada,
    IReadOnlyList<LeituraResposta> Leituras);
