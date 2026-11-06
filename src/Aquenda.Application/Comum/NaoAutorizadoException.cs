namespace Aquenda.Application.Comum;

/// <summary>Token ausente ou inválido. A API traduz para HTTP 401.</summary>
public class NaoAutorizadoException : Exception
{
    public NaoAutorizadoException(string mensagem) : base(mensagem) { }
}
