using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Roda de conversa ou evento de uma ONG parceira (v1 da aba "rodas").
/// Não é chat: o app só divulga o encontro e o link do grupo que a ONG já modera.
/// </summary>
public class RodaConversa : Entidade
{
    public const int IdadeMinimaOnline = 18;

    public string Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string Organizador { get; private set; } = null!;
    public DateTime InicioUtc { get; private set; }
    public int DuracaoMinutos { get; private set; }
    public bool Presencial { get; private set; }
    public int? LocalId { get; private set; }
    public Servico? Local { get; private set; }
    public string? LinkGrupo { get; private set; }
    public int IdadeMinima { get; private set; }

    /// <summary>Marcada como "exemplo" no protótipo, para ninguém ir a um encontro que não existe.</summary>
    public bool Ilustrativa { get; private set; }

    protected RodaConversa() { }

    public RodaConversa(string titulo, string descricao, string organizador, DateTime inicioUtc,
        int duracaoMinutos, bool presencial, int? localId, string? linkGrupo, int idadeMinima,
        bool ilustrativa, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("Título é obrigatório.");
        if (string.IsNullOrWhiteSpace(organizador)) throw new DomainException("Organizador é obrigatório.");
        if (inicioUtc <= agoraUtc) throw new DomainException("A roda precisa começar no futuro.");
        if (duracaoMinutos is < 15 or > 600) throw new DomainException("Duração entre 15 e 600 minutos.");
        if (presencial && localId is null) throw new DomainException("Roda presencial precisa de um local do catálogo.");
        if (!presencial && string.IsNullOrWhiteSpace(linkGrupo))
            throw new DomainException("Roda online precisa do link do grupo moderado.");
        if (!presencial && idadeMinima < IdadeMinimaOnline)
            throw new DomainException($"Roda online é só para {IdadeMinimaOnline}+.");
        if (idadeMinima is < 0 or > 100) throw new DomainException("Idade mínima inválida.");
        if (linkGrupo is not null && !LinkHttpsValido(linkGrupo))
            throw new DomainException("Link do grupo deve ser uma URL https.");

        Titulo = titulo.Trim();
        Descricao = descricao?.Trim() ?? string.Empty;
        Organizador = organizador.Trim();
        InicioUtc = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
        DuracaoMinutos = duracaoMinutos;
        Presencial = presencial;
        LocalId = localId;
        LinkGrupo = linkGrupo;
        IdadeMinima = idadeMinima;
        Ilustrativa = ilustrativa;
    }

    private static bool LinkHttpsValido(string link) =>
        Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    public DateTime FimUtc => InicioUtc.AddMinutes(DuracaoMinutos);
    public bool AconteceEntre(DateTime deUtc, DateTime ateUtc) => FimUtc > deUtc && InicioUtc < ateUtc;
}
