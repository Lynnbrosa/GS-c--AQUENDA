using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Chat;

/// <summary>Sala temática moderada por ONG. Não existe conversa privada entre participantes.</summary>
public class SalaChat : Entidade
{
    public const int TamanhoMaximoMensagem = 500;

    public string Slug { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string ModeradaPor { get; private set; } = null!;
    public bool Aberta { get; private set; } = true;

    protected SalaChat() { }

    public SalaChat(string slug, string nome, string descricao, string moderadaPor)
    {
        if (string.IsNullOrWhiteSpace(slug) || !slug.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            throw new DomainException("Slug só com letras minúsculas, números e hífen.");
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome da sala é obrigatório.");
        if (string.IsNullOrWhiteSpace(moderadaPor)) throw new DomainException("Toda sala precisa de uma ONG moderadora.");
        Slug = slug;
        Nome = nome.Trim();
        Descricao = descricao?.Trim() ?? string.Empty;
        ModeradaPor = moderadaPor.Trim();
    }

    /// <summary>Única forma de criar mensagem: a sala confere se está aberta, quem fala e o que é dito.</summary>
    public MensagemChat Publicar(ParticipanteChat autor, string texto, DateTime agoraUtc, ModeradorAutomatico moderador)
    {
        if (!Aberta)
            throw new DomainException("Esta sala está fechada pela moderação.");
        autor.VerificarPodePublicar(agoraUtc);

        texto = (texto ?? string.Empty).Trim();
        if (texto.Length == 0)
            throw new DomainException("Mensagem vazia.");
        if (texto.Length > TamanhoMaximoMensagem)
            throw new DomainException($"Mensagem com até {TamanhoMaximoMensagem} caracteres.");
        moderador.Verificar(texto);

        return new MensagemChat(this, autor, texto, agoraUtc);
    }

    public void Fechar() => Aberta = false;
    public void Reabrir() => Aberta = true;
}
