using Aquenda.Domain.Privacidade;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Aquenda.Api.Infra;

/// <summary>
/// Formatador do console que passa toda linha de log pelo <see cref="MascaradorPii"/> antes de escrever.
/// CPF, telefone, e-mail, nome de registro, segredo e dado de saúde viram marcadores ([CPF], [DADO DE SAÚDE]...),
/// inclusive em exceções. É o "middleware de logs" da entrega de Cyber: o log da VM nunca guarda o dado.
/// </summary>
public sealed class FormatadorLogSemPii : ConsoleFormatter
{
    public const string Nome = "aquenda-sem-pii";

    public FormatadorLogSemPii() : base(Nome) { }

    public override void Write<TState>(in LogEntry<TState> entrada, IExternalScopeProvider? escopos, TextWriter saida)
    {
        var mensagem = entrada.Formatter?.Invoke(entrada.State, entrada.Exception);
        if (mensagem is null && entrada.Exception is null) return;

        saida.Write(Nivel(entrada.LogLevel));
        saida.Write(": ");
        saida.Write(entrada.Category);
        saida.Write('[');
        saida.Write(entrada.EventId.Id);
        saida.WriteLine(']');
        if (!string.IsNullOrEmpty(mensagem))
        {
            saida.Write("      ");
            saida.WriteLine(MascaradorPii.Mascarar(mensagem));
        }
        if (entrada.Exception is not null)
            saida.WriteLine(MascaradorPii.Mascarar(entrada.Exception.ToString()));
    }

    private static string Nivel(LogLevel nivel) => nivel switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => "none",
    };
}
