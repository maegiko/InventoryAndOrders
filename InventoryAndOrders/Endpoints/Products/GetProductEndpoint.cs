using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class GetProductEndpoint : Endpoint<GetProductByIdRequest, object>
{
    private readonly ProductServices _products;

    public GetProductEndpoint(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Get("/products/{id}");
        AllowAnonymous();

        Description(b => b
            .Produces<ApiErrorResponse>(404)
            .Produces<Product>(200)
        );
    }

    public override async Task HandleAsync(GetProductByIdRequest req, CancellationToken ct)
    {
        Product? product = _products.Get(req.Id);

        if (product is null)
        {
            await Send.ResponseAsync(
                new ApiErrorResponse { Message = "Product was not found." },
                StatusCodes.Status404NotFound,
                ct
            );

            return;
        }

        await Send.OkAsync(product, ct);
    }
}