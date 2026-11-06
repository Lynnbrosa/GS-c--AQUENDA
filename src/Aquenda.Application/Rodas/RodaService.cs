using Aquenda.Application.Comum;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;

namespace Aquenda.Application.Rodas;

public sealed record NovaRoda(
    string Titulo,
    string Descricao,
    string Organizador,
    DateTime InicioUtc,
    int DuracaoMinutos,
    bool Presencial,
    int? LocalId,
    string? LinkGrupo,
    int IdadeMinima);

public interface IRodaService
{
    Task<IReadOnlyList<RodaConversa>> ProximasAsync(int dias, bool? presencial, CancellationToken ct = default);
    Task<RodaConversa> ObterAsync(int id, CancellationToken ct = default);
    Task<RodaConversa> CriarAsync(NovaRoda nova, CancellationToken ct = default);
}

public sealed class RodaService : IRodaService
{
    public const int DiasMaximos = 90;

    private readonly IRodaConversaRepository _rodas;
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public RodaService(IRodaConversaRepository rodas, IServicoRepository servicos, IUnitOfWork uow, IRelogio relogio)
    {
        _rodas = rodas;
        _servicos = servicos;
        _uow = uow;
        _relogio = relogio;
    }

    public Task<IReadOnlyList<RodaConversa>> ProximasAsync(int dias, bool? presencial, CancellationToken ct = default)
    {
        if (dias is < 1 or > DiasMaximos)
            throw new ValidacaoException($"Período entre 1 e {DiasMaximos} dias.");
        var agora = _relogio.AgoraUtc;
        return _rodas.ListarEntreAsync(agora, agora.AddDays(dias), presencial, ct);
    }

    public async Task<RodaConversa> ObterAsync(int id, CancellationToken ct = default) =>
        await _rodas.ObterPorIdAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Roda", id);

    public async Task<RodaConversa> CriarAsync(NovaRoda nova, CancellationToken ct = default)
    {
        if (nova.LocalId is int localId)
        {
            var local = await _servicos.ObterPorIdAsync(localId, ct);
            if (local is null || !local.Ativo)
                throw new DomainException($"Local {localId} não está no catálogo.");
            if (local.Endereco.Sigiloso)
                throw new DomainException("Roda aberta não pode acontecer em endereço sigiloso.");
        }

        var roda = new RodaConversa(nova.Titulo, nova.Descricao, nova.Organizador,
            DateTime.SpecifyKind(nova.InicioUtc.ToUniversalTime(), DateTimeKind.Utc),
            nova.DuracaoMinutos, nova.Presencial, nova.LocalId, nova.LinkGrupo, nova.IdadeMinima,
            ilustrativa: false, _relogio.AgoraUtc);

        await _rodas.AdicionarAsync(roda, ct);
        await _uow.SalvarAsync(ct);
        return await ObterAsync(roda.Id, ct);
    }
}
