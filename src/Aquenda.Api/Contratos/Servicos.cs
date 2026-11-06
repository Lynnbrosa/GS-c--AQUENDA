using System.ComponentModel.DataAnnotations;
using Aquenda.Domain;

namespace Aquenda.Api.Contratos;

/// <summary>Filtros da busca de serviços. Todos opcionais.</summary>
public sealed class BuscaServicosRequisicao
{
    /// <summary>Acolhimento, CentroReferencia, Saude, Juridico ou Role.</summary>
    public CategoriaServico? Categoria { get; init; }

    /// <summary>Um ou mais atendimentos que o serviço precisa ter (ex.: Hormonizacao).</summary>
    public Atendimento[]? Atendimento { get; init; }

    /// <summary>Procura no nome e na descrição.</summary>
    [StringLength(100)]
    public string? Texto { get; init; }

    [StringLength(100)]
    public string? Bairro { get; init; }

    /// <summary>Latitude de onde a pessoa está (o app não guarda).</summary>
    [Range(-90, 90)]
    public double? Lat { get; init; }

    [Range(-180, 180)]
    public double? Lon { get; init; }

    /// <summary>Raio em km. Exige lat e lon.</summary>
    public double? RaioKm { get; init; }

    /// <summary>Só o que está aberto agora (horário de São Paulo).</summary>
    public bool Abertos { get; init; }

    public int Pagina { get; init; } = 1;
    public int Tamanho { get; init; } = 20;
}

public sealed record EnderecoResposta(
    string Texto,
    string Bairro,
    string Cidade,
    string? Logradouro,
    bool Sigiloso,
    double? Latitude,
    double? Longitude);

public sealed record ServicoResumoResposta(
    int Id,
    string Nome,
    CategoriaServico Categoria,
    EnderecoResposta Endereco,
    double? DistanciaKm,
    bool? AbertoAgora,
    string ComoAcessar,
    IReadOnlyList<string> Atendimentos,
    string? Telefone,
    string? Site,
    string Fonte,
    DateOnly VerificadoEm,
    bool PrecisaRevisao);

public sealed record HorarioResposta(string Dia, string? Abre, string? Fecha, bool DiaInteiro);

public sealed record AcolhimentoDetalhe(string PublicoAlvo, int? CapacidadeDeclarada, bool ExigeEncaminhamento);
public sealed record CentroReferenciaDetalhe(bool InscreveTranscidadania);
public sealed record SaudeDetalhe(string? CodigoCnes, TipoAtencao TipoAtencao, bool RedeSampaTrans,
    bool HabilitadaProcessoTransexualizador, bool ExigeEncaminhamento);
public sealed record JuridicoDetalhe(string Orgao, bool AtendeRetificacao, bool RecebeDenuncia);
public sealed record RoleDetalhe(TipoRole Tipo, int FaixaPreco, bool Acessivel, string? TagOsm);

/// <summary>Detalhe de um serviço. Só a seção do tipo dele vem preenchida.</summary>
public sealed record ServicoDetalheResposta(
    ServicoResumoResposta Resumo,
    string Descricao,
    IReadOnlyList<HorarioResposta> Horarios,
    AcolhimentoDetalhe? Acolhimento,
    CentroReferenciaDetalhe? CentroReferencia,
    SaudeDetalhe? Saude,
    JuridicoDetalhe? Juridico,
    RoleDetalhe? Role);

public sealed record AvisoFechamentoResposta(int ServicoId, int Avisos, bool EntrouEmRevisao);

public sealed record PaginaResposta<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int Tamanho, int TotalPaginas);
