using Aquenda.Domain;

namespace Aquenda.Infrastructure.Dados;

/// <summary>Valor da coluna "TipoServico" (herança TPH) de cada categoria.</summary>
internal static class Discriminadores
{
    public const string Coluna = "TipoServico";
    public const string Casa = "casa";
    public const string Centro = "centro";
    public const string Saude = "saude";
    public const string Juridico = "juridico";
    public const string Role = "role";

    public static string De(CategoriaServico categoria) => categoria switch
    {
        CategoriaServico.Acolhimento => Casa,
        CategoriaServico.CentroReferencia => Centro,
        CategoriaServico.Saude => Saude,
        CategoriaServico.Juridico => Juridico,
        CategoriaServico.Role => Role,
        _ => throw new ArgumentOutOfRangeException(nameof(categoria))
    };

    public static CategoriaServico Categoria(string discriminador) => discriminador switch
    {
        Casa => CategoriaServico.Acolhimento,
        Centro => CategoriaServico.CentroReferencia,
        Saude => CategoriaServico.Saude,
        Juridico => CategoriaServico.Juridico,
        Role => CategoriaServico.Role,
        _ => throw new ArgumentOutOfRangeException(nameof(discriminador))
    };
}
