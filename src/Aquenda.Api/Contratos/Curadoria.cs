using System.ComponentModel.DataAnnotations;
using Aquenda.Domain;

namespace Aquenda.Api.Contratos;

public sealed record HorarioRequisicao
{
    [Required] public DayOfWeek? Dia { get; init; }
    /// <summary>"09:00". Igual a "fecha" significa 24h; menor que "abre" passa da meia-noite.</summary>
    [Required, RegularExpression(@"^\d{2}:\d{2}$")] public string Abre { get; init; } = "";
    [Required, RegularExpression(@"^\d{2}:\d{2}$")] public string Fecha { get; init; } = "";
}

/// <summary>Cadastro de serviço pela curadoria. Preencha só os campos da categoria escolhida.</summary>
public sealed record NovoServicoRequisicao
{
    [Required] public CategoriaServico? Categoria { get; init; }
    [Required, StringLength(150, MinimumLength = 2)] public string Nome { get; init; } = "";
    [StringLength(1000)] public string Descricao { get; init; } = "";
    [StringLength(200)] public string? Logradouro { get; init; }
    [Required, StringLength(100)] public string Bairro { get; init; } = "";
    [StringLength(100)] public string Cidade { get; init; } = "São Paulo";
    /// <summary>Sem latitude/longitude, a API tenta achar pelo CEP (BrasilAPI).</summary>
    [StringLength(9)] public string? Cep { get; init; }
    [Range(-90, 90)] public double? Latitude { get; init; }
    [Range(-180, 180)] public double? Longitude { get; init; }
    public bool EnderecoSigiloso { get; init; }
    [StringLength(30)] public string? Telefone { get; init; }
    [Url, StringLength(300)] public string? Site { get; init; }
    [Required, StringLength(500)] public string Fonte { get; init; } = "";
    public DateOnly? VerificadoEm { get; init; }
    public Atendimento[]? Atendimentos { get; init; }
    [StringLength(500)] public string? InstrucaoAcesso { get; init; }
    public HorarioRequisicao[]? Horarios { get; init; }

    [StringLength(200)] public string? PublicoAlvo { get; init; }
    public bool ExigeEncaminhamento { get; init; }
    [Range(1, 500)] public int? CapacidadeDeclarada { get; init; }
    public bool InscreveTranscidadania { get; init; }
    [StringLength(7)] public string? CodigoCnes { get; init; }
    public TipoAtencao? TipoAtencao { get; init; }
    public bool RedeSampaTrans { get; init; }
    public bool HabilitadaProcessoTransexualizador { get; init; }
    [StringLength(200)] public string? Orgao { get; init; }
    public bool AtendeRetificacao { get; init; }
    public bool RecebeDenuncia { get; init; }
    public TipoRole? TipoRole { get; init; }
    [Range(0, 3)] public int FaixaPreco { get; init; }
    public bool Acessivel { get; init; }
}

public sealed record VerificacaoRequisicao
{
    /// <summary>Dia da conferência. Vazio = hoje.</summary>
    public DateOnly? Data { get; init; }
}

public sealed record ItemRevisaoResposta(ServicoResumoResposta Servico, string Motivo, bool Ativo);
