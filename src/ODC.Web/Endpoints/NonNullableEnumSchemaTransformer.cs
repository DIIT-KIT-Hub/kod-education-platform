using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ODC.Web.Endpoints;

/// <summary>
/// Keeps enum schemas of the OpenAPI document free of <c>null</c>. Optional query filters are nullable enums (minimal
/// APIs treat a non-nullable value type of <c>[AsParameters]</c> as required), and the generator would otherwise add
/// <c>null</c> to the shared enum component, so every client type of that enum would allow <c>null</c>. Leaving a filter
/// out of the query string is what "no filter" means.
/// </summary>
internal sealed class NonNullableEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        if (schema.Enum is { Count: > 0 } values && values.Any(static v => v is null))
        {
            for (var i = values.Count - 1; i >= 0; i--)
            {
                if (values[i] is null)
                {
                    values.RemoveAt(i);
                }
            }

            if (schema.Type is { } type)
            {
                schema.Type = type & ~JsonSchemaType.Null;
            }
        }

        return Task.CompletedTask;
    }
}
