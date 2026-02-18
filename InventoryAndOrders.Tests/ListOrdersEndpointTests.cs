using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAndOrders.Tests;

public class ListOrdersEndpointTests
{
    [Fact]
    public async Task ListOrders_WithoutAuth_Returns401()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_WithInvalidBearerToken_Returns401()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "bad-token");

        HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_WithStaffToken_Returns200_AndOrders()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        using HttpClient client = factory.CreateClient();

        ProductServices products = scope.ServiceProvider.GetRequiredService<ProductServices>();
        OrderServices orders = scope.ServiceProvider.GetRequiredService<OrderServices>();

        var product = products.Create(ApiTestData.NewProduct(name: "List Orders Product", totalStock: 10));
        _ = orders.CreateOrder(ApiTestData.NewOrder(product.Id, quantity: 2));

        string token = await RegisterAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync("/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        StaffOrderResponse[]? payload = await response.Content.ReadFromJsonAsync<StaffOrderResponse[]>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload);
        Assert.Contains(payload, o => o.OrderNumber.StartsWith("ORD-", StringComparison.Ordinal));
        Assert.Contains(payload, o => o.OrderStatus == "Pending");
        Assert.Contains(payload, o => o.PaymentStatus == "Unpaid");
        Assert.Contains(payload, o => o.ReservationStatus == "Active");
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string username = $"staff-{suffix}";
        string email = $"staff-{suffix}@example.com";
        string password = "ValidPass123!";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        LoginResponse? login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        return login.Token;
    }
}
