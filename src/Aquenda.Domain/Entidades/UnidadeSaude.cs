using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Estabelecimento de saúde do SUS. Guarda o código CNES e se tem a habilitação
/// 30.02 (processo transexualizador) ou faz parte da Rede SAMPA Trans.
/// </summary>
public class UnidadeSaude : Servico
{
    public string? CodigoCnes { get; private set; }
    public TipoAtencao TipoAtencao { get; private set; }
    public bool HabilitadaProcessoTransexualizador { get; private set; }
    public bool RedeSampaTrans { get; private set; }
    public bool ExigeEncaminhamento { get; private set; }

    public override CategoriaServico Categoria => CategoriaServico.Saude;

    protected UnidadeSaude() { }

    public UnidadeSaude(string nome, string descricao, Endereco endereco, string fonte, DateOnly verificadoEm,
        Atendimento atendimentos, TipoAtencao tipoAtencao, bool redeSampaTrans,
        bool habilitadaProcessoTransexualizador = false, bool exigeEncaminhamento = false, string? codigoCnes = null)
        : base(nome, descricao, endereco, fonte, verificadoEm, atendimentos)
    {
        if (codigoCnes is not null && (codigoCnes.Length != 7 || !codigoCnes.All(char.IsDigit)))
            throw new DomainException("Código CNES tem 7 dígitos.");
        if (tipoAtencao == TipoAtencao.Primaria && exigeEncaminhamento)
            throw new DomainException("Atenção primária é porta de entrada e não exige encaminhamento.");

        CodigoCnes = codigoCnes;
        TipoAtencao = tipoAtencao;
        RedeSampaTrans = redeSampaTrans;
        HabilitadaProcessoTransexualizador = habilitadaProcessoTransexualizador;
        ExigeEncaminhamento = exigeEncaminhamento;
    }

    protected override string InstrucaoPadrao() => (TipoAtencao, ExigeEncaminhamento) switch
    {
        (TipoAtencao.Primaria, _) when RedeSampaTrans =>
            "Vá à UBS e diga que quer começar o acompanhamento pela Rede SAMPA Trans. Leve documento e, se tiver, o cartão do SUS.",
        (TipoAtencao.Primaria, _) =>
            "Vá à UBS de referência do seu endereço com um documento.",
        (TipoAtencao.Secundaria, true) =>
            "Comece pela UBS de referência da sua casa e peça encaminhamento para cá.",
        _ => "Ligue antes para confirmar como agendar."
    };
}
