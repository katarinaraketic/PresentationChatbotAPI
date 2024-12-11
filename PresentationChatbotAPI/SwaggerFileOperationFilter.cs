using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null)
            return;

        var fileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile))
            .ToList();

        if (fileParameters.Any())
        {
            operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = fileParameters.ToDictionary(
                        param => param.Name,
                        param => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }),
                    Required = fileParameters.Select(p => p.Name).ToHashSet()
                }
            };
        }
    }
}
