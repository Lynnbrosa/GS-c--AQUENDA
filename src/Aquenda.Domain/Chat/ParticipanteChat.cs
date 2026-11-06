using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Chat;

/// <summary>
/// Quem fala nas salas. Só tem apelido: sem foto, nome, telefone ou e-mail.
/// O acesso é por um token aleatório; o banco guarda só o hash dele.
/// </summary>
public class ParticipanteChat : Entidade
{
    public const int IdadeMinima = 18;
    private static readonly Regex ApelidoValido = new(@"^[\p{L}\p{N}_ .-]{3,24}$", RegexOptions.CultureInvariant);
    private static readonly string[] Reservados = { "MODERACAO", "MODERAÇÃO", "ADMIN", "ONG", "AQUENDA", "SUPORTE" };

    public string Apelido { get; private set; } = null!;
    public string ApelidoNormalizado { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime? SilenciadoAteUtc { get; private set; }
    public bool Banido { get; private set; }

    protected ParticipanteChat() { }

    private ParticipanteChat(string apelido, string tokenHash, DateTime agoraUtc)
    {
        Apelido = apelido;
        ApelidoNormalizado = Normalizar(apelido);
        TokenHash = tokenHash;
        CriadoEmUtc = agoraUtc;
    }

    /// <summary>
    /// Cria o participante e devolve o token em texto puro. Ele só é mostrado aqui:
    /// o app guarda no aparelho e o servidor nunca mais vê o valor original.
    /// </summary>
    public static (ParticipanteChat Participante, string Token) Criar(string apelido, bool aceitouRegras,
        bool declarouMaioridade, DateTime agoraUtc)
    {
        if (!aceitouRegras)
            throw new DomainException("Para entrar é preciso aceitar as regras das salas.");
        if (!declarouMaioridade)
            throw new DomainException($"As salas são só para maiores de {IdadeMinima} anos.");

        apelido = (apelido ?? string.Empty).Trim();
        if (!ApelidoValido.IsMatch(apelido))
            throw new DomainException("Apelido com 3 a 24 caracteres: letras, números, espaço, ponto, hífen ou _.");
        if (apelido.Count(char.IsDigit) >= 6)
            throw new DomainException("Apelido não pode parecer telefone ou documento.");
        if (Reservados.Contains(Normalizar(apelido)))
            throw new DomainException("Esse apelido é reservado.");
        ModeradorAutomatico.Padrao.Verificar(apelido);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (new ParticipanteChat(apelido, HashToken(token), agoraUtc), token);
    }

    public static string Normalizar(string apelido) => apelido.Trim().ToUpperInvariant();

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));

    public bool EstaSilenciado(DateTime agoraUtc) => SilenciadoAteUtc is { } ate && ate > agoraUtc;

    public void VerificarPodePublicar(DateTime agoraUtc)
    {
        if (Banido)
            throw new DomainException("Este apelido foi removido das salas pela moderação.");
        if (EstaSilenciado(agoraUtc))
            throw new DomainException($"Você está silenciade pela moderação até {FusoSaoPaulo.ParaLocal(SilenciadoAteUtc!.Value):dd/MM HH:mm}.");
    }

    public void Silenciar(TimeSpan duracao, DateTime agoraUtc)
    {
        if (duracao <= TimeSpan.Zero || duracao > TimeSpan.FromDays(30))
            throw new DomainException("Silêncio entre 1 hora e 30 dias.");
        SilenciadoAteUtc = agoraUtc + duracao;
    }

    public void Banir() => Banido = true;
}
