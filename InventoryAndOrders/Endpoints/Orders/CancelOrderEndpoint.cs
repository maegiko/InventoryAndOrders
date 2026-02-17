using FastEndpoints;
using FluentValidation.Results;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;
using InventoryAndOrders.Validators.Orders;

namespace InventoryAndOrders.Endpoints.Orders;

public class CancelOrderEndpoint : EndpointWithoutRequest<object>
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
            .Produces<ApiErrorResponse>(400)
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
            s.Response<ApiErrorResponse>(
                400,
                """
                If any of the following is true:
                - OrderNumber is empty
                - GuestToken is empty
                """
            );
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        GetOrderRequest req = new()
        {
            OrderNumber = Route<string>("orderNumber") ?? "",
            GuestToken = HttpContext.Request.Headers["X-Guest-Token"].ToString()
        };

        ValidationResult validation = await new GetOrderRequestValidator().ValidateAsync(req, ct);
        if (!validation.IsValid)
        {
            string message = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage).Distinct());
            await Send.ResponseAsync(new ApiErrorResponse { Message = message }, StatusCodes.Status400BadRequest, ct);
            return;
        }

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
