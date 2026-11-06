using Aquenda.Api.Contratos;
using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aquenda.Api.Controllers;

/// <summary>
/// Salas de conversa temáticas, moderadas por ONG. Só apelido, só 18+, sem conversa privada.
/// Tempo real pelo SignalR em /hubs/chat.
/// </summary>
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    public const string CabecalhoToken = "X-Token-Chat";

    private readonly IChatService _chat;

    public ChatController(IChatService chat) => _chat = chat;

    /// <summary>Regras das salas, para mostrar antes de entrar.</summary>
    [HttpGet("regras")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> Regras() => Ok(_chat.Regras);

    /// <summary>Salas e quantas mensagens tiveram nas últimas 24h.</summary>
    [HttpGet("salas")]
    [ProducesResponseType<IReadOnlyList<SalaResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalaResposta>>> Salas(CancellationToken ct) =>
        Ok((await _chat.ListarSalasAsync(ct)).Select(s => s.ParaResposta()).ToList());

    /// <summary>Entra nas salas com um apelido. Devolve o token, que só é mostrado esta vez.</summary>
    [HttpPost("participantes")]
    [EnableRateLimiting(Program.LimiteAnonimo)]
    [ProducesResponseType<EntrarChatResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EntrarChatResposta>> Entrar([FromBody] EntrarChatRequisicao req, CancellationToken ct)
    {
        var entrada = await _chat.EntrarAsync(req.Apelido, req.AceitoRegras, req.TenhoMaisDe18, ct);
        return StatusCode(StatusCodes.Status201Created,
            new EntrarChatResposta(entrada.Participante.Id, entrada.Participante.Apelido, entrada.Token, _chat.Regras));
    }

    /// <summary>Mensagens visíveis da sala, da mais antiga para a mais nova. Use antesDe para carregar as anteriores.</summary>
    [HttpGet("salas/{salaId:int}/mensagens")]
    [ProducesResponseType<IReadOnlyList<MensagemResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MensagemResposta>>> Mensagens(int salaId, [FromQuery] int? antesDe,
        [FromQuery] int limite = 50, CancellationToken ct = default) =>
        Ok((await _chat.ListarMensagensAsync(salaId, antesDe, limite, ct)).Select(m => m.ParaResposta()).ToList());

    /// <summary>Envia mensagem. Link, telefone, e-mail e CPF são recusados; limite de 8 por minuto.</summary>
    [HttpPost("salas/{salaId:int}/mensagens")]
    [EnableRateLimiting(Program.LimiteAnonimo)]
    [ProducesResponseType<MensagemResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MensagemResposta>> Enviar(int salaId, [FromBody] NovaMensagemRequisicao req,
        [FromHeader(Name = CabecalhoToken)] string? token, CancellationToken ct)
    {
        var mensagem = await _chat.EnviarAsync(token, salaId, req.Texto, ct);
        return StatusCode(StatusCodes.Status201Created, mensagem.ParaResposta());
    }

    /// <summary>Denuncia em 1 toque. Com 3 denúncias de pessoas diferentes a mensagem some até a ONG revisar.</summary>
    [HttpPost("mensagens/{mensagemId:int}/denuncias")]
    [EnableRateLimiting(Program.LimiteAnonimo)]
    [ProducesResponseType<DenunciaResposta>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DenunciaResposta>> Denunciar(int mensagemId,
        [FromHeader(Name = CabecalhoToken)] string? token, CancellationToken ct)
    {
        var r = await _chat.DenunciarAsync(token, mensagemId, ct);
        return Accepted(new DenunciaResposta(r.MensagemId, r.Denuncias, r.Oculta));
    }
}
