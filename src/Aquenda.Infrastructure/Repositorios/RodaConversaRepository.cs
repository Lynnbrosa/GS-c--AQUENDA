using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Repositorios;

public class RodaConversaRepository : IRodaConversaRepository
{
    /// <summary>Duração máxima de uma roda; usada para cortar no banco antes do filtro exato.</summary>
    private const int DuracaoMaximaMinutos = 600;

    private readonly AquendaDbContext _db;

    public RodaConversaRepository(AquendaDbContext db) => _db = db;

    public Task<RodaConversa?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Rodas.AsNoTracking().Include(r => r.Local).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<RodaConversa>> ListarEntreAsync(DateTime deUtc, DateTime ateUtc, bool? presencial,
        CancellationToken ct = default)
    {
        var inicioMinimo = deUtc.AddMinutes(-DuracaoMaximaMinutos);
        var q = _db.Rodas.AsNoTracking()
            .Include(r => r.Local)
            .Where(r => r.InicioUtc < ateUtc && r.InicioUtc > inicioMinimo);

        if (presencial is bool p)
            q = q.Where(r => r.Presencial == p);

        var candidatas = await q.OrderBy(r => r.InicioUtc).ToListAsync(ct);
        return candidatas.Where(r => r.AconteceEntre(deUtc, ateUtc)).ToList();
    }

    public async Task AdicionarAsync(RodaConversa roda, CancellationToken ct = default) =>
        await _db.Rodas.AddAsync(roda, ct);
}
