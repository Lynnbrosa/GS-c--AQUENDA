using System.ComponentModel.DataAnnotations;

namespace Aquenda.Api.Contratos;

public sealed record NovaRodaRequisicao
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; init; } = "";

    [StringLength(1000)]
    public string Descricao { get; init; } = "";

    [Required, StringLength(150, MinimumLength = 2)]
    public string Organizador { get; init; } = "";

    [Required]
    public DateTimeOffset? Inicio { get; init; }

    [Range(15, 600)]
    public int DuracaoMinutos { get; init; } = 120;

    public bool Presencial { get; init; } = true;

    /// <summary>Id de um serviço do catálogo. Obrigatório se presencial.</summary>
    public int? LocalId { get; init; }

    /// <summary>Link https do grupo moderado pela ONG. Obrigatório se online.</summary>
    [StringLength(300)]
    public string? LinkGrupo { get; init; }

    [Range(0, 100)]
    public int IdadeMinima { get; init; } = 18;
}

public sealed record LocalRodaResposta(int Id, string Nome, string Endereco);

public sealed record RodaResposta(
    int Id,
    string Titulo,
    string Descricao,
    string Organizador,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    bool Presencial,
    LocalRodaResposta? Local,
    string? LinkGrupo,
    int IdadeMinima,
    bool Exemplo);
