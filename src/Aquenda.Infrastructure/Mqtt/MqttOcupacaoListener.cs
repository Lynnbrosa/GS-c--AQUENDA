using Aquenda.Application.Comum;
using Aquenda.Application.Ocupacao;
using Aquenda.Domain;
using Aquenda.Domain.Comum;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace Aquenda.Infrastructure.Mqtt;

/// <summary>
/// Serviço em segundo plano que assina o tópico do ESP32 e grava cada leitura.
/// Se o broker cair, tenta de novo com espera crescente (2s, 4s, 8s... até o máximo).
/// </summary>
public sealed class MqttOcupacaoListener : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly MqttOptions _opcoes;
    private readonly ILogger<MqttOcupacaoListener> _log;

    public MqttOcupacaoListener(IServiceScopeFactory scopes, IOptions<MqttOptions> opcoes, ILogger<MqttOcupacaoListener> log)
    {
        _scopes = scopes;
        _opcoes = opcoes.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opcoes.Habilitado)
        {
            _log.LogInformation("MQTT desligado (Mqtt:Habilitado=false). Leituras chegam só pelo POST de vagas da API.");
            return;
        }

        var fabrica = new MqttFactory();
        var tentativa = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var cliente = fabrica.CreateMqttClient();
            cliente.ApplicationMessageReceivedAsync += e =>
                ProcessarAsync(e.ApplicationMessage.Topic, e.ApplicationMessage.PayloadSegment, stoppingToken);

            try
            {
                var opcoes = new MqttClientOptionsBuilder()
                    .WithTcpServer(_opcoes.Host, _opcoes.Porta)
                    .WithClientId($"{_opcoes.ClientIdPrefixo}-{Guid.NewGuid():N}")
                    .WithCleanSession()
                    .Build();

                await cliente.ConnectAsync(opcoes, stoppingToken);
                await cliente.SubscribeAsync(
                    fabrica.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(_opcoes.Topico)).Build(),
                    stoppingToken);

                _log.LogInformation("MQTT conectado em {Host}:{Porta}, assinando {Topico}", _opcoes.Host, _opcoes.Porta, _opcoes.Topico);
                tentativa = 0;

                while (cliente.IsConnected && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (MQTTnet.Exceptions.MqttCommunicationException e)
            {
                // Broker fora do ar ou rede bloqueada: não precisa de stack trace.
                _log.LogWarning("Sem conexão com o MQTT ({Host}:{Porta}): {Motivo}", _opcoes.Host, _opcoes.Porta,
                    e.InnerException?.Message ?? e.Message);
            }
            catch (Exception e)
            {
                _log.LogError(e, "Falha inesperada no MQTT ({Host}:{Porta})", _opcoes.Host, _opcoes.Porta);
            }

            if (stoppingToken.IsCancellationRequested) break;
            tentativa++;
            var espera = TimeSpan.FromSeconds(Math.Min(_opcoes.ReconexaoMaximaSegundos, Math.Pow(2, Math.Min(tentativa, 10))));
            _log.LogInformation("Reconectando ao MQTT em {Segundos}s", espera.TotalSeconds);
            try { await Task.Delay(espera, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Processa uma mensagem. Público para os testes chamarem sem broker.</summary>
    public async Task ProcessarAsync(string topico, ArraySegment<byte> payload, CancellationToken ct)
    {
        MensagemVagas mensagem;
        try
        {
            mensagem = MqttMensagemParser.Ler(topico, payload);
        }
        catch (FormatException e)
        {
            _log.LogWarning("Mensagem MQTT ignorada em {Topico}: {Motivo}", topico, e.Message);
            return;
        }

        try
        {
            await using var escopo = _scopes.CreateAsyncScope();
            var ocupacao = escopo.ServiceProvider.GetRequiredService<IOcupacaoService>();
            var vagas = await ocupacao.RegistrarAsync(mensagem.CasaId, mensagem.Leitura, OrigemLeitura.Sensor, ct);
            _log.LogInformation("Casa {CasaId}: {Livres} vagas livres de {Total}", vagas.CasaId, vagas.Livres, vagas.Total);
        }
        catch (Exception e) when (e is DomainException or RecursoNaoEncontradoException)
        {
            _log.LogWarning("Leitura recusada para casa {CasaId}: {Motivo}", mensagem.CasaId, e.Message);
        }
    }
}
