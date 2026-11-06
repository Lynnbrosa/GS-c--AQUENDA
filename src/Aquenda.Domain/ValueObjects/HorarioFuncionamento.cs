using Aquenda.Domain.Comum;

namespace Aquenda.Domain.ValueObjects;

/// <summary>
/// Uma faixa de funcionamento num dia da semana. Aceita faixas que passam da
/// meia-noite (balada das 23h às 5h) e o dia inteiro (abre == fecha).
/// </summary>
public sealed class HorarioFuncionamento
{
    public DayOfWeek DiaSemana { get; private set; }
    public TimeOnly Abre { get; private set; }
    public TimeOnly Fecha { get; private set; }

    private HorarioFuncionamento() { } // EF Core

    public HorarioFuncionamento(DayOfWeek diaSemana, TimeOnly abre, TimeOnly fecha)
    {
        if (!Enum.IsDefined(diaSemana))
            throw new DomainException("Dia da semana inválido.");
        DiaSemana = diaSemana;
        Abre = abre;
        Fecha = fecha;
    }

    public bool DiaInteiro => Abre == Fecha;
    public bool AtravessaMeiaNoite => Fecha < Abre;

    /// <summary>Diz se este horário cobre um instante já no fuso local.</summary>
    public bool Cobre(DateTime local)
    {
        var hora = TimeOnly.FromDateTime(local);
        if (DiaInteiro)
            return local.DayOfWeek == DiaSemana;
        if (!AtravessaMeiaNoite)
            return local.DayOfWeek == DiaSemana && hora >= Abre && hora < Fecha;

        // Faixa noturna: parte no próprio dia, parte na madrugada seguinte.
        if (local.DayOfWeek == DiaSemana && hora >= Abre)
            return true;
        var ontem = (DayOfWeek)(((int)local.DayOfWeek + 6) % 7);
        return ontem == DiaSemana && hora < Fecha;
    }

    public static IEnumerable<HorarioFuncionamento> Dias(IEnumerable<DayOfWeek> dias, int abre, int fecha) =>
        dias.Select(d => new HorarioFuncionamento(d, new TimeOnly(abre % 24, 0), new TimeOnly(fecha % 24, 0)));

    public static IEnumerable<HorarioFuncionamento> SegundaASexta(int abre, int fecha) =>
        Dias(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, abre, fecha);

    public static IEnumerable<HorarioFuncionamento> TodosOsDias24h() =>
        Enum.GetValues<DayOfWeek>().Select(d => new HorarioFuncionamento(d, TimeOnly.MinValue, TimeOnly.MinValue));

    public override string ToString() =>
        DiaInteiro ? $"{DiaSemana}: 24h" : $"{DiaSemana}: {Abre:HH\\:mm}–{Fecha:HH\\:mm}";
}
