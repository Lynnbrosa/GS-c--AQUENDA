using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>Bar, balada, espaço cultural ou ponto de encontro LGBT+ (aba "rolês").</summary>
public class LocalRole : Servico
{
    public TipoRole Tipo { get; private set; }

    /// <summary>1 = barato, 2 = médio, 3 = caro.</summary>
    public int FaixaPreco { get; private set; }
    public bool Acessivel { get; private set; }

    /// <summary>Valor da tag lgbtq do OpenStreetMap (welcome, primary, only), quando veio de lá.</summary>
    public string? TagOsm { get; private set; }

    public override CategoriaServico Categoria => CategoriaServico.Role;

    protected LocalRole() { }

    public LocalRole(string nome, string descricao, Endereco endereco, string fonte, DateOnly verificadoEm,
        TipoRole tipo, int faixaPreco, bool acessivel, string? tagOsm = null)
        : base(nome, descricao, endereco, fonte, verificadoEm, Atendimento.Convivencia)
    {
        if (faixaPreco is < 0 or > 3)
            throw new DomainException("Faixa de preço vai de 0 (grátis) a 3.");
        if (tagOsm is not null && tagOsm is not ("welcome" or "primary" or "only"))
            throw new DomainException("Tag lgbtq do OSM deve ser welcome, primary ou only.");
        Tipo = tipo;
        FaixaPreco = faixaPreco;
        Acessivel = acessivel;
        TagOsm = tagOsm;
    }

    /// <summary>Atualiza com o que veio de novo da fonte (OpenStreetMap). Não mexe em verificação nem avisos.</summary>
    public void AtualizarDaFonte(string nome, TipoRole tipo, string? tagOsm, bool acessivel)
    {
        Renomear(nome, Descricao);
        if (tagOsm is not null && tagOsm is not ("welcome" or "primary" or "only"))
            throw new DomainException("Tag lgbtq do OSM deve ser welcome, primary ou only.");
        Tipo = tipo;
        TagOsm = tagOsm;
        Acessivel = acessivel;
    }

    protected override string InstrucaoPadrao() => Tipo == TipoRole.EspacoPublico
        ? "Espaço aberto. Vá em grupo à noite e combine um ponto de encontro."
        : "Confira se abre hoje antes de sair de casa.";
}
