using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

namespace InventoryAndOrders.Endpoints.Orders;

public class StaffGetOrderEndpoint : Endpoint<StaffGetOrderRequest, StaffOrderResponse>
{
    private readonly OrderServices _orders;

    public StaffGetOrderEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Get("/staff/orders/{orderNumber}");
        Roles("staff");

        Description(b => b
            .Produces<StaffOrderResponse>(200)
            .Produces<ApiErrorResponse>(404)
            .Produces<ErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ApiErrorResponse>(
                404,
                """
                If any of the following is true:
                - OrderNumber is invalid
                """
            );
            s.Response<ErrorResponse>(
                400,
                """
                If any of the following is true:
                - OrderNumber is empty
                """
            );
        });
    }

    public override async Task HandleAsync(StaffGetOrderRequest req, CancellationToken ct)
    {
        try
        {
            StaffOrderResponse res = _orders.StaffGetOrder(req.OrderNumber);
            await Send.OkAsync(res, ct);
        }
        catch (InvalidOrderException ex)
        {
            await Send.ResultAsync(
                TypedResults.NotFound(new ApiErrorResponse { Message = ex.Message }));
        }
    }
}