using Aquenda.Application.Comum;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Application.Curadoria;

/// <summary>Converte CEP em coordenada. A infraestrutura implementa com a BrasilAPI.</summary>
public interface IGeocodificador
{
    /// <summary>Null quando o CEP não tem coordenada ou a fonte está fora do ar.</summary>
    Task<Coordenada?> CoordenadaDoCepAsync(string cep, CancellationToken ct = default);
}

public sealed record ItemRevisao(Servico Servico, string Motivo);

public interface ICuradoriaService
{
    Task<Servico> CriarAsync(NovoServico novo, CancellationToken ct = default);
    Task<Servico> MarcarVerificadoAsync(int id, DateOnly? data, CancellationToken ct = default);
    Task<Servico> DesativarAsync(int id, CancellationToken ct = default);
    Task<Servico> ReativarAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ItemRevisao>> FilaRevisaoAsync(CancellationToken ct = default);
}

public sealed class CuradoriaService : ICuradoriaService
{
    private readonly IServicoRepository _servicos;
    private readonly IGeocodificador _geo;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public CuradoriaService(IServicoRepository servicos, IGeocodificador geo, IUnitOfWork uow, IRelogio relogio)
    {
        _servicos = servicos;
        _geo = geo;
        _uow = uow;
        _relogio = relogio;
    }

    private DateOnly Hoje => DateOnly.FromDateTime(_relogio.AgoraUtc);

    public async Task<Servico> CriarAsync(NovoServico novo, CancellationToken ct = default)
    {
        if (novo.Latitude.HasValue != novo.Longitude.HasValue)
            throw new ValidacaoException("Informe latitude e longitude juntas.");

        Coordenada? coordenada = novo.Latitude is double lat && novo.Longitude is double lon
            ? new Coordenada(lat, lon)
            : null;

        // Sem coordenada, tenta pelo CEP. Endereço sigiloso não consulta: o CEP já aponta a rua.
        if (coordenada is null && !novo.EnderecoSigiloso && FabricaServicos.NormalizarCep(novo.Cep) is { } cep)
            coordenada = await _geo.CoordenadaDoCepAsync(cep, ct);

        var servico = FabricaServicos.Criar(novo, coordenada, Hoje);
        await _servicos.AdicionarAsync(servico, ct);
        await _uow.SalvarAsync(ct);
        return servico;
    }

    public async Task<Servico> MarcarVerificadoAsync(int id, DateOnly? data, CancellationToken ct = default)
    {
        var dia = data ?? Hoje;
        if (dia > Hoje) throw new DomainException("Data de verificação no futuro.");
        var servico = await ObterAsync(id, ct);
        servico.MarcarVerificado(dia);
        await _uow.SalvarAsync(ct);
        return servico;
    }

    public async Task<Servico> DesativarAsync(int id, CancellationToken ct = default)
    {
        var servico = await ObterAsync(id, ct);
        servico.Desativar();
        await _uow.SalvarAsync(ct);
        return servico;
    }

    public async Task<Servico> ReativarAsync(int id, CancellationToken ct = default)
    {
        var servico = await ObterAsync(id, ct);
        servico.Reativar();
        await _uow.SalvarAsync(ct);
        return servico;
    }

    public async Task<IReadOnlyList<ItemRevisao>> FilaRevisaoAsync(CancellationToken ct = default)
    {
        var avisados = await _servicos.ListarParaRevisaoAsync(ct);
        var vencidos = await _servicos.ListarVerificadosAntesDeAsync(Hoje.AddDays(-Servico.DiasValidadeVerificacao), ct);

        return avisados
            .Select(s => new ItemRevisao(s, $"{s.AvisosFechamento} avisos de que fechou ou mudou"))
            .Concat(vencidos
                .Where(v => avisados.All(a => a.Id != v.Id))
                .Select(s => new ItemRevisao(s, $"Verificado pela última vez em {s.VerificadoEm:dd/MM/yyyy}")))
            .ToList();
    }

    private async Task<Servico> ObterAsync(int id, CancellationToken ct) =>
        await _servicos.ObterPorIdAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Serviço", id);
}
