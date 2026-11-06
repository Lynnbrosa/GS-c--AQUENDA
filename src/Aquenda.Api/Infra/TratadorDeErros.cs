using Aquenda.Application.Comum;
using Aquenda.Domain.Comum;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Aquenda.Api.Infra;

/// <summary>
/// Converte exceções em respostas ProblemDetails (RFC 7807). Regra de negócio vira 422,
/// parâmetro inválido vira 400, recurso inexistente vira 404. O resto vira 500 sem detalhes internos.
/// O log do ExceptionHandlerMiddleware está desligado no appsettings porque no .NET 8 ele registra
/// como erro até um 404 tratado aqui; os 500 de verdade são registrados por este tratador.
/// </summary>
public sealed class TratadorDeErros : IExceptionHandler
{
    private readonly IProblemDetailsService _problemas;
    private readonly ILogger<TratadorDeErros> _log;

    public TratadorDeErros(IProblemDetailsService problemas, ILogger<TratadorDeErros> log)
    {
        _problemas = problemas;
        _log = log;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception erro, CancellationToken ct)
    {
        var (status, titulo) = erro switch
        {
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Não encontrado"),
            ValidacaoException => (StatusCodes.Status400BadRequest, "Parâmetro inválido"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _log.LogError(erro, "Erro não tratado em {Caminho}", http.Request.Path);

        http.Response.StatusCode = status;
        return await _problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = erro,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = status == StatusCodes.Status500InternalServerError ? "Tente de novo em instantes." : erro.Message
            }
        });
    }
}
