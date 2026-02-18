using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace InventoryAndOrders.Swagger;

public sealed class CancelOrderHeaderOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        string path = context.OperationDescription.Path;
        string method = context.OperationDescription.Method;

        if (!string.Equals(method, "post", StringComparison.OrdinalIgnoreCase)) return true;
        if (!path.EndsWith("/orders/{orderNumber}/cancel", StringComparison.OrdinalIgnoreCase)) return true;

        bool exists = context.OperationDescription.Operation.Parameters
            .Any(p => p.Kind == OpenApiParameterKind.Header &&
                      string.Equals(p.Name, "X-Guest-Token", StringComparison.OrdinalIgnoreCase));

        if (exists) return true;

        context.OperationDescription.Operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = "X-Guest-Token",
                Kind = OpenApiParameterKind.Header,
                IsRequired = true,
                Description = "Guest token required to authorize canceling this order.",
                CustomSchema = new JsonSchema { Type = JsonObjectType.String }
            });

        return true;
    }
}
