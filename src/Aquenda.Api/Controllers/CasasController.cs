using Aquenda.Api.Contratos;
using Aquenda.Api.Infra;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Aquenda.Api.Controllers;

/// <summary>Vagas das casas de acolhimento, alimentadas pelo ESP32 (MQTT) ou pela equipe da casa.</summary>
[ApiController]
[Route("api/casas")]
[Produces("application/json")]
public class CasasController : ControllerBase
{
    private readonly IOcupacaoService _ocupacao;

    public CasasController(IOcupacaoService ocupacao) => _ocupacao = ocupacao;

    /// <summary>Painel com as vagas de todas as casas, casas com vaga primeiro.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VagasResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VagasResposta>>> Painel([FromQuery] bool somenteComVaga, CancellationToken ct)
    {
        var painel = await _ocupacao.PainelAsync(somenteComVaga, ct);
        return Ok(painel.Select(v => v.ParaResposta()).ToList());
    }

    /// <summary>"Vagas agora" de uma casa. É o endpoint que o app consome (contrato do grupo).</summary>
    [HttpGet("{id:int}/vagas")]
    [ProducesResponseType<VagasResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VagasResposta>> Vagas(int id, CancellationToken ct) =>
        Ok((await _ocupacao.ObterVagasAsync(id, ct)).ParaResposta());

    /// <summary>Registra uma contagem feita à mão (reserva quando o sensor está fora).</summary>
    /// <remarks>Corpo igual ao payload MQTT: {"total":10,"ocupadas":7,"ts":"2026-11-14T21:00:00Z"}</remarks>
    [HttpPost("{id:int}/vagas")]
    [ChaveOperador]
    [ProducesResponseType<VagasResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VagasResposta>> RegistrarLeitura(int id, [FromBody] LeituraRequisicao req, CancellationToken ct)
    {
        var leitura = new NovaLeitura(req.Total!.Value, req.Ocupadas!.Value, req.Ts?.UtcDateTime);
        var vagas = await _ocupacao.RegistrarAsync(id, leitura, OrigemLeitura.Manual, ct);
        return Ok(vagas.ParaResposta());
    }

    /// <summary>Histórico de ocupação, com média, pico e quantas vezes lotou.</summary>
    [HttpGet("{id:int}/vagas/historico")]
    [ProducesResponseType<HistoricoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricoResposta>> Historico(int id, [FromQuery] int horas = 24, CancellationToken ct = default) =>
        Ok((await _ocupacao.HistoricoAsync(id, horas, ct)).ParaResposta());
}
