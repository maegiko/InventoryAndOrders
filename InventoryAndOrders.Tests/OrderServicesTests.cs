using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAndOrders.Tests;

public class OrderServicesTests
{
    [Fact]
    public void GetOrder_WithValidCredentials_ReturnsExpectedOrder()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        ProductServices products = scope.ServiceProvider.GetRequiredService<ProductServices>();
        OrderServices orders = scope.ServiceProvider.GetRequiredService<OrderServices>();

        Product createdProduct = products.Create(ApiTestData.NewProduct(name: "Service Product", totalStock: 8));
        CreateOrderResponse createdOrder = orders.CreateOrder(ApiTestData.NewOrder(createdProduct.Id, quantity: 3));

        GetOrderResponse result = orders.GetOrder(createdOrder.OrderNumber, createdOrder.GuestToken);

        Assert.Equal(createdOrder.OrderNumber, result.OrderNumber);
        Assert.Equal("Pending", result.OrderStatus);
        Assert.Equal("Unpaid", result.PaymentStatus);
        Assert.Single(result.Items);
        Assert.Equal("Service Product", result.Items[0].ProductName);
        Assert.Equal(3, result.Items[0].Quantity);
    }

    [Fact]
    public void GetOrder_WithInvalidCredentials_ThrowsInvalidOrderException()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        ProductServices products = scope.ServiceProvider.GetRequiredService<ProductServices>();
        OrderServices orders = scope.ServiceProvider.GetRequiredService<OrderServices>();

        Product createdProduct = products.Create(ApiTestData.NewProduct(name: "Service Product", totalStock: 4));
        CreateOrderResponse createdOrder = orders.CreateOrder(ApiTestData.NewOrder(createdProduct.Id, quantity: 1));

        Assert.Throws<InvalidOrderException>(() =>
            orders.GetOrder(createdOrder.OrderNumber, "INVALID-TOKEN"));
    }
}
