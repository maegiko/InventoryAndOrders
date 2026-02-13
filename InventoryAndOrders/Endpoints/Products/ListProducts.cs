using FastEndpoints;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints.Products;

public class ListProducts : EndpointWithoutRequest<IEnumerable<Product>>
{
    private readonly ProductServices _products;

    public ListProducts(ProductServices products)
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