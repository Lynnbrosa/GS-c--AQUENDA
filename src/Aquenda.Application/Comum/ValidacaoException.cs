namespace Aquenda.Application.Comum;

/// <summary>Parâmetro de entrada inválido. A API traduz para HTTP 400.</summary>
public class ValidacaoException : Exception
{
    public ValidacaoException(string mensagem) : base(mensagem) { }
}
