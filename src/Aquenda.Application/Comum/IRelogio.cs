namespace Aquenda.Application.Comum;

/// <summary>Abstração do relógio, para os testes controlarem "agora".</summary>
public interface IRelogio
{
    DateTime AgoraUtc { get; }
}
