using Aquenda.Api.Contratos;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Catalogo;
using Aquenda.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aquenda.Api.Controllers;

/// <summary>Catálogo: acolhimento, centros de referência, saúde, jurídico e rolês.</summary>
[ApiController]
[Route("api/servicos")]
[Produces("application/json")]
public class ServicosController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public ServicosController(ICatalogoService catalogo) => _catalogo = catalogo;

    /// <summary>Busca serviços. Com lat/lon, ordena do mais perto para o mais longe.</summary>
    /// <remarks>Exemplo: GET /api/servicos?categoria=Saude&amp;atendimento=Hormonizacao&amp;lat=-23.5447&amp;lon=-46.6459&amp;raioKm=5</remarks>
    [HttpGet]
    [ProducesResponseType<PaginaResposta<ServicoResumoResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaResposta<ServicoResumoResposta>>> Buscar(
        [FromQuery] BuscaServicosRequisicao req, CancellationToken ct)
    {
        var filtro = new FiltroBusca
        {
            Categoria = req.Categoria,
            Atendimento = (req.Atendimento ?? Array.Empty<Atendimento>()).Aggregate(Atendimento.Nenhum, (acc, a) => acc | a),
            Texto = req.Texto,
            Bairro = req.Bairro,
            Latitude = req.Lat,
            Longitude = req.Lon,
            RaioKm = req.RaioKm,
            SomenteAbertos = req.Abertos,
            Pagina = req.Pagina,
            TamanhoPagina = req.Tamanho
        };

        var pagina = await _catalogo.BuscarAsync(filtro, ct);
        return Ok(pagina.Mapear(r => r.ParaResumo()));
    }

    /// <summary>Detalhe de um serviço, com horários e os dados do tipo dele.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ServicoDetalheResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicoDetalheResposta>> Obter(int id, [FromQuery] double? lat, [FromQuery] double? lon,
        CancellationToken ct)
    {
        var resultado = await _catalogo.ObterAsync(id, lat, lon, ct);
        return Ok(resultado.ParaDetalhe());
    }

    /// <summary>Quantos serviços ativos há em cada categoria, e quantos estão em revisão.</summary>
    [HttpGet("resumo")]
    [ProducesResponseType<ResumoCatalogo>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumoCatalogo>> Resumo(CancellationToken ct) =>
        Ok(await _catalogo.ResumoAsync(ct));

    /// <summary>Botão "avisar que fechou". Depois de 3 avisos o item volta para a curadoria.</summary>
    [HttpPost("{id:int}/avisos-fechamento")]
    [EnableRateLimiting(Program.LimiteAnonimo)]
    [ProducesResponseType<AvisoFechamentoResposta>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvisoFechamentoResposta>> AvisarFechamento(int id, CancellationToken ct)
    {
        var servico = await _catalogo.AvisarFechamentoAsync(id, ct);
        return Accepted(new AvisoFechamentoResposta(servico.Id, servico.AvisosFechamento, servico.PrecisaRevisao));
    }
}
