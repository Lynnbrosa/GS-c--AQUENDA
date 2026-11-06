using Aquenda.Application.Comum;
using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Aquenda.Domain.Entidades;
using Aquenda.Domain.Repositorios;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Application.Curadoria;

/// <summary>Lugar com a tag lgbtq no OpenStreetMap.</summary>
public sealed record LugarOsm(
    string IdExterno,
    string? Nome,
    TipoRole Tipo,
    string? TagLgbtq,
    double Latitude,
    double Longitude,
    string? Rua,
    string? Numero,
    string? Bairro,
    string? Site,
    string? Telefone,
    bool Acessivel,
    string UrlFonte);

public interface IFonteOsm
{
    Task<IReadOnlyList<LugarOsm>> BuscarLugaresLgbtqAsync(CancellationToken ct = default);
}

public sealed record ResultadoIngestao(int Lidos, int Novos, int Atualizados, int Ignorados, IReadOnlyList<string> Avisos);

public interface IIngestaoOsmService
{
    Task<ResultadoIngestao> ImportarRolesAsync(CancellationToken ct = default);
}

/// <summary>
/// Importa os rolês do OpenStreetMap (tag lgbtq=welcome|primary|only). Lugares novos entram
/// no catálogo; os que já existem (mesmo id OSM) são atualizados sem perder a curadoria.
/// </summary>
public sealed class IngestaoOsmService : IIngestaoOsmService
{
    private readonly IFonteOsm _fonte;
    private readonly IServicoRepository _servicos;
    private readonly IUnitOfWork _uow;
    private readonly IRelogio _relogio;

    public IngestaoOsmService(IFonteOsm fonte, IServicoRepository servicos, IUnitOfWork uow, IRelogio relogio)
    {
        _fonte = fonte;
        _servicos = servicos;
        _uow = uow;
        _relogio = relogio;
    }

    public async Task<ResultadoIngestao> ImportarRolesAsync(CancellationToken ct = default)
    {
        var lugares = await _fonte.BuscarLugaresLgbtqAsync(ct);
        var existentes = await _servicos.ObterPorIdsExternosAsync(lugares.Select(l => l.IdExterno), ct);
        var hoje = DateOnly.FromDateTime(_relogio.AgoraUtc);

        int novos = 0, atualizados = 0, ignorados = 0;
        var avisos = new List<string>();

        foreach (var lugar in lugares.DistinctBy(l => l.IdExterno))
        {
            if (string.IsNullOrWhiteSpace(lugar.Nome))
            {
                ignorados++;
                continue;
            }

            try
            {
                var endereco = new Endereco(MontarRua(lugar), string.IsNullOrWhiteSpace(lugar.Bairro) ? "a confirmar" : lugar.Bairro,
                    coordenada: new Coordenada(lugar.Latitude, lugar.Longitude));

                if (existentes.TryGetValue(lugar.IdExterno, out var existente))
                {
                    if (existente is not LocalRole role)
                    {
                        ignorados++;
                        avisos.Add($"{lugar.IdExterno} já está no catálogo como outro tipo de serviço.");
                        continue;
                    }
                    role.AtualizarDaFonte(lugar.Nome, lugar.Tipo, lugar.TagLgbtq, lugar.Acessivel);
                    role.AtualizarEndereco(endereco);
                    atualizados++;
                }
                else
                {
                    var role = new LocalRole(lugar.Nome, "Importado do OpenStreetMap (tag lgbtq). Aguarda curadoria.",
                        endereco, lugar.UrlFonte, hoje, lugar.Tipo, faixaPreco: 0, lugar.Acessivel, lugar.TagLgbtq);
                    role.VincularFonteExterna(lugar.IdExterno);
                    role.DefinirContato(lugar.Telefone, lugar.Site);
                    await _servicos.AdicionarAsync(role, ct);
                    novos++;
                }
            }
            catch (DomainException e)
            {
                ignorados++;
                avisos.Add($"{lugar.IdExterno} ({lugar.Nome}): {e.Message}");
            }
        }

        await _uow.SalvarAsync(ct);
        return new ResultadoIngestao(lugares.Count, novos, atualizados, ignorados, avisos);
    }

    private static string? MontarRua(LugarOsm l) =>
        string.IsNullOrWhiteSpace(l.Rua) ? null : string.IsNullOrWhiteSpace(l.Numero) ? l.Rua : $"{l.Rua}, {l.Numero}";
}
