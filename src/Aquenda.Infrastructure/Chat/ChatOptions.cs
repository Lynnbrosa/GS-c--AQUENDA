namespace Aquenda.Infrastructure.Chat;

/// <summary>Seção "Chat" do appsettings.</summary>
public sealed class ChatOptions
{
    public const string Secao = "Chat";

    /// <summary>Depois de quantos dias as mensagens são apagadas de vez.</summary>
    public int DiasRetencao { get; set; } = 7;

    public int IntervaloLimpezaMinutos { get; set; } = 60;
}
