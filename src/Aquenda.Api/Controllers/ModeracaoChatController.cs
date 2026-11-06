using Aquenda.Api.Contratos;
using Aquenda.Api.Infra;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Chat;
using Microsoft.AspNetCore.Mvc;

namespace Aquenda.Api.Controllers;

/// <summary>Painel da ONG moderadora. Todas as rotas exigem X-Chave-Operador.</summary>
[ApiController]
[Route("api/chat/moderacao")]
[Produces("application/json")]
[ChaveOperador]
public class ModeracaoChatController : ControllerBase
{
    private readonly IChatService _chat;

    public ModeracaoChatController(IChatService chat) => _chat = chat;

    /// <summary>Mensagens denunciadas ou ocultadas que ainda não foram revisadas.</summary>
    [HttpGet("fila")]
    [ProducesResponseType<IReadOnlyList<MensagemModeracaoResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MensagemModeracaoResposta>>> Fila(CancellationToken ct) =>
        Ok((await _chat.FilaModeracaoAsync(ct)).Select(m => m.ParaModeracao()).ToList());

    [HttpPost("mensagens/{id:int}/ocultar")]
    [ProducesResponseType<MensagemModeracaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MensagemModeracaoResposta>> Ocultar(int id, [FromBody] OcultarRequisicao req, CancellationToken ct) =>
        Ok((await _chat.OcultarAsync(id, req.Motivo, ct)).ParaModeracao());

    [HttpPost("mensagens/{id:int}/restaurar")]
    [ProducesResponseType<MensagemModeracaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MensagemModeracaoResposta>> Restaurar(int id, CancellationToken ct) =>
        Ok((await _chat.RestaurarAsync(id, ct)).ParaModeracao());

    [HttpPost("participantes/{id:int}/silenciar")]
    [ProducesResponseType<ParticipanteModeracaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipanteModeracaoResposta>> Silenciar(int id, [FromBody] SilenciarRequisicao req,
        CancellationToken ct) =>
        Ok((await _chat.SilenciarAsync(id, req.Horas, ct)).ParaModeracao());

    /// <summary>Tira o apelido das salas de vez. As mensagens dele somem das listas.</summary>
    [HttpPost("participantes/{id:int}/banir")]
    [ProducesResponseType<ParticipanteModeracaoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipanteModeracaoResposta>> Banir(int id, CancellationToken ct) =>
        Ok((await _chat.BanirAsync(id, ct)).ParaModeracao());

    [HttpPost("salas/{id:int}/fechar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Fechar(int id, CancellationToken ct)
    {
        await _chat.DefinirSalaAbertaAsync(id, false, ct);
        return NoContent();
    }

    [HttpPost("salas/{id:int}/reabrir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reabrir(int id, CancellationToken ct)
    {
        await _chat.DefinirSalaAbertaAsync(id, true, ct);
        return NoContent();
    }
}
