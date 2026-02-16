using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;

namespace InventoryAndOrders.Tests;

internal static class ApiTestData
{
    internal static CreateProductRequest NewProduct(
        string name = "Test Product",
        decimal price = 9.99m,
        int totalStock = 10) =>
        new()
        {
            Name = name,
            Price = price,
            TotalStock = totalStock
        };

    internal static CreateOrderRequest NewOrder(int productId, int quantity = 1) =>
        new()
        {
            CustomerInfo = new CustomerInfo
            {
                FirstName = "Alex",
                LastName = "Tester",
                Email = "alex@example.com",
                Phone = "1234567890"
            },
            Address = new Address
            {
                Street = "123 Test Street",
                City = "Testville",
                Postcode = "12345",
                Country = "US"
            },
            Items =
            [
                new CreateOrderItem
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            ]
        };
}
