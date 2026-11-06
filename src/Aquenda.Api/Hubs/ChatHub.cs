using Microsoft.AspNetCore.SignalR;

namespace Aquenda.Api.Hubs;

/// <summary>
/// Tempo real das salas. O app conecta em /hubs/chat, chama EntrarNaSala(salaId) e passa a
/// receber os eventos "mensagem", "mensagemRemovida" e "mensagemRestaurada".
/// Enviar continua sendo pelo POST, que passa por todas as validações.
/// </summary>
public sealed class ChatHub : Hub
{
    public static string Grupo(int salaId) => $"sala-{salaId}";

    public Task EntrarNaSala(int salaId) => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(salaId));

    public Task SairDaSala(int salaId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(salaId));
}
