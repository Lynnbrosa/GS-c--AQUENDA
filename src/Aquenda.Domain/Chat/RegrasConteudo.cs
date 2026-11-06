using System.Text.RegularExpressions;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Privacidade;

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

    protected RegraPorPadrao(Regex padrao) => _padrao = padrao;

    protected abstract string Motivo { get; }

    public string? Violacao(string texto) => _padrao.IsMatch(texto) ? Motivo : null;
}

/// <summary>Links levam para golpe e para fora da moderação.</summary>
public sealed class RegraSemLink : RegraPorPadrao
{
    public RegraSemLink() : base(PadroesPii.Link) { }
    protected override string Motivo => "Links não são permitidos na sala. Para se encontrar, use as rodas presenciais.";
}

public sealed class RegraSemCpf : RegraPorPadrao
{
    public RegraSemCpf() : base(PadroesPii.Cpf) { }
    protected override string Motivo => "Não compartilhe CPF na sala.";
}

public sealed class RegraSemTelefone : RegraPorPadrao
{
    public RegraSemTelefone() : base(PadroesPii.Telefone) { }
    protected override string Motivo => "Não compartilhe telefone na sala. Contato pessoal fica fora do app, por segurança.";
}

public sealed class RegraSemEmail : RegraPorPadrao
{
    public RegraSemEmail() : base(PadroesPii.Email) { }
    protected override string Motivo => "Não compartilhe e-mail na sala.";
}

/// <summary>O nome de registro (nome morto) nunca é gravado: expor isso é violência contra pessoa trans.</summary>
public sealed class RegraSemNomeDeRegistro : RegraPorPadrao
{
    public RegraSemNomeDeRegistro() : base(PadroesPii.NomeDeRegistro) { }
    protected override string Motivo => "Seu nome de registro fica só com você: o Aquenda não grava nome de registro. Dá para contar a história sem ele.";
}

/// <summary>Senha, PIN e chave de acesso não vão para a sala nem para o banco.</summary>
public sealed class RegraSemSegredo : RegraPorPadrao
{
    public RegraSemSegredo() : base(PadroesPii.Segredo) { }
    protected override string Motivo => "Não compartilhe senha, PIN ou chave de acesso na sala.";
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
    /// A ordem importa: segredo e nome de registro primeiro (o motivo mais grave), e-mail antes de link
    /// (gmail.com parece link) e CPF antes de telefone. 11 dígitos seguidos podem ser os dois; nesse caso
    /// o motivo é CPF, e a mensagem é recusada do mesmo jeito.
    /// </summary>
    public static ModeradorAutomatico Padrao { get; } =
        new(new IRegraConteudo[]
        {
            new RegraSemSegredo(), new RegraSemNomeDeRegistro(), new RegraSemEmail(), new RegraSemLink(), new RegraSemCpf(), new RegraSemTelefone(),
        });

    public string? PrimeiraViolacao(string texto) =>
        _regras.Select(r => r.Violacao(texto)).FirstOrDefault(v => v is not null);

    public void Verificar(string texto)
    {
        if (PrimeiraViolacao(texto) is { } motivo)
            throw new DomainException(motivo);
    }
}
