using Aquenda.Application.Comum;
using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;

namespace Aquenda.Application.Metricas;

public sealed record ContagemCategoria(CategoriaServico? Categoria, int Buscas, int Acoes);
public sealed record ContagemDia(DateOnly Dia, int Buscas, int Acoes);
public sealed record ContagemServico(int ServicoId, int Acoes);

/// <summary>
/// Indicador de impacto da ficha: buscas que terminam em "como chegar" ou "ligar".
/// </summary>
public sealed record ResumoImpacto(
    DateOnly De,
    DateOnly Ate,
    int Buscas,
    int ComoChegar,
    int Ligar,
    int AbrirSite,
    double? TaxaConversao,
    IReadOnlyList<ContagemCategoria> PorCategoria,
    IReadOnlyList<ContagemDia> PorDia,
    IReadOnlyList<ContagemServico> ServicosMaisProcurados);

public interface IMetricaService
{
    Task RegistrarBuscaAsync(CategoriaServico? categoria, CancellationToken ct = default);
    Task RegistrarAcaoAsync(TipoInteracao tipo, int servicoId, CancellationToken ct = default);
    Task<ResumoImpacto> ResumoAsync(DateOnly? de, DateOnly? ate, CancellationToken ct = default);
}

public sealed class MetricaService : IMetricaService
{
    public const int DiasMaximosResumo = 366;

    private readonly IInteracaoRepository _interacoes;
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public MetricaService(IInteracaoRepository interacoes, IServicoRepository servicos, IUnitOfWork uow, IRelogio relogio)
    {
        _interacoes = interacoes;
        _servicos = servicos;
        _uow = uow;
        _relogio = relogio;
    }

    public Task RegistrarBuscaAsync(CategoriaServico? categoria, CancellationToken ct = default) =>
        IncrementarAsync(TipoInteracao.Busca, categoria, null, ct);

    public async Task RegistrarAcaoAsync(TipoInteracao tipo, int servicoId, CancellationToken ct = default)
    {
        if (tipo == TipoInteracao.Busca)
            throw new ValidacaoException("Busca é contada pelo próprio endpoint de busca.");
        var servico = await _servicos.ObterPorIdAsync(servicoId, ct)
            ?? throw new RecursoNaoEncontradoException("Serviço", servicoId);
        await IncrementarAsync(tipo, servico.Categoria, servico.Id, ct);
    }

    public async Task<ResumoImpacto> ResumoAsync(DateOnly? de, DateOnly? ate, CancellationToken ct = default)
    {
        var hoje = DateOnly.FromDateTime(_relogio.AgoraUtc);
        var fim = ate ?? hoje;
        var inicio = de ?? fim.AddDays(-29);
        if (inicio > fim)
            throw new ValidacaoException("Data inicial depois da final.");
        if (fim.DayNumber - inicio.DayNumber >= DiasMaximosResumo)
            throw new ValidacaoException($"Período de até {DiasMaximosResumo} dias.");

        var linhas = await _interacoes.ListarPeriodoAsync(inicio, fim, ct);

        int Soma(TipoInteracao t) => linhas.Where(l => l.Tipo == t).Sum(l => l.Quantidade);
        static bool EhAcao(InteracaoDiaria l) => l.Tipo is TipoInteracao.ComoChegar or TipoInteracao.Ligar;

        var buscas = Soma(TipoInteracao.Busca);
        var comoChegar = Soma(TipoInteracao.ComoChegar);
        var ligar = Soma(TipoInteracao.Ligar);

        var porCategoria = linhas
            .GroupBy(l => l.Categoria)
            .Select(g => new ContagemCategoria(
                g.Key,
                g.Where(l => l.Tipo == TipoInteracao.Busca).Sum(l => l.Quantidade),
                g.Where(EhAcao).Sum(l => l.Quantidade)))
            .OrderByDescending(c => c.Acoes)
            .ThenBy(c => c.Categoria)
            .ToList();

        var porDia = linhas
            .GroupBy(l => l.Dia)
            .OrderBy(g => g.Key)
            .Select(g => new ContagemDia(
                g.Key,
                g.Where(l => l.Tipo == TipoInteracao.Busca).Sum(l => l.Quantidade),
                g.Where(EhAcao).Sum(l => l.Quantidade)))
            .ToList();

        var top = linhas
            .Where(l => EhAcao(l) && l.ServicoId.HasValue)
            .GroupBy(l => l.ServicoId!.Value)
            .Select(g => new ContagemServico(g.Key, g.Sum(l => l.Quantidade)))
            .OrderByDescending(s => s.Acoes)
            .ThenBy(s => s.ServicoId)
            .Take(5)
            .ToList();

        double? taxa = buscas == 0 ? null : Math.Round((double)(comoChegar + ligar) / buscas, 3);

        return new ResumoImpacto(inicio, fim, buscas, comoChegar, ligar, Soma(TipoInteracao.AbrirSite),
            taxa, porCategoria, porDia, top);
    }

    private async Task IncrementarAsync(TipoInteracao tipo, CategoriaServico? categoria, int? servicoId, CancellationToken ct)
    {
        var dia = DateOnly.FromDateTime(_relogio.AgoraUtc);
        var contador = await _interacoes.ObterAsync(dia, tipo, categoria, servicoId, ct);
        if (contador is null)
        {
            contador = new InteracaoDiaria(dia, tipo, categoria, servicoId);
            await _interacoes.AdicionarAsync(contador, ct);
        }
        contador.Incrementar();
        await _uow.SalvarAsync(ct);
    }
}
