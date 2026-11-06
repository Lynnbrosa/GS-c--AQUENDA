using Aquenda.Application.Comum;
using Aquenda.Application.Metricas;
using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Application.Catalogo;

public sealed record FiltroBusca
{
    public const double RaioMaximoKm = 50;
    public const int TamanhoMaximoPagina = 50;

    public CategoriaServico? Categoria { get; init; }
    public Atendimento Atendimento { get; init; } = Atendimento.Nenhum;
    public string? Texto { get; init; }
    public string? Bairro { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? RaioKm { get; init; }
    public bool SomenteAbertos { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanhoPagina { get; init; } = 20;

    public void Validar()
    {
        if (Latitude.HasValue != Longitude.HasValue)
            throw new ValidacaoException("Informe latitude e longitude juntas.");
        if (RaioKm.HasValue && !Latitude.HasValue)
            throw new ValidacaoException("Raio só funciona com latitude e longitude.");
        if (RaioKm is <= 0 or > RaioMaximoKm)
            throw new ValidacaoException($"Raio deve ficar entre 0 e {RaioMaximoKm} km.");
        if (Pagina < 1)
            throw new ValidacaoException("Página começa em 1.");
        if (TamanhoPagina is < 1 or > TamanhoMaximoPagina)
            throw new ValidacaoException($"Tamanho da página entre 1 e {TamanhoMaximoPagina}.");
        if (Texto?.Length > 100)
            throw new ValidacaoException("Texto de busca com até 100 caracteres.");
    }
}

public sealed record ResultadoBusca(Servico Servico, double? DistanciaKm, bool? AbertoAgora);

public sealed record ResumoCatalogo(IReadOnlyDictionary<CategoriaServico, int> PorCategoria, int Total, int ParaRevisao);

public interface ICatalogoService
{
    Task<Pagina<ResultadoBusca>> BuscarAsync(FiltroBusca filtro, CancellationToken ct = default);
    Task<ResultadoBusca> ObterAsync(int id, double? latitude = null, double? longitude = null, CancellationToken ct = default);
    Task<ResumoCatalogo> ResumoAsync(CancellationToken ct = default);
    Task<Servico> AvisarFechamentoAsync(int id, CancellationToken ct = default);
}

public sealed class CatalogoService : ICatalogoService
{
    private readonly IServicoRepository _servicos;
    private readonly IMetricaService _metricas;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public CatalogoService(IServicoRepository servicos, IMetricaService metricas, IUnitOfWork uow, IRelogio relogio)
    {
        _servicos = servicos;
        _metricas = metricas;
        _uow = uow;
        _relogio = relogio;
    }

    public async Task<Pagina<ResultadoBusca>> BuscarAsync(FiltroBusca filtro, CancellationToken ct = default)
    {
        filtro.Validar();
        var origem = Origem(filtro.Latitude, filtro.Longitude);

        var criterio = new CriterioBusca
        {
            Categoria = filtro.Categoria,
            Atendimento = filtro.Atendimento,
            Texto = filtro.Texto,
            Bairro = filtro.Bairro,
            Caixa = origem is { } o && filtro.RaioKm is { } raio ? o.CaixaEnvolvente(raio) : null
        };

        var candidatos = await _servicos.BuscarAsync(criterio, ct);
        var agora = _relogio.AgoraUtc;

        // O banco já cortou pela caixa; aqui entra a distância exata, o filtro de
        // "aberto agora" e a ordenação: mais perto primeiro, sem coordenada no fim.
        var resultados = candidatos
            .Select(s => new ResultadoBusca(s, Distancia(origem, s), s.EstaAbertoEm(agora)))
            .Where(r => filtro.RaioKm is null || r.DistanciaKm <= filtro.RaioKm)
            .Where(r => !filtro.SomenteAbertos || r.AbertoAgora == true)
            .OrderBy(r => r.DistanciaKm is null)
            .ThenBy(r => r.DistanciaKm)
            .ThenBy(r => r.Servico.Categoria)
            .ThenBy(r => r.Servico.Nome, StringComparer.InvariantCultureIgnoreCase)
            .ToList();

        var pagina = resultados
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToList();

        if (filtro.Pagina == 1)
            await _metricas.RegistrarBuscaAsync(filtro.Categoria, ct);

        return new Pagina<ResultadoBusca>(pagina, resultados.Count, filtro.Pagina, filtro.TamanhoPagina);
    }

    public async Task<ResultadoBusca> ObterAsync(int id, double? latitude = null, double? longitude = null, CancellationToken ct = default)
    {
        if (latitude.HasValue != longitude.HasValue)
            throw new ValidacaoException("Informe latitude e longitude juntas.");
        var servico = await ObterAtivoAsync(id, ct);
        return new ResultadoBusca(servico, Distancia(Origem(latitude, longitude), servico), servico.EstaAbertoEm(_relogio.AgoraUtc));
    }

    public async Task<ResumoCatalogo> ResumoAsync(CancellationToken ct = default)
    {
        var porCategoria = await _servicos.ContarAtivosPorCategoriaAsync(ct);
        var revisao = await _servicos.ListarParaRevisaoAsync(ct);
        return new ResumoCatalogo(porCategoria, porCategoria.Values.Sum(), revisao.Count);
    }

    public async Task<Servico> AvisarFechamentoAsync(int id, CancellationToken ct = default)
    {
        var servico = await ObterAtivoAsync(id, ct);
        servico.RegistrarAvisoFechamento(_relogio.AgoraUtc);
        await _uow.SalvarAsync(ct);
        return servico;
    }

    private async Task<Servico> ObterAtivoAsync(int id, CancellationToken ct)
    {
        var servico = await _servicos.ObterPorIdAsync(id, ct);
        if (servico is null || !servico.Ativo)
            throw new RecursoNaoEncontradoException("Serviço", id);
        return servico;
    }

    private static Coordenada? Origem(double? latitude, double? longitude)
    {
        if (latitude is null || longitude is null) return null;
        try { return new Coordenada(latitude.Value, longitude.Value); }
        catch (Aquenda.Domain.Comum.DomainException e) { throw new ValidacaoException(e.Message); }
    }

    private static double? Distancia(Coordenada? origem, Servico servico) =>
        origem is { } o && servico.Endereco.Coordenada is { } destino
            ? Math.Round(o.DistanciaKmAte(destino), 2)
            : null;
}
