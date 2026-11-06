using System.ComponentModel.DataAnnotations;
using Aquenda.Domain;

namespace Aquenda.Api.Contratos;

/// <summary>Toque em "como chegar", "ligar" ou "abrir site". Não leva nada sobre quem tocou.</summary>
public sealed record InteracaoRequisicao
{
    [Required]
    public TipoInteracao? Tipo { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int? ServicoId { get; init; }
}
