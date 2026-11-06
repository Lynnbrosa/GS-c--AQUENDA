using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aquenda.Api.Infra;

/// <summary>
/// Protege rotas de escrita usadas por quem opera uma casa ou ONG.
/// Exige o cabeçalho X-Chave-Operador igual a Seguranca:ChaveOperador.
/// Sem chave configurada, só libera em Development.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ChaveOperadorAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string Cabecalho = "X-Chave-Operador";
    public const string Configuracao = "Seguranca:ChaveOperador";

    public Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var servicos = contexto.HttpContext.RequestServices;
        var esperada = servicos.GetRequiredService<IConfiguration>()[Configuracao];

        if (string.IsNullOrEmpty(esperada))
        {
            if (!servicos.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                contexto.Result = Problema(StatusCodes.Status503ServiceUnavailable,
                    "Escrita desligada: configure Seguranca__ChaveOperador no servidor.");
            return Task.CompletedTask;
        }

        var recebida = contexto.HttpContext.Request.Headers[Cabecalho].ToString();
        if (!IguaisEmTempoConstante(recebida, esperada))
            contexto.Result = Problema(StatusCodes.Status401Unauthorized, $"Cabeçalho {Cabecalho} ausente ou inválido.");

        return Task.CompletedTask;
    }

    private static bool IguaisEmTempoConstante(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));

    private static ObjectResult Problema(int status, string detalhe) =>
        new(new ProblemDetails { Status = status, Title = "Acesso negado", Detail = detalhe }) { StatusCode = status };
}
