using Aquenda.Domain.Comum;

namespace Aquenda.Domain.Entidades;

/// <summary>
/// Foto da ocupação de uma casa num instante. Só é criada pela própria casa,
/// que garante as invariantes (ocupadas entre 0 e total).
/// </summary>
public class LeituraOcupacao : Entidade
{
    /// <summary>Depois desse tempo sem leitura nova, o app avisa que o número pode estar velho.</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromHours(2);

    public int CasaId { get; private set; }
    public int Total { get; private set; }
    public int Ocupadas { get; private set; }
    public DateTime RegistradaEmUtc { get; private set; }
    public DateTime RecebidaEmUtc { get; private set; }
    public OrigemLeitura Origem { get; private set; }

    protected LeituraOcupacao() { } // EF Core

    internal LeituraOcupacao(CasaAcolhimento casa, int total, int ocupadas, DateTime registradaEmUtc,
        OrigemLeitura origem, DateTime recebidaEmUtc)
    {
        if (total <= 0)
            throw new DomainException("Total de vagas deve ser maior que zero.");
        if (total > 500)
            throw new DomainException("Total de vagas acima do esperado para uma casa (máx. 500).");
        if (ocupadas < 0)
            throw new DomainException("Vagas ocupadas não pode ser negativo.");
        if (ocupadas > total)
            throw new DomainException("Vagas ocupadas não pode passar do total.");

        CasaId = casa.Id;
        Total = total;
        Ocupadas = ocupadas;
        RegistradaEmUtc = DateTime.SpecifyKind(registradaEmUtc, DateTimeKind.Utc);
        RecebidaEmUtc = DateTime.SpecifyKind(recebidaEmUtc, DateTimeKind.Utc);
        Origem = origem;
    }

    public int Livres => Total - Ocupadas;
    public double TaxaOcupacao => (double)Ocupadas / Total;
    public bool Lotada => Livres == 0;

    public bool EstaDesatualizada(DateTime agoraUtc) => agoraUtc - RegistradaEmUtc > Validade;
}
