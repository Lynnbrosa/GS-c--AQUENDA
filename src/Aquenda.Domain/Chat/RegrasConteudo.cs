using System.Text.RegularExpressions;
using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Chat;

/// <summary>Uma regra que o texto de uma mensagem precisa respeitar.</summary>
public interface IRegraConteudo
{
    /// <summary>Devolve o motivo da recusa, ou null se o texto passou.</summary>
    string? Violacao(string texto);
}

/// <summary>Base das regras por expressão regular: cada filha só informa o padrão e a mensagem.</summary>
public abstract class RegraPorPadrao : IRegraConteudo
{
    private readonly Regex _padrao;

    protected RegraPorPadrao(string padrao) =>
        _padrao = new Regex(padrao, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    protected abstract string Motivo { get; }

    public string? Violacao(string texto) => _padrao.IsMatch(texto) ? Motivo : null;
}

/// <summary>Links levam para golpe e para fora da moderação.</summary>
public sealed class RegraSemLink : RegraPorPadrao
{
    public RegraSemLink() : base(@"(https?://|www\.|\b[\w-]+\.(com|net|org|br|io|me|app|ly|gg|link|site|xyz)\b)") { }
    protected override string Motivo => "Links não são permitidos na sala. Para se encontrar, use as rodas presenciais.";
}

public sealed class RegraSemCpf : RegraPorPadrao
{
    public RegraSemCpf() : base(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b") { }
    protected override string Motivo => "Não compartilhe CPF na sala.";
}

public sealed class RegraSemTelefone : RegraPorPadrao
{
    public RegraSemTelefone() : base(@"(\+?55[\s-]?)?\(?\b\d{2}\)?[\s-]?9?\d{4}[\s.-]?\d{4}\b") { }
    protected override string Motivo => "Não compartilhe telefone na sala. Contato pessoal fica fora do app, por segurança.";
}

public sealed class RegraSemEmail : RegraPorPadrao
{
    public RegraSemEmail() : base(@"[\w.+-]+@[\w-]+\.[\w.]+") { }
    protected override string Motivo => "Não compartilhe e-mail na sala.";
}

/// <summary>
/// Aplica as regras em ordem e recusa na primeira violação. É a moderação automática;
/// a humana (ONG) vem depois, pelas denúncias.
/// </summary>
public sealed class ModeradorAutomatico
{
    private readonly IReadOnlyList<IRegraConteudo> _regras;

    public ModeradorAutomatico(IEnumerable<IRegraConteudo> regras) => _regras = regras.ToList();

    /// <summary>
    /// A ordem importa: e-mail antes de link (gmail.com parece link) e CPF antes de telefone.
    /// 11 dígitos seguidos podem ser os dois; nesse caso o motivo é CPF, e a mensagem é recusada do mesmo jeito.
    /// </summary>
    public static ModeradorAutomatico Padrao { get; } =
        new(new IRegraConteudo[] { new RegraSemEmail(), new RegraSemLink(), new RegraSemCpf(), new RegraSemTelefone() });

    public string? PrimeiraViolacao(string texto) =>
        _regras.Select(r => r.Violacao(texto)).FirstOrDefault(v => v is not null);

    public void Verificar(string texto)
    {
        if (PrimeiraViolacao(texto) is { } motivo)
            throw new DomainException(motivo);
    }
}
