using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

namespace InventoryAndOrders.Endpoints.Orders;

public class GetOrderEndpoint : Endpoint<GetOrderRequest, GetOrderResponse>
{
    private readonly OrderServices _orders;

    public GetOrderEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Get("orders/{orderNumber}");
        AllowAnonymous();

        Description(b => b
            .Produces<GetOrderResponse>(200)
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
                - GuestToken is invalid
                """
            );
            s.Response<ErrorResponse>(
                400,
                """
                If any of the following is true:
                - OrderNumber is empty
                - GuestToken is empty
                """
            );
        });
    }

    public override async Task HandleAsync(GetOrderRequest req, CancellationToken ct)
    {
        try
        {
            GetOrderResponse res = _orders.GetOrder(req.OrderNumber, req.GuestToken);
            await Send.OkAsync(res, ct);
        }
        catch (InvalidOrderException ex)
        {
            await Send.ResultAsync(
                TypedResults.NotFound(new ApiErrorResponse { Message = ex.Message }));
        }
    }
}