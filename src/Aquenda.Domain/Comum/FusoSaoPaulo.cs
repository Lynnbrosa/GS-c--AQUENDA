namespace Aquenda.Domain.Comum;

/// <summary>
/// Converte horários UTC para o horário de São Paulo. O Brasil não tem horário
/// de verão desde 2019, então UTC-3 fixo serve de reserva se o sistema não tiver tzdata.
/// </summary>
public static class FusoSaoPaulo
{
    private static readonly TimeZoneInfo Fuso = Carregar();

    private static TimeZoneInfo Carregar()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }

    public static DateTime ParaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Fuso);
}
