namespace Aquenda.Domain.Comum;

/// <summary>
/// Violação de uma regra de negócio. A API traduz para HTTP 422.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem) { }
}
