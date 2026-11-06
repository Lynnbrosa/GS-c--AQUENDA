using Aquenda.Application.Comum;
using Aquenda.Domain.Chat;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Repositorios;

namespace Aquenda.Application.Chat;

/// <summary>Avisa quem está com a sala aberta. A API implementa com SignalR.</summary>
public interface INotificadorChat
{
    Task MensagemNovaAsync(MensagemChat mensagem, CancellationToken ct = default);
    Task MensagemRemovidaAsync(int salaId, int mensagemId, CancellationToken ct = default);
    Task MensagemRestauradaAsync(MensagemChat mensagem, CancellationToken ct = default);
}

public sealed record SalaResumo(SalaChat Sala, int MensagensUltimas24h);
public sealed record EntradaChat(ParticipanteChat Participante, string Token);
public sealed record ResultadoDenuncia(int MensagemId, int Denuncias, bool Oculta);

public interface IChatService
{
    IReadOnlyList<string> Regras { get; }
    Task<IReadOnlyList<SalaResumo>> ListarSalasAsync(CancellationToken ct = default);
    Task<EntradaChat> EntrarAsync(string apelido, bool aceitouRegras, bool declarouMaioridade, CancellationToken ct = default);
    Task<ParticipanteChat> AutenticarAsync(string? token, CancellationToken ct = default);
    /// <summary>Só quem entrou nas salas lê: as mensagens podem falar de saúde e transição.</summary>
    Task<IReadOnlyList<MensagemChat>> ListarMensagensAsync(string? token, int salaId, int? antesDeId, int limite, CancellationToken ct = default);
    /// <summary>Confere o token antes de colocar a conexão no grupo da sala (tempo real).</summary>
    Task AutorizarLeituraAsync(string? token, int salaId, CancellationToken ct = default);
    Task<MensagemChat> EnviarAsync(string? token, int salaId, string texto, CancellationToken ct = default);
    Task<ResultadoDenuncia> DenunciarAsync(string? token, int mensagemId, CancellationToken ct = default);

    // Moderação (ONG)
    Task<IReadOnlyList<MensagemChat>> FilaModeracaoAsync(CancellationToken ct = default);
    Task<MensagemChat> OcultarAsync(int mensagemId, string motivo, CancellationToken ct = default);
    Task<MensagemChat> RestaurarAsync(int mensagemId, CancellationToken ct = default);
    Task<ParticipanteChat> SilenciarAsync(int participanteId, int horas, CancellationToken ct = default);
    Task<ParticipanteChat> BanirAsync(int participanteId, CancellationToken ct = default);
    Task<SalaChat> DefinirSalaAbertaAsync(int salaId, bool aberta, CancellationToken ct = default);
}

public sealed class ChatService : IChatService
{
    public const int MensagensPorMinuto = 8;
    public const int LimiteMaximoPagina = 100;

    private readonly IChatRepository _chat;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;
    private readonly INotificadorChat _notificador;
    private readonly ModeradorAutomatico _moderador;

    public ChatService(IChatRepository chat, IUnitOfWork uow, IRelogio relogio, INotificadorChat notificador,
        ModeradorAutomatico? moderador = null)
    {
        _chat = chat;
        _uow = uow;
        _relogio = relogio;
        _notificador = notificador;
        _moderador = moderador ?? ModeradorAutomatico.Padrao;
    }

    public IReadOnlyList<string> Regras { get; } = new[]
    {
        $"As salas são só para maiores de {ParticipanteChat.IdadeMinima} anos.",
        "Use só um apelido. Não mande foto, nome de registro, telefone, e-mail, CPF ou link.",
        "Não existe conversa privada. Para se encontrar, use as rodas presenciais das ONGs.",
        $"Viu algo errado? Denuncie a mensagem. Com {MensagemChat.DenunciasParaOcultar} denúncias ela some até a ONG revisar.",
        "Respeito é a regra. LGBTfobia, assédio e golpe tiram o apelido das salas.",
        "As mensagens são apagadas automaticamente depois de alguns dias."
    };

    public async Task<IReadOnlyList<SalaResumo>> ListarSalasAsync(CancellationToken ct = default)
    {
        var salas = await _chat.ListarSalasAsync(ct);
        var atividade = await _chat.ContarMensagensVisiveisPorSalaDesdeAsync(_relogio.AgoraUtc.AddHours(-24), ct);
        return salas.Select(s => new SalaResumo(s, atividade.GetValueOrDefault(s.Id))).ToList();
    }

    public async Task<EntradaChat> EntrarAsync(string apelido, bool aceitouRegras, bool declarouMaioridade, CancellationToken ct = default)
    {
        var (participante, token) = ParticipanteChat.Criar(apelido, aceitouRegras, declarouMaioridade, _relogio.AgoraUtc);
        if (await _chat.ApelidoEmUsoAsync(participante.ApelidoNormalizado, ct))
            throw new DomainException("Esse apelido já está em uso. Tente outro.");

        await _chat.AdicionarParticipanteAsync(participante, ct);
        await _uow.SalvarAsync(ct);
        return new EntradaChat(participante, token);
    }

