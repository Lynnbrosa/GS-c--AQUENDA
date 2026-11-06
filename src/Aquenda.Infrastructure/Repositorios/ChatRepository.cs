using Aquenda.Domain.Chat;
using Aquenda.Infrastructure.Dados;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Repositorios;

public class ChatRepository : IChatRepository
{
    private readonly AquendaDbContext _db;

    public ChatRepository(AquendaDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalaChat>> ListarSalasAsync(CancellationToken ct = default) =>
        await _db.SalasChat.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct);

    public Task<SalaChat?> ObterSalaAsync(int id, CancellationToken ct = default) =>
        _db.SalasChat.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyDictionary<int, int>> ContarMensagensVisiveisPorSalaDesdeAsync(DateTime desdeUtc, CancellationToken ct = default) =>
        await _db.MensagensChat
            .Where(m => !m.Oculta && m.EnviadaEmUtc >= desdeUtc)
            .GroupBy(m => m.SalaId)
            .Select(g => new { SalaId = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.SalaId, x => x.Quantidade, ct);

    public Task<bool> ExisteSalaAsync(CancellationToken ct = default) => _db.SalasChat.AnyAsync(ct);

    public async Task AdicionarSalaAsync(SalaChat sala, CancellationToken ct = default) =>
        await _db.SalasChat.AddAsync(sala, ct);

    public Task<ParticipanteChat?> ObterParticipantePorTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.ParticipantesChat.FirstOrDefaultAsync(p => p.TokenHash == tokenHash, ct);

    public Task<ParticipanteChat?> ObterParticipanteAsync(int id, CancellationToken ct = default) =>
        _db.ParticipantesChat.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ApelidoEmUsoAsync(string apelidoNormalizado, CancellationToken ct = default) =>
        _db.ParticipantesChat.AnyAsync(p => p.ApelidoNormalizado == apelidoNormalizado, ct);

    public async Task AdicionarParticipanteAsync(ParticipanteChat participante, CancellationToken ct = default) =>
        await _db.ParticipantesChat.AddAsync(participante, ct);

    public Task<MensagemChat?> ObterMensagemAsync(int id, CancellationToken ct = default) =>
        _db.MensagensChat
            .Include(m => m.Participante)
            .Include(m => m.Denuncias)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<MensagemChat>> ListarMensagensVisiveisAsync(int salaId, int? antesDeId, int limite,
        CancellationToken ct = default)
    {
        var q = _db.MensagensChat.AsNoTracking()
            .Include(m => m.Participante)
            .Where(m => m.SalaId == salaId && !m.Oculta && !m.Participante.Banido);
        if (antesDeId is int antes)
            q = q.Where(m => m.Id < antes);

        // Pega as N mais novas e devolve em ordem de leitura (antiga → nova).
        var pagina = await q.OrderByDescending(m => m.Id).Take(limite).ToListAsync(ct);
        pagina.Reverse();
        return pagina;
    }

    public async Task<IReadOnlyList<MensagemChat>> ListarAguardandoModeracaoAsync(CancellationToken ct = default) =>
        await _db.MensagensChat.AsNoTracking()
            .Include(m => m.Participante)
            .Include(m => m.Denuncias)
            .Where(m => m.RevisadaEmUtc == null && (m.Oculta || m.Denuncias.Any()))
            .OrderByDescending(m => m.Oculta)
            .ThenByDescending(m => m.Denuncias.Count)
            .ThenBy(m => m.Id)
            .ToListAsync(ct);

    public Task<int> ContarMensagensDoParticipanteDesdeAsync(int participanteId, DateTime desdeUtc, CancellationToken ct = default) =>
        _db.MensagensChat.CountAsync(m => m.ParticipanteId == participanteId && m.EnviadaEmUtc > desdeUtc, ct);

    public async Task AdicionarMensagemAsync(MensagemChat mensagem, CancellationToken ct = default) =>
        await _db.MensagensChat.AddAsync(mensagem, ct);

    public async Task<int> ApagarMensagensAnterioresAsync(DateTime limiteUtc, CancellationToken ct = default)
    {
        // Denúncias primeiro: não depende do PRAGMA foreign_keys do SQLite.
        await _db.DenunciasMensagem
            .Where(d => _db.MensagensChat.Any(m => m.Id == d.MensagemId && m.EnviadaEmUtc < limiteUtc))
            .ExecuteDeleteAsync(ct);
        return await _db.MensagensChat.Where(m => m.EnviadaEmUtc < limiteUtc).ExecuteDeleteAsync(ct);
    }
}
