namespace Aquenda.Infrastructure.Mqtt;

/// <summary>Seção "Mqtt" do appsettings. Contrato do grupo: broker.hivemq.com:1883.</summary>
public sealed class MqttOptions
{
    public const string Secao = "Mqtt";

    public bool Habilitado { get; set; }
    public string Host { get; set; } = "broker.hivemq.com";
    public int Porta { get; set; } = 1883;

    /// <summary>"+" assina todas as casas: aquenda/casa/1/vagas, aquenda/casa/2/vagas...</summary>
    public string Topico { get; set; } = "aquenda/casa/+/vagas";
    public string ClientIdPrefixo { get; set; } = "aquenda-api";
    public int ReconexaoMaximaSegundos { get; set; } = 60;
}
