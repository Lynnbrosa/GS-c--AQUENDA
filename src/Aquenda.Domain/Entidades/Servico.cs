using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Um serviço do catálogo: lugar que acolhe, cuida, defende ou recebe a comunidade.
/// Classe abstrata: cada tipo concreto sabe sua categoria e explica, do seu jeito,
/// como a pessoa entra no serviço (polimorfismo em <see cref="ComoAcessar"/>).
/// </summary>
public abstract class Servico : Entidade
{
    /// <summary>A partir de quantos avisos "fechou/mudou" o item volta para revisão.</summary>
    public const int AvisosParaRevisao = 3;

    private readonly List<HorarioFuncionamento> _horarios = new();

    public string Nome { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public Endereco Endereco { get; private set; } = null!;
    public string? Telefone { get; private set; }
    public string? Site { get; private set; }
    public Atendimento Atendimentos { get; private set; }

    /// <summary>Instrução de entrada vinda da curadoria. Se vazia, cada tipo usa a sua padrão.</summary>
    public string? InstrucaoAcesso { get; private set; }

    /// <summary>Fonte pública da informação. Todo item do catálogo precisa de uma.</summary>
    public string Fonte { get; private set; } = null!;
    public DateOnly VerificadoEm { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int AvisosFechamento { get; private set; }
    public DateTime? UltimoAvisoEmUtc { get; private set; }

    public IReadOnlyCollection<HorarioFuncionamento> Horarios => _horarios.AsReadOnly();

    public abstract CategoriaServico Categoria { get; }

    protected Servico() { } // EF Core

    protected Servico(string nome, string descricao, Endereco endereco, string fonte,
        DateOnly verificadoEm, Atendimento atendimentos)
    {
        Renomear(nome, descricao);
        Endereco = endereco ?? throw new DomainException("Endereço é obrigatório.");
        DefinirFonte(fonte, verificadoEm);
        Atendimentos = atendimentos;
    }

    public void Renomear(string nome, string descricao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome é obrigatório.");
        if (nome.Length > 150)
            throw new DomainException("Nome deve ter até 150 caracteres.");
        Nome = nome.Trim();
        Descricao = descricao?.Trim() ?? string.Empty;
    }

    public void DefinirFonte(string fonte, DateOnly verificadoEm)
    {
        if (string.IsNullOrWhiteSpace(fonte))
            throw new DomainException("Todo serviço precisa de uma fonte pública.");
        Fonte = fonte.Trim();
        VerificadoEm = verificadoEm;
    }

    public void DefinirContato(string? telefone, string? site)
    {
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        Site = string.IsNullOrWhiteSpace(site) ? null : site.Trim();
    }

    public void DefinirInstrucaoAcesso(string? instrucao) =>
        InstrucaoAcesso = string.IsNullOrWhiteSpace(instrucao) ? null : instrucao.Trim();

    public void DefinirHorarios(IEnumerable<HorarioFuncionamento> horarios)
    {
        _horarios.Clear();
        _horarios.AddRange(horarios);
    }

    public bool Oferece(Atendimento atendimento) =>
        atendimento != Atendimento.Nenhum && Atendimentos.HasFlag(atendimento);

    /// <summary>
    /// Se está aberto no instante UTC informado. Nulo quando não há horário cadastrado:
    /// é melhor dizer "não sei" do que mandar alguém para uma porta fechada.
    /// </summary>
    public bool? EstaAbertoEm(DateTime agoraUtc)
    {
        if (_horarios.Count == 0) return null;
        var local = FusoSaoPaulo.ParaLocal(agoraUtc);
        return _horarios.Any(h => h.Cobre(local));
    }

    /// <summary>Passo de entrada mostrado na tela, como "vá à sua UBS e peça encaminhamento".</summary>
    public string ComoAcessar() => InstrucaoAcesso ?? InstrucaoPadrao();

    /// <summary>Instrução padrão de cada tipo de serviço.</summary>
    protected abstract string InstrucaoPadrao();

    /// <summary>Alguém avisou pelo app que o lugar fechou ou mudou.</summary>
    public void RegistrarAvisoFechamento(DateTime agoraUtc)
    {
        AvisosFechamento++;
        UltimoAvisoEmUtc = agoraUtc;
    }

    public bool PrecisaRevisao => AvisosFechamento >= AvisosParaRevisao;

    /// <summary>Curadoria conferiu de novo: zera os avisos.</summary>
    public void MarcarVerificado(DateOnly data)
    {
        VerificadoEm = data;
        AvisosFechamento = 0;
        UltimoAvisoEmUtc = null;
    }

    public void Desativar() => Ativo = false;
    public void Reativar() => Ativo = true;
}
