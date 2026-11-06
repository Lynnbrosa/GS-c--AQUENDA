using Aquenda.Api.Contratos;
using Aquenda.Application.Metricas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aquenda.Api.Controllers;

/// <summary>
/// Indicador de impacto: buscas que terminam em "como chegar" ou "ligar".
/// Só contadores por dia; nada identifica quem buscou.
/// </summary>
[ApiController]
[Route("api/metricas")]
[Produces("application/json")]
public class MetricasController : ControllerBase
{
    private readonly IMetricaService _metricas;

    public MetricasController(IMetricaService metricas) => _metricas = metricas;

    /// <summary>Conta um toque em "como chegar", "ligar" ou "abrir site".</summary>
    [HttpPost("interacoes")]
    [EnableRateLimiting(Program.LimiteAnonimo)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Registrar([FromBody] InteracaoRequisicao req, CancellationToken ct)
    {
        await _metricas.RegistrarAcaoAsync(req.Tipo!.Value, req.ServicoId!.Value, ct);
        return NoContent();
    }

    /// <summary>Resumo do período (padrão: últimos 30 dias).</summary>
    [HttpGet("impacto")]
    [ProducesResponseType<ResumoImpacto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumoImpacto>> Impacto([FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        Ok(await _metricas.ResumoAsync(de, ate, ct));
}
