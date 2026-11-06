using Aquenda.Api.Mapeamentos;
using Aquenda.Application.Chat;
using Aquenda.Domain.Chat;
using Microsoft.AspNetCore.SignalR;

namespace Aquenda.Api.Hubs;

public sealed class NotificadorSignalR : INotificadorChat
{
    private readonly IHubContext<ChatHub> _hub;

    public NotificadorSignalR(IHubContext<ChatHub> hub) => _hub = hub;

    public Task MensagemNovaAsync(MensagemChat mensagem, CancellationToken ct = default) =>
        _hub.Clients.Group(ChatHub.Grupo(mensagem.SalaId)).SendAsync("mensagem", mensagem.ParaResposta(), ct);

    public Task MensagemRemovidaAsync(int salaId, int mensagemId, CancellationToken ct = default) =>
        _hub.Clients.Group(ChatHub.Grupo(salaId)).SendAsync("mensagemRemovida", new { salaId, mensagemId }, ct);

    public Task MensagemRestauradaAsync(MensagemChat mensagem, CancellationToken ct = default) =>
        _hub.Clients.Group(ChatHub.Grupo(mensagem.SalaId)).SendAsync("mensagemRestaurada", mensagem.ParaResposta(), ct);
}
