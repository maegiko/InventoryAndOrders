using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

namespace InventoryAndOrders.Endpoints.Orders;

public class CreateOrderEndpoint : Endpoint<CreateOrderRequest, CreateOrderResponse>
{
    private readonly OrderServices _orders;

    public CreateOrderEndpoint(OrderServices orders)
    {
        _orders = orders;
    }

    public override void Configure()
    {
        Post("/checkout");
        AllowAnonymous();

        Description(b => b
            .Produces<CreateOrderResponse>(201)
            .Produces<ApiErrorResponse>(404)
            .Produces<ApiErrorResponse>(409)
            .Produces<ErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ErrorResponse>(
                400,
                """
                If any of the following is true:
                - 'FirstName' is missing or empty
                - 'LastName' is missing or empty
                - 'Email' is missing or empty
                - 'Phone' is missing or empty
                - 'Street' is missing or empty
                - 'City' is missing or empty
                - 'Postcode' is missing or empty
                - 'Country is missing or empty
                - No items are present in order
                - 'ProductId' or 'Quantity' are negative
                """,
                "application/json"
            );
            s.Response<ApiErrorResponse>(
                404,
                "Product is not found."
           );
            s.Response<ApiErrorResponse>(
                409,
                """
                If any of the following is true:
                - Product is out of stock
                - Product is unavailable
                """
            );
        });
    }

    public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
    {
        try
        {
            CreateOrderResponse created = _orders.CreateOrder(req);
            HttpContext.Response.Headers.Location = $"/orders/{created.OrderNumber}";
            await Send.ResponseAsync(created, StatusCodes.Status201Created, ct);
        }
        catch (ProductNotFoundException ex)
        {
            await Send.ResultAsync(
                TypedResults.NotFound(new ApiErrorResponse { Message = ex.Message }));
        }
        catch (ProductUnavailableException ex)
        {
            await Send.ResultAsync(
                TypedResults.Conflict(new ApiErrorResponse { Message = ex.Message }));
        }
        catch (ProductStockException ex)
        {
            await Send.ResultAsync(
                TypedResults.Conflict(new ApiErrorResponse { Message = ex.Message }));
        }
    }
}