using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;

namespace InventoryAndOrders.Endpoints;

public class StaffListOrdersEndpoint : EndpointWithoutRequest<IEnumerable<StaffOrderResponse>>
{
    private readonly OrderServices _orders;

    public StaffListOrdersEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Get("/staff/orders");
        Roles("staff");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(_orders.ListOrders(), ct);
    }
}