using FastEndpoints;
using InventoryAndOrders.Models;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class UpdateProductEndpoint : Endpoint<UpdateProductRequest, object>
{
    private readonly ProductServices _products;

    public UpdateProductEndpoint(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Patch("/products/{id}");
        Roles("staff");

        Description(b => b
            .Produces<Product>(200)
            .Produces<ErrorResponse>(400)
            .Produces<ApiErrorResponse>(404)
        );

        Summary(s =>
        {
            s.Response(
                400,
                """
                If any of the following is true:
                - No fields are provided
                - 'Name' is empty
                - 'Price' is less than 0
                """
            );

            s.Response(
                404,
                "Product does not exist in the Database"
            );
        });
    }

    public override async Task HandleAsync(UpdateProductRequest req, CancellationToken ct)
    {
        Product? product = _products.Update(req.Id, req);

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