using Aquenda.Domain.Entidades;

namespace Aquenda.Domain.Repositorios;

/// <summary>
/// Critério de busca que o repositório traduz para SQL. A distância exata é
/// calculada depois, em memória; aqui só entra a caixa envolvente.
/// </summary>
public sealed record CriterioBusca
{
    public CategoriaServico? Categoria { get; init; }
    public Atendimento Atendimento { get; init; } = Atendimento.Nenhum;
    public string? Texto { get; init; }
    public string? Bairro { get; init; }
    public (double LatMin, double LatMax, double LonMin, double LonMax)? Caixa { get; init; }
    public bool IncluirInativos { get; init; }
}

public interface IServicoRepository
{
    Task<Servico?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> BuscarAsync(CriterioBusca criterio, CancellationToken ct = default);
    Task<IReadOnlyDictionary<CategoriaServico, int>> ContarAtivosPorCategoriaAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> ListarParaRevisaoAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Servico>> ListarVerificadosAntesDeAsync(DateOnly data, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, Servico>> ObterPorIdsExternosAsync(IEnumerable<string> idsExternos, CancellationToken ct = default);
    Task<bool> ExisteAlgumAsync(CancellationToken ct = default);
    Task AdicionarAsync(Servico servico, CancellationToken ct = default);
}

public interface ICasaAcolhimentoRepository
{
    Task<CasaAcolhimento?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CasaAcolhimento>> ListarAtivasAsync(CancellationToken ct = default);
    Task<LeituraOcupacao?> UltimaLeituraAsync(int casaId, CancellationToken ct = default);

    /// <summary>Última leitura de cada casa, numa consulta só.</summary>
    Task<IReadOnlyDictionary<int, LeituraOcupacao>> UltimasLeiturasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LeituraOcupacao>> HistoricoAsync(int casaId, DateTime desdeUtc, CancellationToken ct = default);
}

public interface IRodaConversaRepository
{
    Task<RodaConversa?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RodaConversa>> ListarEntreAsync(DateTime deUtc, DateTime ateUtc, bool? presencial, CancellationToken ct = default);
    Task AdicionarAsync(RodaConversa roda, CancellationToken ct = default);
}

public interface IInteracaoRepository
{
    Task<InteracaoDiaria?> ObterAsync(DateOnly dia, TipoInteracao tipo, CategoriaServico? categoria, int? servicoId, CancellationToken ct = default);
    Task<IReadOnlyList<InteracaoDiaria>> ListarPeriodoAsync(DateOnly de, DateOnly ate, CancellationToken ct = default);
    Task AdicionarAsync(InteracaoDiaria interacao, CancellationToken ct = default);
}

/// <summary>Fecha a transação: tudo que os repositórios mudaram é salvo junto.</summary>
public interface IUnitOfWork
{
    Task<int> SalvarAsync(CancellationToken ct = default);
}
