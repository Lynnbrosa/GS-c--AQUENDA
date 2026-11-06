using Aquenda.Domain.ValueObjects;

namespace Aquenda.Tests.Dominio;

public class HorarioFuncionamentoTests
{
    // 2026-11-02 é uma segunda-feira.
    private static DateTime Segunda(int hora, int minuto = 0) => new(2026, 11, 2, hora, minuto, 0);

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(16, 59, true)]
    [InlineData(17, 0, false)]
    [InlineData(8, 59, false)]
    public void Faixa_comum_inclui_abertura_e_exclui_fechamento(int hora, int minuto, bool esperado)
    {
        var h = new HorarioFuncionamento(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        Assert.Equal(esperado, h.Cobre(Segunda(hora, minuto)));
    }

    [Fact]
    public void Faixa_noturna_continua_na_madrugada_do_dia_seguinte()
    {
        var sabado = new HorarioFuncionamento(DayOfWeek.Saturday, new TimeOnly(23, 0), new TimeOnly(5, 0));
        Assert.True(sabado.AtravessaMeiaNoite);
        Assert.True(sabado.Cobre(new DateTime(2026, 11, 7, 23, 30, 0)));  // sábado 23h30
        Assert.True(sabado.Cobre(new DateTime(2026, 11, 8, 4, 59, 0)));   // domingo 4h59
        Assert.False(sabado.Cobre(new DateTime(2026, 11, 8, 5, 0, 0)));   // domingo 5h
        Assert.False(sabado.Cobre(new DateTime(2026, 11, 7, 4, 0, 0)));   // sábado 4h (é a sexta que cobriria)
    }

    [Fact]
    public void Dia_inteiro_cobre_qualquer_hora_do_dia()
    {
        var h = HorarioFuncionamento.TodosOsDias24h().Single(x => x.DiaSemana == DayOfWeek.Monday);
        Assert.True(h.DiaInteiro);
        Assert.True(h.Cobre(Segunda(0)));
        Assert.True(h.Cobre(Segunda(23, 59)));
        Assert.False(h.Cobre(Segunda(12).AddDays(1)));
    }

    [Fact]
    public void Segunda_a_sexta_gera_cinco_faixas_sem_fim_de_semana()
    {
        var dias = HorarioFuncionamento.SegundaASexta(9, 17).Select(h => h.DiaSemana).ToList();
        Assert.Equal(5, dias.Count);
        Assert.DoesNotContain(DayOfWeek.Saturday, dias);
        Assert.DoesNotContain(DayOfWeek.Sunday, dias);
    }
}
