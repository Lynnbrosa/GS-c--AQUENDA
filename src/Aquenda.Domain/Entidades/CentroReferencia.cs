using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>Centro de Referência LGBTI+ da Prefeitura: porta de entrada da rede.</summary>
public class CentroReferencia : Servico
{
    /// <summary>Faz inscrição no Transcidadania.</summary>
    public bool InscreveTranscidadania { get; private set; }

    public override CategoriaServico Categoria => CategoriaServico.CentroReferencia;

    protected CentroReferencia() { }

    public CentroReferencia(string nome, string descricao, Endereco endereco, string fonte,
        DateOnly verificadoEm, Atendimento atendimentos, bool inscreveTranscidadania)
        : base(nome, descricao, endereco, fonte, verificadoEm, atendimentos)
    {
        InscreveTranscidadania = inscreveTranscidadania;
    }

    protected override string InstrucaoPadrao() => InscreveTranscidadania
        ? "Vá no horário de atendimento com um documento. Ali também se faz a inscrição no Transcidadania."
        : "Vá no horário de atendimento com um documento.";
}
