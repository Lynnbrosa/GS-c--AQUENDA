using Aquenda.Application.Chat;
using Aquenda.Application.Comum;
using Microsoft.AspNetCore.SignalR;

namespace Aquenda.Api.Hubs;

/// <summary>
/// Tempo real das salas. O app conecta em /hubs/chat, chama EntrarNaSala(salaId, token) e passa a
/// receber os eventos "mensagem", "mensagemRemovida" e "mensagemRestaurada".
/// Sem token de participante válido, a conexão não entra no grupo e não recebe nada.
/// Enviar continua sendo pelo POST, que passa por todas as validações.
/// </summary>
public sealed class ChatHub : Hub
{
    private readonly IChatService _chat;

    public ChatHub(IChatService chat) => _chat = chat;

    public static string Grupo(int salaId) => $"sala-{salaId}";

    public async Task EntrarNaSala(int salaId, string? token)
    {
        try
        {
            await _chat.AutorizarLeituraAsync(token, salaId, Context.ConnectionAborted);
        }
        catch (NaoAutorizadoException e)
        {
            throw new HubException(e.Message);
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(salaId));
    }

    public Task SairDaSala(int salaId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(salaId));
}
