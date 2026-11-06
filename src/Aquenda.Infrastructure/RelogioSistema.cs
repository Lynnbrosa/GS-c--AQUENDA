using Aquenda.Application.Comum;

namespace Aquenda.Infrastructure;

public sealed class RelogioSistema : IRelogio
{
    public DateTime AgoraUtc => DateTime.UtcNow;
}
