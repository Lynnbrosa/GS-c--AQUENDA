using Aquenda.Application.Comum;
using Aquenda.Domain.Chat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aquenda.Infrastructure.Chat;

/// <summary>
/// Apaga mensagens antigas das salas de tempos em tempos. Conversa de público vulnerável
/// não precisa ficar guardada: menos dado guardado, menos dado para vazar.
/// </summary>
public sealed class LimpezaChatService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ChatOptions _opcoes;
    private readonly ILogger<LimpezaChatService> _log;

    public LimpezaChatService(IServiceScopeFactory scopes, IOptions<ChatOptions> opcoes, ILogger<LimpezaChatService> log)
    {
        _scopes = scopes;
        _opcoes = opcoes.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _opcoes.IntervaloLimpezaMinutos)));
        do
        {
            try
            {
                await LimparAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Falha na limpeza do chat");
            }
        }
        while (await AguardarAsync(timer, stoppingToken));
    }

    /// <summary>Uma rodada de limpeza. Público para os testes.</summary>
    public async Task<int> LimparAsync(CancellationToken ct)
    {
        await using var escopo = _scopes.CreateAsyncScope();
        var chat = escopo.ServiceProvider.GetRequiredService<IChatRepository>();
        var relogio = escopo.ServiceProvider.GetRequiredService<IRelogio>();

        var limite = relogio.AgoraUtc.AddDays(-Math.Max(1, _opcoes.DiasRetencao));
        var apagadas = await chat.ApagarMensagensAnterioresAsync(limite, ct);
        if (apagadas > 0)
            _log.LogInformation("Limpeza do chat: {Quantidade} mensagens antigas apagadas", apagadas);
        return apagadas;
    }

    private static async Task<bool> AguardarAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
