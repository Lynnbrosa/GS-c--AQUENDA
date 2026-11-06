using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Infrastructure.Dados;
using Microsoft.EntityFrameworkCore;

namespace Aquenda.Infrastructure.Repositorios;

public class ServicoRepository : IServicoRepository
{
    private readonly AquendaDbContext _db;

    public ServicoRepository(AquendaDbContext db) => _db = db;

    public Task<Servico?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Servico>> BuscarAsync(CriterioBusca criterio, CancellationToken ct = default)
    {
        IQueryable<Servico> q = _db.Servicos.AsNoTracking();

        if (!criterio.IncluirInativos)
            q = q.Where(s => s.Ativo);

        if (criterio.Categoria is { } categoria)
        {
            var tipo = Discriminadores.De(categoria);
            q = q.Where(s => EF.Property<string>(s, Discriminadores.Coluna) == tipo);
        }

        if (criterio.Atendimento != Atendimento.Nenhum)
        {
            var atendimento = criterio.Atendimento;
            q = q.Where(s => (s.Atendimentos & atendimento) == atendimento);
        }

        if (!string.IsNullOrWhiteSpace(criterio.Texto))
        {
            var padrao = $"%{Escapar(criterio.Texto.Trim())}%";
            q = q.Where(s => EF.Functions.Like(s.Nome, padrao, "\\")
                          || EF.Functions.Like(s.Descricao, padrao, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(criterio.Bairro))
        {
            var bairro = Escapar(criterio.Bairro.Trim());
            q = q.Where(s => EF.Functions.Like(s.Endereco.Bairro, bairro, "\\"));
        }

        if (criterio.Caixa is { } c)
        {
            q = q.Where(s => s.Endereco.Latitude != null
                          && s.Endereco.Latitude >= c.LatMin && s.Endereco.Latitude <= c.LatMax
                          && s.Endereco.Longitude >= c.LonMin && s.Endereco.Longitude <= c.LonMax);
        }

        return await q.OrderBy(s => s.Nome).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<CategoriaServico, int>> ContarAtivosPorCategoriaAsync(CancellationToken ct = default)
    {
        var contagem = await _db.Servicos
            .Where(s => s.Ativo)
            .GroupBy(s => EF.Property<string>(s, Discriminadores.Coluna))
            .Select(g => new { Tipo = g.Key, Quantidade = g.Count() })
            .ToListAsync(ct);

        return contagem.ToDictionary(x => Discriminadores.Categoria(x.Tipo), x => x.Quantidade);
    }

    public async Task<IReadOnlyList<Servico>> ListarParaRevisaoAsync(CancellationToken ct = default) =>
        await _db.Servicos.AsNoTracking()
            .Where(s => s.Ativo && s.AvisosFechamento >= Servico.AvisosParaRevisao)
            .OrderByDescending(s => s.AvisosFechamento)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Servico>> ListarVerificadosAntesDeAsync(DateOnly data, CancellationToken ct = default) =>
        await _db.Servicos.AsNoTracking()
            .Where(s => s.Ativo && s.VerificadoEm < data)
            .OrderBy(s => s.VerificadoEm)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, Servico>> ObterPorIdsExternosAsync(IEnumerable<string> idsExternos,
        CancellationToken ct = default)
    {
        var ids = idsExternos.Distinct().ToList();
        return await _db.Servicos
            .Where(s => s.IdExterno != null && ids.Contains(s.IdExterno))
            .ToDictionaryAsync(s => s.IdExterno!, ct);
    }

    public Task<bool> ExisteAlgumAsync(CancellationToken ct = default) => _db.Servicos.AnyAsync(ct);

    public async Task AdicionarAsync(Servico servico, CancellationToken ct = default) =>
        await _db.Servicos.AddAsync(servico, ct);

    /// <summary>Evita que % e _ digitados pela pessoa virem curinga no LIKE.</summary>
    private static string Escapar(string texto) =>
        texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
