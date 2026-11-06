using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Repositorios;

public class InteracaoRepository : IInteracaoRepository
{
    private readonly AquendaDbContext _db;

    public InteracaoRepository(AquendaDbContext db) => _db = db;

    public async Task<InteracaoDiaria?> ObterAsync(DateOnly dia, TipoInteracao tipo, CategoriaServico? categoria, int? servicoId, CancellationToken ct = default)
    {
        // Contador criado nesta mesma requisição e ainda não salvo.
        var local = _db.Interacoes.Local.FirstOrDefault(i => i.Dia == dia && i.Tipo == tipo && i.Categoria == categoria && i.ServicoId == servicoId);
        if (local is not null) return local;

        return await _db.Interacoes.FirstOrDefaultAsync(
            i => i.Dia == dia && i.Tipo == tipo && i.Categoria == categoria && i.ServicoId == servicoId, ct);
    }

    public async Task<IReadOnlyList<InteracaoDiaria>> ListarPeriodoAsync(DateOnly de, DateOnly ate, CancellationToken ct = default) =>
        await _db.Interacoes.AsNoTracking()
            .Where(i => i.Dia >= de && i.Dia <= ate)
            .ToListAsync(ct);

    public async Task AdicionarAsync(InteracaoDiaria interacao, CancellationToken ct = default) =>
        await _db.Interacoes.AddAsync(interacao, ct);
}