    public async Task<ParticipanteChat> AutenticarAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new NaoAutorizadoException("Entre nas salas primeiro (POST /api/chat/participantes) e mande o token.");
        return await _chat.ObterParticipantePorTokenHashAsync(ParticipanteChat.HashToken(token), ct)
            ?? throw new NaoAutorizadoException("Token do chat inválido.");
    }

    public async Task<IReadOnlyList<MensagemChat>> ListarMensagensAsync(string? token, int salaId, int? antesDeId, int limite, CancellationToken ct = default)
    {
        if (limite is < 1 or > LimiteMaximoPagina)
            throw new ValidacaoException($"Limite entre 1 e {LimiteMaximoPagina}.");
        await AutorizarLeituraAsync(token, salaId, ct);
        return await _chat.ListarMensagensVisiveisAsync(salaId, antesDeId, limite, ct);
    }

    public async Task AutorizarLeituraAsync(string? token, int salaId, CancellationToken ct = default)
    {
        await AutenticarAsync(token, ct);
        await ObterSalaAsync(salaId, ct);
    }

    public async Task<MensagemChat> EnviarAsync(string? token, int salaId, string texto, CancellationToken ct = default)
    {
        var autor = await AutenticarAsync(token, ct);
        var sala = await ObterSalaAsync(salaId, ct);
        var agora = _relogio.AgoraUtc;

        var recentes = await _chat.ContarMensagensDoParticipanteDesdeAsync(autor.Id, agora.AddMinutes(-1), ct);
        if (recentes >= MensagensPorMinuto)
            throw new DomainException("Calma: muitas mensagens em seguida. Espere um minuto.");

        var mensagem = sala.Publicar(autor, texto, agora, _moderador);
        await _chat.AdicionarMensagemAsync(mensagem, ct);
        await _uow.SalvarAsync(ct);
        await _notificador.MensagemNovaAsync(mensagem, ct);
        return mensagem;
    }

    public async Task<ResultadoDenuncia> DenunciarAsync(string? token, int mensagemId, CancellationToken ct = default)
    {
        var denunciante = await AutenticarAsync(token, ct);
        var mensagem = await ObterMensagemAsync(mensagemId, ct);
        if (mensagem.Oculta)
            return new ResultadoDenuncia(mensagem.Id, mensagem.Denuncias.Count, true);

        var sumiu = mensagem.Denunciar(denunciante, _relogio.AgoraUtc);
        await _uow.SalvarAsync(ct);
        if (sumiu)
            await _notificador.MensagemRemovidaAsync(mensagem.SalaId, mensagem.Id, ct);
        return new ResultadoDenuncia(mensagem.Id, mensagem.Denuncias.Count, mensagem.Oculta);
    }

    public Task<IReadOnlyList<MensagemChat>> FilaModeracaoAsync(CancellationToken ct = default) =>
        _chat.ListarAguardandoModeracaoAsync(ct);

    public async Task<MensagemChat> OcultarAsync(int mensagemId, string motivo, CancellationToken ct = default)
    {
        var mensagem = await ObterMensagemAsync(mensagemId, ct);
        var estavaVisivel = !mensagem.Oculta;
        mensagem.Ocultar(motivo, _relogio.AgoraUtc);
        await _uow.SalvarAsync(ct);
        if (estavaVisivel)
            await _notificador.MensagemRemovidaAsync(mensagem.SalaId, mensagem.Id, ct);
        return mensagem;
    }

    public async Task<MensagemChat> RestaurarAsync(int mensagemId, CancellationToken ct = default)
    {
        var mensagem = await ObterMensagemAsync(mensagemId, ct);
        var estavaOculta = mensagem.Oculta;
        mensagem.Restaurar(_relogio.AgoraUtc);
        await _uow.SalvarAsync(ct);
        if (estavaOculta)
            await _notificador.MensagemRestauradaAsync(mensagem, ct);
        return mensagem;
    }

    public async Task<ParticipanteChat> SilenciarAsync(int participanteId, int horas, CancellationToken ct = default)
    {
        var participante = await ObterParticipanteAsync(participanteId, ct);
        participante.Silenciar(TimeSpan.FromHours(horas), _relogio.AgoraUtc);
        await _uow.SalvarAsync(ct);
        return participante;
    }

    public async Task<ParticipanteChat> BanirAsync(int participanteId, CancellationToken ct = default)
    {
        var participante = await ObterParticipanteAsync(participanteId, ct);
        participante.Banir();
        await _uow.SalvarAsync(ct);
        return participante;
    }

    public async Task<SalaChat> DefinirSalaAbertaAsync(int salaId, bool aberta, CancellationToken ct = default)
    {
        var sala = await ObterSalaAsync(salaId, ct);
        if (aberta) sala.Reabrir(); else sala.Fechar();
        await _uow.SalvarAsync(ct);
        return sala;
    }

    private async Task<SalaChat> ObterSalaAsync(int id, CancellationToken ct) =>
        await _chat.ObterSalaAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Sala", id);

    private async Task<MensagemChat> ObterMensagemAsync(int id, CancellationToken ct) =>
        await _chat.ObterMensagemAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Mensagem", id);

    private async Task<ParticipanteChat> ObterParticipanteAsync(int id, CancellationToken ct) =>
        await _chat.ObterParticipanteAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Participante", id);
}
