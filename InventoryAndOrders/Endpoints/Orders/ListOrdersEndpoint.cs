using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints;

public class ListOrdersEndpoint: EndpointWithoutRequest<IEnumerable<StaffOrderResponse>>
{
    private readonly OrderServices _orders;

    public ListOrdersEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Get("/orders");
        Roles("staff");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(_orders.ListOrders(), ct);
    }
}