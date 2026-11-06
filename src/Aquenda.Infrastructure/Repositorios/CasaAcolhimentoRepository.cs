using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Repositorios;

public class CasaAcolhimentoRepository : ICasaAcolhimentoRepository
{
    private readonly AquendaDbContext _db;

    public CasaAcolhimentoRepository(AquendaDbContext db) => _db = db;

    public Task<CasaAcolhimento?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Casas.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<CasaAcolhimento>> ListarAtivasAsync(CancellationToken ct = default) =>
        await _db.Casas.AsNoTracking()
            .Where(c => c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync(ct);

    public Task<LeituraOcupacao?> UltimaLeituraAsync(int casaId, CancellationToken ct = default) =>
        _db.Leituras.AsNoTracking()
            .Where(l => l.CasaId == casaId)
            .OrderByDescending(l => l.RegistradaEmUtc)
            .ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<int, LeituraOcupacao>> UltimasLeiturasAsync(CancellationToken ct = default)
    {
        // Vira ROW_NUMBER() OVER (PARTITION BY CasaId ...) no SQLite: uma linha por casa.
        var ultimas = await _db.Leituras.AsNoTracking()
            .GroupBy(l => l.CasaId)
            .Select(g => g.OrderByDescending(l => l.RegistradaEmUtc).ThenByDescending(l => l.Id).First())
            .ToListAsync(ct);

        return ultimas.ToDictionary(l => l.CasaId);
    }

    public async Task<IReadOnlyList<LeituraOcupacao>> HistoricoAsync(int casaId, DateTime desdeUtc, CancellationToken ct = default) =>
        await _db.Leituras.AsNoTracking()
            .Where(l => l.CasaId == casaId && l.RegistradaEmUtc >= desdeUtc)
            .OrderBy(l => l.RegistradaEmUtc)
            .ToListAsync(ct);
}
