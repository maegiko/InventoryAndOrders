using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;
using System.Text.Json;

namespace InventoryAndOrders.Tests;

public class OrderEndpointsTests
{
    [Fact]
    public async Task CreateOrder_Returns201_WithLocation_AndGuestToken()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Book", totalStock: 5));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/checkout",
            ApiTestData.NewOrder(product.Id, quantity: 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOrderResponse? created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created.OrderNumber));
        Assert.False(string.IsNullOrWhiteSpace(created.GuestToken));
        Assert.Equal("Pending", created.OrderStatus);
        Assert.Equal("Unpaid", created.PaymentStatus);
        Assert.Equal($"/orders/{created.OrderNumber}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CreateOrder_WithUnknownProduct_Returns404()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/checkout",
            ApiTestData.NewOrder(productId: 999999, quantity: 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_Returns409()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Monitor", totalStock: 1));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/checkout",
            ApiTestData.NewOrder(product.Id, quantity: 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidBody_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        CreateOrderRequest invalid = ApiTestData.NewOrder(productId: 1, quantity: 1);
        invalid.CustomerInfo.FirstName = "";
        invalid.Items = [];

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders/checkout", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WithValidCredentials_Returns200_AndOrderPayload()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Laptop", totalStock: 5));
        CreateOrderResponse created = await CreateOrderAsync(client, ApiTestData.NewOrder(product.Id, quantity: 2));

        using HttpRequestMessage req = new(HttpMethod.Get, $"/orders/{created.OrderNumber}");
        req.Headers.Add("X-Guest-Token", created.GuestToken);

        HttpResponseMessage response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        GetOrderResponse? order = await response.Content.ReadFromJsonAsync<GetOrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(created.OrderNumber, order.OrderNumber);
        Assert.Equal("Pending", order.OrderStatus);
        Assert.Equal("Unpaid", order.PaymentStatus);
        Assert.Single(order.Items);
        Assert.Equal("Laptop", order.Items[0].ProductName);
        Assert.Equal(2, order.Items[0].Quantity);
    }

    [Fact]
    public async Task GetOrder_WithMissingGuestToken_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/orders/ORD-000001");
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
    }

    [Fact]
    public async Task GetOrder_WithBlankOrderNumber_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage req = new(HttpMethod.Get, "/orders/%20");
        req.Headers.Add("X-Guest-Token", "SOME-TOKEN");

        HttpResponseMessage response = await client.SendAsync(req);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
    }

    [Fact]
    public async Task GetOrder_WithInvalidCredentials_Returns404_AndApiError()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product product = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Tablet", totalStock: 5));
        CreateOrderResponse created = await CreateOrderAsync(client, ApiTestData.NewOrder(product.Id, quantity: 1));

        using HttpRequestMessage req = new(HttpMethod.Get, $"/orders/{created.OrderNumber}");
        req.Headers.Add("X-Guest-Token", "WRONG-TOKEN");

        HttpResponseMessage response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiErrorResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    private static async Task<Product> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        return product;
    }

    private static async Task<CreateOrderResponse> CreateOrderAsync(HttpClient client, CreateOrderRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders/checkout", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOrderResponse? order = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(order);
        return order;
    }
}
