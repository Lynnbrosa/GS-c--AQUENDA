using System.ComponentModel.DataAnnotations;

namespace Aquenda.Api.Contratos;

public sealed record SalaResposta(int Id, string Slug, string Nome, string Descricao, string ModeradaPor, bool Aberta,
    int MensagensUltimas24h);

public sealed record EntrarChatRequisicao
{
    /// <summary>3 a 24 caracteres. Não use nome de registro.</summary>
    [Required, StringLength(24, MinimumLength = 3)]
    public string Apelido { get; init; } = "";

    /// <summary>Precisa ser true: a pessoa leu as regras (GET /api/chat/regras).</summary>
    public bool AceitoRegras { get; init; }

    /// <summary>Precisa ser true: as salas são só para 18+.</summary>
    public bool TenhoMaisDe18 { get; init; }
}

/// <summary>O token só aparece aqui. O app guarda no aparelho e manda no cabeçalho X-Token-Chat.</summary>
public sealed record EntrarChatResposta(int ParticipanteId, string Apelido, string Token, IReadOnlyList<string> Regras);

public sealed record NovaMensagemRequisicao
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Texto { get; init; } = "";
}

public sealed record MensagemResposta(int Id, int SalaId, string Apelido, string Texto, DateTimeOffset EnviadaEm);

public sealed record DenunciaResposta(int MensagemId, int Denuncias, bool Oculta);

public sealed record MensagemModeracaoResposta(int Id, int SalaId, int ParticipanteId, string Apelido, string Texto,
    DateTimeOffset EnviadaEm, int Denuncias, bool Oculta, string? MotivoOcultacao);

public sealed record OcultarRequisicao
{
    [Required, StringLength(300, MinimumLength = 3)]
    public string Motivo { get; init; } = "";
}

public sealed record SilenciarRequisicao
{
    [Range(1, 720)]
    public int Horas { get; init; } = 24;
}

public sealed record ParticipanteModeracaoResposta(int Id, string Apelido, DateTimeOffset? SilenciadoAte, bool Banido);
