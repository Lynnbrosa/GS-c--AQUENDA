using Aquenda.Domain;

namespace Aquenda.Application.Curadoria;

public sealed record NovoHorario(DayOfWeek Dia, TimeOnly Abre, TimeOnly Fecha);

/// <summary>
/// Dados para cadastrar um serviço. Os campos comuns valem para todos; os específicos
/// só para a categoria correspondente (ex.: CodigoCnes para Saude).
/// </summary>
public sealed record NovoServico
{
    public CategoriaServico Categoria { get; init; }
    public string Nome { get; init; } = "";
    public string Descricao { get; init; } = "";
    public string? Logradouro { get; init; }
    public string Bairro { get; init; } = "";
    public string Cidade { get; init; } = "São Paulo";
    public string? Cep { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public bool EnderecoSigiloso { get; init; }
    public string? Telefone { get; init; }
    public string? Site { get; init; }
    public string Fonte { get; init; } = "";
    public DateOnly? VerificadoEm { get; init; }
    public Atendimento Atendimentos { get; init; }
    public string? InstrucaoAcesso { get; init; }
    public IReadOnlyList<NovoHorario> Horarios { get; init; } = Array.Empty<NovoHorario>();

    // Acolhimento
    public string? PublicoAlvo { get; init; }
    public bool ExigeEncaminhamento { get; init; }
    public int? CapacidadeDeclarada { get; init; }

    // Centro de referência
    public bool InscreveTranscidadania { get; init; }

    // Saúde
    public string? CodigoCnes { get; init; }
    public TipoAtencao? TipoAtencao { get; init; }
    public bool RedeSampaTrans { get; init; }
    public bool HabilitadaProcessoTransexualizador { get; init; }

    // Jurídico
    public string? Orgao { get; init; }
    public bool AtendeRetificacao { get; init; }
    public bool RecebeDenuncia { get; init; }

    // Rolê
    public TipoRole? TipoRole { get; init; }
    public int FaixaPreco { get; init; }
    public bool Acessivel { get; init; }
}
