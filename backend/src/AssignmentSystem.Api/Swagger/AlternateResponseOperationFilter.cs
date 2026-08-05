// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AssignmentSystem.Api.Swagger;

/// <summary>
/// Rewrites the response schema for any action carrying
/// <see cref="ProducesAlternateResponseAttribute"/> into a <c>oneOf</c> of the declared
/// types, so a route that answers in more than one shape documents all of them.
/// </summary>
public sealed class AlternateResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var declarations = context.MethodInfo
            .GetCustomAttributes(inherit: true)
            .OfType<ProducesAlternateResponseAttribute>();

        foreach (var declaration in declarations)
        {
            if (!operation.Responses.TryGetValue(
                    declaration.StatusCode.ToString(), out var response))
            {
                continue;
            }

            // Generated through the repository rather than inline, so each type is
            // registered once under components/schemas and referenced here.
            var schemas = declaration.Types
                .Select(type => context.SchemaGenerator.GenerateSchema(
                    type, context.SchemaRepository))
                .ToList();

            if (schemas.Count == 0)
            {
                continue;
            }

            foreach (var content in response.Content.Values)
            {
                content.Schema = new OpenApiSchema { OneOf = schemas };
            }
        }
    }
}
