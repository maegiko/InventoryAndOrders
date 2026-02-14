using FastEndpoints;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class ListProductsEndpoint : EndpointWithoutRequest<IEnumerable<Product>>
{
    private readonly ProductServices _products;

    public ListProductsEndpoint(ProductServices products)
    {
        _products = products;
    }

    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(_products.List(), ct);
    }
}