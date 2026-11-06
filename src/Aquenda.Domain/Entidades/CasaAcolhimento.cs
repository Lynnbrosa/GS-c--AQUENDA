using Aquenda.Domain.Comum;
using Aquenda.Domain.ValueObjects;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Casa de acolhimento (ex.: Casa Florescer). Recebe leituras de ocupação do
/// ESP32 via MQTT, e é daí que o app tira o "vagas agora".
/// </summary>
public class CasaAcolhimento : Servico
{
    /// <summary>Tolerância para relógio adiantado no dispositivo.</summary>
    public static readonly TimeSpan ToleranciaRelogio = TimeSpan.FromMinutes(5);

    private readonly List<LeituraOcupacao> _leituras = new();

    public string PublicoAlvo { get; private set; } = null!;
    public int? CapacidadeDeclarada { get; private set; }
    public bool ExigeEncaminhamento { get; private set; }

    public IReadOnlyCollection<LeituraOcupacao> Leituras => _leituras.AsReadOnly();

    public override CategoriaServico Categoria => CategoriaServico.Acolhimento;

    protected CasaAcolhimento() { } // EF Core

    public CasaAcolhimento(string nome, string descricao, Endereco endereco, string fonte,
        DateOnly verificadoEm, string publicoAlvo, bool exigeEncaminhamento, int? capacidadeDeclarada = null,
        Atendimento atendimentos = Atendimento.Acolhida)
        : base(nome, descricao, endereco, fonte, verificadoEm, atendimentos | Atendimento.Acolhida)
    {
        if (string.IsNullOrWhiteSpace(publicoAlvo))
            throw new DomainException("Público-alvo é obrigatório.");
        if (capacidadeDeclarada is <= 0)
            throw new DomainException("Capacidade declarada deve ser positiva.");
        PublicoAlvo = publicoAlvo.Trim();
        ExigeEncaminhamento = exigeEncaminhamento;
        CapacidadeDeclarada = capacidadeDeclarada;
    }

    /// <summary>
    /// Registra quantas camas estão ocupadas. Valida os números antes de aceitar,
    /// porque um sensor com defeito não pode anunciar vaga que não existe.
    /// </summary>
    public LeituraOcupacao RegistrarLeitura(int total, int ocupadas, DateTime registradaEmUtc,
        OrigemLeitura origem, DateTime agoraUtc)
    {
        if (!Ativo)
            throw new DomainException("Casa inativa não recebe leituras.");
        if (registradaEmUtc > agoraUtc + ToleranciaRelogio)
            throw new DomainException("Leitura com data no futuro. Confira o relógio do dispositivo.");

        var leitura = new LeituraOcupacao(this, total, ocupadas, registradaEmUtc, origem, agoraUtc);
        _leituras.Add(leitura);
        return leitura;
    }

    protected override string InstrucaoPadrao() => ExigeEncaminhamento
        ? "Peça a vaga pelo CRAS ou CREAS mais perto. Eles fazem o encaminhamento para a casa."
        : "Entre em contato com a casa antes de ir. O endereço é passado no primeiro contato.";
}
