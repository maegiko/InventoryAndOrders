using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;

namespace InventoryAndOrders.Tests;

public class StaffGetOrderEndpointTests
{
    [Fact]
    public async Task StaffGetOrder_WithoutAuth_Returns401()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/staff/orders/ORD-000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaffGetOrder_WithInvalidBearerToken_Returns401()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "bad-token");

        HttpResponseMessage response = await client.GetAsync("/staff/orders/ORD-000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaffGetOrder_WithBlankOrderNumber_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        HttpResponseMessage response = await client.GetAsync("/staff/orders/%20");
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
    }

    [Fact]
    public async Task StaffGetOrder_WithUnknownOrder_Returns404_AndApiError()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        HttpResponseMessage response = await client.GetAsync("/staff/orders/ORD-999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiErrorResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task StaffGetOrder_ForPendingOrder_Returns200_AndCancelledAtNull()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Staff Get Pending", totalStock: 5));
        CreateOrderResponse created = await CreateOrderAsync(client, ApiTestData.NewOrder(product.Id, quantity: 2));

        HttpResponseMessage response = await client.GetAsync($"/staff/orders/{created.OrderNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        StaffOrderResponse? payload = await response.Content.ReadFromJsonAsync<StaffOrderResponse>();
        Assert.NotNull(payload);
        Assert.Equal(created.OrderNumber, payload.OrderNumber);
        Assert.Equal("Pending", payload.OrderStatus);
        Assert.Equal("Unpaid", payload.PaymentStatus);
        Assert.Equal("Active", payload.ReservationStatus);
        Assert.Null(payload.CancelledAt);
        Assert.Equal(19.98m, payload.TotalPrice);
    }

    [Fact]
    public async Task StaffGetOrder_ForCancelledOrder_Returns200_AndCancelledAtValue()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Staff Get Cancelled", totalStock: 5));
        CreateOrderResponse created = await CreateOrderAsync(client, ApiTestData.NewOrder(product.Id, quantity: 1));

        HttpResponseMessage cancelResponse = await CancelOrderAsync(client, created.OrderNumber, created.GuestToken);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        HttpResponseMessage response = await client.GetAsync($"/staff/orders/{created.OrderNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        StaffOrderResponse? payload = await response.Content.ReadFromJsonAsync<StaffOrderResponse>();
        Assert.NotNull(payload);
        Assert.Equal(created.OrderNumber, payload.OrderNumber);
        Assert.Equal("Cancelled", payload.OrderStatus);
        Assert.Equal("Cancelled", payload.ReservationStatus);
        Assert.NotNull(payload.CancelledAt);
    }

    private static async Task<Product> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        if (client.DefaultRequestHeaders.Authorization is null)
        {
            await TestAuthHelper.AuthenticateAsStaffAsync(client);
        }

        HttpResponseMessage response = await client.PostAsJsonAsync("/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        return product;
    }

    private static async Task<CreateOrderResponse> CreateOrderAsync(HttpClient client, CreateOrderRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders/create", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOrderResponse? order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(order);
        return order;
    }

    private static async Task<HttpResponseMessage> CancelOrderAsync(HttpClient client, string orderNumber, string guestToken)
    {
        using HttpRequestMessage req = new(HttpMethod.Post, $"/orders/{orderNumber}/cancel");
        req.Headers.Add("X-Guest-Token", guestToken);
        return await client.SendAsync(req);
    }
}
