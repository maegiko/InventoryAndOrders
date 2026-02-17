using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

namespace InventoryAndOrders.Endpoints.Orders;

public class CancelOrderEndpoint : Endpoint<GetOrderRequest, CancelOrderResponse>
{
    private readonly OrderServices _orders;

    public CancelOrderEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Post("orders/{orderNumber}/cancel");
        AllowAnonymous();

        Description(b => b
            .Produces<CancelOrderResponse>(200)
            .Produces<ApiErrorResponse>(404)
            .Produces<ApiErrorResponse>(409)
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
            s.Response<ApiErrorResponse>(
                409,
                """
                If any of the following is true:
                - Order is not in a cancellable state
                - Order has been paid for
                - Conflict when unreserving stock
                - Order state changed concurrently
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
            CancelOrderResponse res = _orders.CancelOrder(req.OrderNumber, req.GuestToken);
            await Send.OkAsync(res, ct);
        }
        catch (InvalidOrderException ex)
        {
            await Send.ResultAsync(
                TypedResults.NotFound(new ApiErrorResponse { Message = ex.Message }));
        }
        catch (OrderStatusException ex)
        {
            await Send.ResultAsync(
                TypedResults.Conflict(new ApiErrorResponse { Message = ex.Message }));
        }
        catch (OrderCancelException ex)
        {
            await Send.ResultAsync(
                TypedResults.Conflict(new ApiErrorResponse { Message = ex.Message }));
        }
    }
}