using FastEndpoints;
using InventoryAndOrders.Models;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class CreateProductEndpoint : Endpoint<CreateProductRequest, Product>
{
    private readonly ProductServices _products;

    public CreateProductEndpoint(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Post("/products");
        Roles("staff");

        Description(b => b
            .Produces<Product>(201)
            .Produces<ErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ErrorResponse>(
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

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        Product created = _products.Create(req);
        await Send.CreatedAtAsync<GetProductEndpoint>(
            new { id = created.Id },
            created,
            cancellation: ct
        );
    }
}