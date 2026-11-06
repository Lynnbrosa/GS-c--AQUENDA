using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>Defensoria, núcleos jurídicos e canais de denúncia de LGBTfobia.</summary>
public class ServicoJuridico : Servico
{
    public string Orgao { get; private set; } = null!;
    public bool AtendeRetificacao { get; private set; }
    public bool RecebeDenuncia { get; private set; }

    public override CategoriaServico Categoria => CategoriaServico.Juridico;

    protected ServicoJuridico() { }

    public ServicoJuridico(string nome, string descricao, Endereco endereco, string fonte, DateOnly verificadoEm,
        string orgao, bool atendeRetificacao, bool recebeDenuncia)
        : base(nome, descricao, endereco, fonte, verificadoEm,
            Atendimento.Juridico
            | (atendeRetificacao ? Atendimento.Retificacao : Atendimento.Nenhum)
            | (recebeDenuncia ? Atendimento.Denuncia : Atendimento.Nenhum))
    {
        Orgao = string.IsNullOrWhiteSpace(orgao) ? nome : orgao.Trim();
        AtendeRetificacao = atendeRetificacao;
        RecebeDenuncia = recebeDenuncia;
    }

    protected override string InstrucaoPadrao() => AtendeRetificacao
        ? "Para retificar nome e gênero no cartório não precisa de advogado nem de laudo. A Defensoria ajuda se houver custo ou recusa."
        : "Leve documento e o que tiver de prova (prints, nomes, datas).";
}
