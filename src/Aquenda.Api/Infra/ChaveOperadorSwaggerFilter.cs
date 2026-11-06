using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Aquenda.Api.Infra;

/// <summary>Mostra o cadeado no Swagger só nas rotas marcadas com [ChaveOperador].</summary>
public sealed class ChaveOperadorSwaggerFilter : IOperationFilter
{
    public const string Esquema = "ChaveOperador";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<ChaveOperadorAttribute>().Any())
            return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = Esquema } }]
                = Array.Empty<string>()
        });
    }
}
