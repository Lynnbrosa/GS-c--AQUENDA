namespace Aquenda.Domain.Chat;

public interface IChatRepository
{
    Task<IReadOnlyList<SalaChat>> ListarSalasAsync(CancellationToken ct = default);
    Task<SalaChat?> ObterSalaAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> ContarMensagensVisiveisPorSalaDesdeAsync(DateTime desdeUtc, CancellationToken ct = default);
    Task<bool> ExisteSalaAsync(CancellationToken ct = default);
    Task AdicionarSalaAsync(SalaChat sala, CancellationToken ct = default);

    Task<ParticipanteChat?> ObterParticipantePorTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<ParticipanteChat?> ObterParticipanteAsync(int id, CancellationToken ct = default);
    Task<bool> ApelidoEmUsoAsync(string apelidoNormalizado, CancellationToken ct = default);
    Task AdicionarParticipanteAsync(ParticipanteChat participante, CancellationToken ct = default);

    /// <summary>Carrega a mensagem com autor e denúncias, pronta para ser alterada.</summary>
    Task<MensagemChat?> ObterMensagemAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<MensagemChat>> ListarMensagensVisiveisAsync(int salaId, int? antesDeId, int limite, CancellationToken ct = default);
    Task<IReadOnlyList<MensagemChat>> ListarAguardandoModeracaoAsync(CancellationToken ct = default);
    /// <summary>Mensagens do participante depois de <paramref name="desdeUtc"/> (sem incluir o instante).</summary>
    Task<int> ContarMensagensDoParticipanteDesdeAsync(int participanteId, DateTime desdeUtc, CancellationToken ct = default);
    Task AdicionarMensagemAsync(MensagemChat mensagem, CancellationToken ct = default);

    /// <summary>Apaga de vez mensagens (e denúncias) mais velhas que o limite. Devolve quantas.</summary>
    Task<int> ApagarMensagensAnterioresAsync(DateTime limiteUtc, CancellationToken ct = default);
}
