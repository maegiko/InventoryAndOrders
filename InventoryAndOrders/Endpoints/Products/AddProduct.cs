using FastEndpoints;
using InventoryAndOrders.Models;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class AddProduct : Endpoint<NewProductRequest, object>
{
    private readonly ProductServices _products;

    public AddProduct(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();

        Description(b => b
            .Produces<Product>(201)
            .Produces<ApiErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ApiErrorResponse>(
                400,
                """
                If any of the following is true:
                - 'Name' is missing or empty
                - 'Price' is less than 0
                - 'TotalStock' is less than 0
                """,
                "application/json"
            );
        });
    }

    public override async Task HandleAsync(NewProductRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            await Send.ResponseAsync(
                new ApiErrorResponse { Message = "Name cannot be empty." },
                StatusCodes.Status400BadRequest,
                ct
            );
            return;
        }

        if (req.Price < 0)
        {
            await Send.ResponseAsync(
                new ApiErrorResponse { Message = "Price must be >= 0." },
                StatusCodes.Status400BadRequest,
                ct
            );
            return;
        }

        if (req.TotalStock < 0)
        {
            await Send.ResponseAsync(
                new ApiErrorResponse { Message = "TotalStock must be >= 0." },
                StatusCodes.Status400BadRequest,
                ct
            );
            return;
        }

        Product created = _products.Add(req);
        await Send.CreatedAtAsync<ViewSingleProduct>(
            new { id = created.Id },
            created,
            cancellation: ct
        );
    }
}