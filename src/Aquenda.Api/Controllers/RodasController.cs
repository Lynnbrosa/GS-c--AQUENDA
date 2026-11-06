using Aquenda.Api.Contratos;
using Aquenda.Api.Infra;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Rodas;
using Microsoft.AspNetCore.Mvc;

namespace Aquenda.Api.Controllers;

/// <summary>Rodas de conversa e eventos de ONGs parceiras.</summary>
[ApiController]
[Route("api/rodas")]
[Produces("application/json")]
public class RodasController : ControllerBase
{
    private readonly IRodaService _rodas;

    public RodasController(IRodaService rodas) => _rodas = rodas;

    /// <summary>Próximas rodas nos próximos N dias (padrão 30).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RodaResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RodaResposta>>> Proximas([FromQuery] int dias = 30,
        [FromQuery] bool? presencial = null, CancellationToken ct = default)
    {
        var rodas = await _rodas.ProximasAsync(dias, presencial, ct);
        return Ok(rodas.Select(r => r.ParaResposta()).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<RodaResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RodaResposta>> Obter(int id, CancellationToken ct) =>
        Ok((await _rodas.ObterAsync(id, ct)).ParaResposta());

    /// <summary>Cadastra uma roda (uso da ONG parceira).</summary>
    [HttpPost]
    [ChaveOperador]
    [ProducesResponseType<RodaResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RodaResposta>> Criar([FromBody] NovaRodaRequisicao req, CancellationToken ct)
    {
        var roda = await _rodas.CriarAsync(new NovaRoda(
            req.Titulo, req.Descricao, req.Organizador, req.Inicio!.Value.UtcDateTime, req.DuracaoMinutos,
            req.Presencial, req.LocalId, req.LinkGrupo, req.IdadeMinima), ct);
        return CreatedAtAction(nameof(Obter), new { id = roda.Id }, roda.ParaResposta());
    }
}
