using Aquenda.Application.Comum;
using Aquenda.Domain;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;

namespace Aquenda.Application.Ocupacao;

/// <summary>Leitura recebida do ESP32 (MQTT) ou de uma pessoa da casa (POST).</summary>
public sealed record NovaLeitura(int Total, int Ocupadas, DateTime? RegistradaEmUtc);

public enum SituacaoVagas
{
    SemLeitura,
    Disponivel,
    Lotada,
    Desatualizada
}

/// <summary>O que o app mostra como "vagas agora".</summary>
public sealed record VagasAgora(
    int CasaId,
    string Casa,
    SituacaoVagas Situacao,
    int? Total,
    int? Ocupadas,
    int? Livres,
    DateTime? AtualizadoEmUtc,
    OrigemLeitura? Origem,
    string ComoAcessar);

public sealed record HistoricoOcupacao(
    int CasaId,
    DateTime DesdeUtc,
    IReadOnlyList<LeituraOcupacao> Leituras,
    double? OcupacaoMedia,
    int? PicoOcupadas,
    int VezesLotada);

public interface IOcupacaoService
{
    Task<VagasAgora> RegistrarAsync(int casaId, NovaLeitura leitura, OrigemLeitura origem, CancellationToken ct = default);
    Task<VagasAgora> ObterVagasAsync(int casaId, CancellationToken ct = default);
    Task<IReadOnlyList<VagasAgora>> PainelAsync(bool somenteComVaga = false, CancellationToken ct = default);
    Task<HistoricoOcupacao> HistoricoAsync(int casaId, int horas, CancellationToken ct = default);
}

public sealed class OcupacaoService : IOcupacaoService
{
    public const int HorasMaximasHistorico = 24 * 30;

    private readonly ICasaAcolhimentoRepository _casas;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public OcupacaoService(ICasaAcolhimentoRepository casas, IUnitOfWork uow, IRelogio relogio)
    {
        _casas = casas;
        _uow = uow;
        _relogio = relogio;
    }

    public async Task<VagasAgora> RegistrarAsync(int casaId, NovaLeitura leitura, OrigemLeitura origem, CancellationToken ct = default)
    {
        var casa = await ObterCasaAsync(casaId, ct);
        var agora = _relogio.AgoraUtc;
        var registrada = casa.RegistrarLeitura(leitura.Total, leitura.Ocupadas,
            leitura.RegistradaEmUtc?.ToUniversalTime() ?? agora, origem, agora);
        await _uow.SalvarAsync(ct);

        // Leitura atrasada (chegou depois de outra mais nova) não muda o "agora".
        var ultima = await _casas.UltimaLeituraAsync(casaId, ct) ?? registrada;
        return Montar(casa, ultima, agora);
    }

    public async Task<VagasAgora> ObterVagasAsync(int casaId, CancellationToken ct = default)
    {
        var casa = await ObterCasaAsync(casaId, ct);
        var ultima = await _casas.UltimaLeituraAsync(casaId, ct);
        return Montar(casa, ultima, _relogio.AgoraUtc);
    }

    public async Task<IReadOnlyList<VagasAgora>> PainelAsync(bool somenteComVaga = false, CancellationToken ct = default)
    {
        // Duas consultas em sequência: o mesmo DbContext não aceita consultas paralelas.
        var casas = await _casas.ListarAtivasAsync(ct);
        var ultimas = await _casas.UltimasLeiturasAsync(ct);
        var agora = _relogio.AgoraUtc;

        return casas
            .Select(c => Montar(c, ultimas.GetValueOrDefault(c.Id), agora))
            .Where(v => !somenteComVaga || v.Situacao == SituacaoVagas.Disponivel)
            .OrderBy(v => Prioridade(v.Situacao))
            .ThenByDescending(v => v.Livres ?? 0)
            .ThenBy(v => v.Casa)
            .ToList();
    }

    public async Task<HistoricoOcupacao> HistoricoAsync(int casaId, int horas, CancellationToken ct = default)
    {
        if (horas is < 1 or > HorasMaximasHistorico)
            throw new ValidacaoException($"Histórico entre 1 e {HorasMaximasHistorico} horas.");
        await ObterCasaAsync(casaId, ct);

        var desde = _relogio.AgoraUtc.AddHours(-horas);
        var leituras = await _casas.HistoricoAsync(casaId, desde, ct);

        return new HistoricoOcupacao(
            casaId,
            desde,
            leituras,
            leituras.Count == 0 ? null : Math.Round(leituras.Average(l => l.TaxaOcupacao), 3),
            leituras.Count == 0 ? null : leituras.Max(l => l.Ocupadas),
            leituras.Count(l => l.Lotada));
    }

    /// <summary>Ordem do painel: quem tem vaga, depois lotada, depois sem dado confiável.</summary>
    private static int Prioridade(SituacaoVagas situacao) => situacao switch
    {
        SituacaoVagas.Disponivel => 0,
        SituacaoVagas.Lotada => 1,
        SituacaoVagas.Desatualizada => 2,
        _ => 3
    };

    private async Task<CasaAcolhimento> ObterCasaAsync(int casaId, CancellationToken ct)
    {
        var casa = await _casas.ObterPorIdAsync(casaId, ct);
        if (casa is null || !casa.Ativo)
            throw new RecursoNaoEncontradoException("Casa de acolhimento", casaId);
        return casa;
    }

    private static VagasAgora Montar(CasaAcolhimento casa, LeituraOcupacao? leitura, DateTime agora)
    {
        var situacao = leitura switch
        {
            null => SituacaoVagas.SemLeitura,
            _ when leitura.EstaDesatualizada(agora) => SituacaoVagas.Desatualizada,
            { Lotada: true } => SituacaoVagas.Lotada,
            _ => SituacaoVagas.Disponivel
        };

        return new VagasAgora(casa.Id, casa.Nome, situacao, leitura?.Total, leitura?.Ocupadas, leitura?.Livres,
            leitura?.RegistradaEmUtc, leitura?.Origem, casa.ComoAcessar());
    }
}
