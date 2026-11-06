using Aquenda.Api.Contratos;
using Aquenda.Api.Infra;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Catalogo;
using Aquenda.Application.Curadoria;
using Aquenda.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Aquenda.Api.Controllers;

/// <summary>Curadoria do catálogo pela equipe e ONGs parceiras. Todas as rotas exigem X-Chave-Operador.</summary>
[ApiController]
[Route("api/curadoria")]
[Produces("application/json")]
[ChaveOperador]
public class CuradoriaController : ControllerBase
{
    private readonly ICuradoriaService _curadoria;
    private readonly IIngestaoOsmService _ingestao;

    public CuradoriaController(ICuradoriaService curadoria, IIngestaoOsmService ingestao)
    {
        _curadoria = curadoria;
        _ingestao = ingestao;
    }

    /// <summary>Cadastra um serviço. A categoria decide qual tipo é criado.</summary>
    [HttpPost("servicos")]
    [ProducesResponseType<ServicoDetalheResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ServicoDetalheResposta>> Criar([FromBody] NovoServicoRequisicao req, CancellationToken ct)
    {
        var servico = await _curadoria.CriarAsync(new NovoServico
        {
            Categoria = req.Categoria!.Value,
            Nome = req.Nome,
            Descricao = req.Descricao,
            Logradouro = req.Logradouro,
            Bairro = req.Bairro,
            Cidade = req.Cidade,
            Cep = req.Cep,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            EnderecoSigiloso = req.EnderecoSigiloso,
            Telefone = req.Telefone,
            Site = req.Site,
            Fonte = req.Fonte,
            VerificadoEm = req.VerificadoEm,
            Atendimentos = (req.Atendimentos ?? Array.Empty<Atendimento>()).Aggregate(Atendimento.Nenhum, (a, b) => a | b),
            InstrucaoAcesso = req.InstrucaoAcesso,
            Horarios = (req.Horarios ?? Array.Empty<HorarioRequisicao>())
                .Select(h => new NovoHorario(h.Dia!.Value, TimeOnly.Parse(h.Abre), TimeOnly.Parse(h.Fecha))).ToList(),
            PublicoAlvo = req.PublicoAlvo,
            ExigeEncaminhamento = req.ExigeEncaminhamento,
            CapacidadeDeclarada = req.CapacidadeDeclarada,
            InscreveTranscidadania = req.InscreveTranscidadania,
            CodigoCnes = req.CodigoCnes,
            TipoAtencao = req.TipoAtencao,
            RedeSampaTrans = req.RedeSampaTrans,
            HabilitadaProcessoTransexualizador = req.HabilitadaProcessoTransexualizador,
            Orgao = req.Orgao,
            AtendeRetificacao = req.AtendeRetificacao,
            RecebeDenuncia = req.RecebeDenuncia,
            TipoRole = req.TipoRole,
            FaixaPreco = req.FaixaPreco,
            Acessivel = req.Acessivel
        }, ct);

        var detalhe = new ResultadoBusca(servico, null, null).ParaDetalhe();
        return CreatedAtAction(nameof(ServicosController.Obter), "Servicos", new { id = servico.Id }, detalhe);
    }

    /// <summary>Fila de revisão: itens com 3+ avisos de fechamento ou verificados há mais de 180 dias.</summary>
    [HttpGet("revisao")]
    [ProducesResponseType<IReadOnlyList<ItemRevisaoResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemRevisaoResposta>>> Revisao(CancellationToken ct) =>
        Ok((await _curadoria.FilaRevisaoAsync(ct))
            .Select(i => new ItemRevisaoResposta(new ResultadoBusca(i.Servico, null, null).ParaResumo(), i.Motivo, i.Servico.Ativo))
            .ToList());

    /// <summary>Conferiu o serviço: atualiza a data de verificação e zera os avisos.</summary>
    [HttpPost("servicos/{id:int}/verificacao")]
    [ProducesResponseType<ServicoResumoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicoResumoResposta>> Verificar(int id, [FromBody] VerificacaoRequisicao? req, CancellationToken ct) =>
        Ok(new ResultadoBusca(await _curadoria.MarcarVerificadoAsync(id, req?.Data, ct), null, null).ParaResumo());

    /// <summary>Tira o serviço do catálogo (fechou de vez). Nada é apagado.</summary>
    [HttpDelete("servicos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        await _curadoria.DesativarAsync(id, ct);
        return NoContent();
    }

    [HttpPost("servicos/{id:int}/reativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reativar(int id, CancellationToken ct)
    {
        await _curadoria.ReativarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Importa rolês do OpenStreetMap (tag lgbtq) para o catálogo. Reimportar atualiza sem duplicar.</summary>
    [HttpPost("ingestao/osm")]
    [ProducesResponseType<ResultadoIngestao>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ResultadoIngestao>> ImportarOsm(CancellationToken ct) =>
        Ok(await _ingestao.ImportarRolesAsync(ct));
}
