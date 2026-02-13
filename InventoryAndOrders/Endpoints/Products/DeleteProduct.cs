using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class DeleteProduct : Endpoint<GetProductByIdRequest, object>
{
    private readonly ProductServices _products;

    public DeleteProduct(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Delete("/products/{id}");
        AllowAnonymous();

        Description(b => b
            .Produces<ApiErrorResponse>(404)
            .Produces(204)
        );

        Summary(s =>
        {
            s.Response<ApiErrorResponse>(
                404,
                "Product does not exist in Database",
                "application/json"
            );

            s.Response(
                204,
                "Product was successfully deleted"
            );
        });
    }

    public override async Task HandleAsync(GetProductByIdRequest req, CancellationToken ct)
    {
        bool isDeleted = _products.Delete(req.Id);

        if (!isDeleted)
        {
            await Send.ResponseAsync(
                new { message = "Product was not found." },
                StatusCodes.Status404NotFound,
                ct
            );
            return;
        }

        await Send.NoContentAsync(ct);
    }
}