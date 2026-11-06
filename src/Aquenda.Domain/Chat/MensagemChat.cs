using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Chat;

/// <summary>
/// Mensagem numa sala. Com <see cref="DenunciasParaOcultar"/> denúncias de pessoas diferentes
/// ela some na hora e espera a ONG revisar.
/// </summary>
public class MensagemChat : Entidade
{
    public const int DenunciasParaOcultar = 3;

    private readonly List<DenunciaMensagem> _denuncias = new();

    public int SalaId { get; private set; }
    public int ParticipanteId { get; private set; }
    public ParticipanteChat Participante { get; private set; } = null!;
    public string Texto { get; private set; } = null!;
    public DateTime EnviadaEmUtc { get; private set; }
    public bool Oculta { get; private set; }
    public string? MotivoOcultacao { get; private set; }
    public DateTime? RevisadaEmUtc { get; private set; }

    public IReadOnlyCollection<DenunciaMensagem> Denuncias => _denuncias.AsReadOnly();

    protected MensagemChat() { }

    internal MensagemChat(SalaChat sala, ParticipanteChat autor, string texto, DateTime agoraUtc)
    {
        SalaId = sala.Id;
        ParticipanteId = autor.Id;
        Participante = autor;
        Texto = texto;
        EnviadaEmUtc = agoraUtc;
    }

    /// <summary>Registra uma denúncia. Devolve true se esta denúncia fez a mensagem sumir.</summary>
    public bool Denunciar(ParticipanteChat denunciante, DateTime agoraUtc)
    {
        if (denunciante.Id == ParticipanteId)
            throw new DomainException("Não dá para denunciar a própria mensagem.");
        if (_denuncias.Any(d => d.ParticipanteId == denunciante.Id))
            throw new DomainException("Você já denunciou esta mensagem.");

        _denuncias.Add(new DenunciaMensagem(denunciante.Id, agoraUtc));

        if (Oculta || _denuncias.Count < DenunciasParaOcultar)
            return false;
        Oculta = true;
        MotivoOcultacao = "Ocultada por denúncias, aguardando a moderação.";
        return true;
    }

    public bool AguardandoModeracao => RevisadaEmUtc is null && (Oculta || _denuncias.Count > 0);

    /// <summary>A ONG confirmou que a mensagem viola as regras.</summary>
    public void Ocultar(string motivo, DateTime agoraUtc)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new DomainException("Informe o motivo.");
        Oculta = true;
        MotivoOcultacao = motivo.Trim();
        RevisadaEmUtc = agoraUtc;
    }

    /// <summary>A ONG revisou e a mensagem pode voltar. As denúncias antigas são arquivadas.</summary>
    public void Restaurar(DateTime agoraUtc)
    {
        Oculta = false;
        MotivoOcultacao = null;
        RevisadaEmUtc = agoraUtc;
        _denuncias.Clear();
    }
}

public class DenunciaMensagem : Entidade
{
    public int MensagemId { get; private set; }
    public int ParticipanteId { get; private set; }
    public DateTime CriadaEmUtc { get; private set; }

    protected DenunciaMensagem() { }

    internal DenunciaMensagem(int participanteId, DateTime agoraUtc)
    {
        ParticipanteId = participanteId;
        CriadaEmUtc = agoraUtc;
    }
}
