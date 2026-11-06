using Aquenda.Api.Contratos;
using Aquenda.Application.Chat;
using Aquenda.Domain.Chat;

namespace Aquenda.Api.Mapeamentos;

public static class MapeadorChat
{
    private static DateTimeOffset Utc(DateTime d) => new(DateTime.SpecifyKind(d, DateTimeKind.Utc), TimeSpan.Zero);

    public static SalaResposta ParaResposta(this SalaResumo s) =>
        new(s.Sala.Id, s.Sala.Slug, s.Sala.Nome, s.Sala.Descricao, s.Sala.ModeradaPor, s.Sala.Aberta, s.MensagensUltimas24h);

    public static MensagemResposta ParaResposta(this MensagemChat m) =>
        new(m.Id, m.SalaId, m.Participante.Apelido, m.Texto, Utc(m.EnviadaEmUtc));

    public static MensagemModeracaoResposta ParaModeracao(this MensagemChat m) =>
        new(m.Id, m.SalaId, m.ParticipanteId, m.Participante.Apelido, m.Texto, Utc(m.EnviadaEmUtc),
            m.Denuncias.Count, m.Oculta, m.MotivoOcultacao);

    public static ParticipanteModeracaoResposta ParaModeracao(this ParticipanteChat p) =>
        new(p.Id, p.Apelido, p.SilenciadoAteUtc is DateTime d ? Utc(d) : null, p.Banido);
}
