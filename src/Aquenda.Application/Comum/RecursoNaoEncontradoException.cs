namespace Aquenda.Application.Comum;

/// <summary>O recurso pedido não existe. A API traduz para HTTP 404.</summary>
public class RecursoNaoEncontradoException : Exception
{
    public RecursoNaoEncontradoException(string recurso, object id)
        : base($"{recurso} {id} não encontrado.") { }
}
