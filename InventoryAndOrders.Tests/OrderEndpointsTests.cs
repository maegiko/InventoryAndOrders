using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;

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

    private static async Task<Product> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        return product;
    }
}
