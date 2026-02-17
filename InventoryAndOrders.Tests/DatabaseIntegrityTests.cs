using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.Data;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Enums;
using InventoryAndOrders.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAndOrders.Tests;

public class DatabaseIntegrityTests
{
    [Fact]
    public async Task Schema_CreatesExpectedColumns()
    {
        using TestApiFactory factory = new();
        using HttpClient _ = factory.CreateClient(); // triggers app startup + schema creation

        using SqliteConnection conn = OpenTestConnection(factory);

        string[] productColumns = GetColumnNames(conn, "Products");
        string[] orderColumns = GetColumnNames(conn, "Orders");
        string[] orderItemColumns = GetColumnNames(conn, "OrderItems");

        Assert.Contains("Id", productColumns);
        Assert.Contains("Name", productColumns);
        Assert.Contains("Price", productColumns);
        Assert.Contains("IsDeleted", productColumns);
        Assert.Contains("TotalStock", productColumns);
        Assert.Contains("ReservedStock", productColumns);

        Assert.Contains("Id", orderColumns);
        Assert.Contains("OrderNumber", orderColumns);
        Assert.Contains("GuestToken", orderColumns);
        Assert.Contains("OrderStatus", orderColumns);
        Assert.Contains("PaymentStatus", orderColumns);
        Assert.Contains("ReservationStatus", orderColumns);
        Assert.Contains("TotalPrice", orderColumns);
        Assert.Contains("CustomerFirstName", orderColumns);
        Assert.Contains("ShipPostcode", orderColumns);

        Assert.Contains("Id", orderItemColumns);
        Assert.Contains("OrderId", orderItemColumns);
        Assert.Contains("ProductId", orderItemColumns);
        Assert.Contains("ProductName", orderItemColumns);
        Assert.Contains("UnitPrice", orderItemColumns);
        Assert.Contains("Quantity", orderItemColumns);
    }

    [Fact]
    public async Task CreateOrder_WritesOrderAndItems_AndReservesStock()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product createdProduct = await CreateProductAsync(
            client,
            ApiTestData.NewProduct(name: "Notebook", price: 10m, totalStock: 10));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/create",
            ApiTestData.NewOrder(createdProduct.Id, quantity: 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOrderResponse? createdOrder = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(createdOrder);
        Assert.Equal("Pending", createdOrder.OrderStatus);
        Assert.Equal("Unpaid", createdOrder.PaymentStatus);
        Assert.Equal(30m, createdOrder.TotalPrice);

        using SqliteConnection conn = OpenTestConnection(factory);

        long orderCount = ExecuteScalar<long>(
            conn,
            "SELECT COUNT(*) FROM Orders WHERE OrderNumber = @OrderNumber;",
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.Equal(1, orderCount);

        long orderItemCount = ExecuteScalar<long>(
            conn,
            """
            SELECT COUNT(*)
            FROM OrderItems oi
            JOIN Orders o ON o.Id = oi.OrderId
            WHERE o.OrderNumber = @OrderNumber
              AND oi.ProductId = @ProductId
              AND oi.Quantity = @Quantity;
            """,
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber),
            new SqliteParameter("@ProductId", createdProduct.Id),
            new SqliteParameter("@Quantity", 3));
        Assert.Equal(1, orderItemCount);

        long reservedStock = ExecuteScalar<long>(
            conn,
            "SELECT ReservedStock FROM Products WHERE Id = @ProductId;",
            new SqliteParameter("@ProductId", createdProduct.Id));
        Assert.Equal(3, reservedStock);
    }

    [Fact]
    public async Task CreateOrder_Failure_RollsBackOrderAndStockWrites()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product createdProduct = await CreateProductAsync(
            client,
            ApiTestData.NewProduct(name: "SSD", price: 50m, totalStock: 1));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/orders/create",
            ApiTestData.NewOrder(createdProduct.Id, quantity: 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using SqliteConnection conn = OpenTestConnection(factory);

        long orderCount = ExecuteScalar<long>(conn, "SELECT COUNT(*) FROM Orders;");
        long orderItemCount = ExecuteScalar<long>(conn, "SELECT COUNT(*) FROM OrderItems;");
        long reservedStock = ExecuteScalar<long>(
            conn,
            "SELECT ReservedStock FROM Products WHERE Id = @ProductId;",
            new SqliteParameter("@ProductId", createdProduct.Id));

        Assert.Equal(0, orderCount);
        Assert.Equal(0, orderItemCount);
        Assert.Equal(0, reservedStock);
    }

    [Fact]
    public async Task CreateOrder_DuplicateProductItems_AreMergedIntoSingleOrderItemRow()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product createdProduct = await CreateProductAsync(
            client,
            ApiTestData.NewProduct(name: "Cable", price: 5m, totalStock: 10));

        CreateOrderRequest request = ApiTestData.NewOrder(createdProduct.Id, quantity: 1);
        request.Items.Add(new CreateOrderItem
        {
            ProductId = createdProduct.Id,
            Quantity = 2
        });

        HttpResponseMessage response = await client.PostAsJsonAsync("/orders/create", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateOrderResponse? createdOrder = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(createdOrder);
        Assert.Equal("Pending", createdOrder.OrderStatus);
        Assert.Equal("Unpaid", createdOrder.PaymentStatus);
        Assert.Equal(15m, createdOrder.TotalPrice);

        using SqliteConnection conn = OpenTestConnection(factory);

        long itemRows = ExecuteScalar<long>(
            conn,
            """
            SELECT COUNT(*)
            FROM OrderItems oi
            JOIN Orders o ON o.Id = oi.OrderId
            WHERE o.OrderNumber = @OrderNumber;
            """,
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.Equal(1, itemRows);

        long mergedQuantity = ExecuteScalar<long>(
            conn,
            """
            SELECT oi.Quantity
            FROM OrderItems oi
            JOIN Orders o ON o.Id = oi.OrderId
            WHERE o.OrderNumber = @OrderNumber;
            """,
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.Equal(3, mergedQuantity);
    }

    [Fact]
    public async Task CancelOrder_UpdatesOrderState_AndUnreservesStock()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        Product createdProduct = await CreateProductAsync(
            client,
            ApiTestData.NewProduct(name: "Dock", price: 15m, totalStock: 10));

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/orders/create",
            ApiTestData.NewOrder(createdProduct.Id, quantity: 3));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(createdOrder);

        using HttpRequestMessage cancelReq = new(HttpMethod.Post, $"/orders/{createdOrder.OrderNumber}/cancel");
        cancelReq.Headers.Add("X-Guest-Token", createdOrder.GuestToken);
        HttpResponseMessage cancelResponse = await client.SendAsync(cancelReq);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using SqliteConnection conn = OpenTestConnection(factory);

        long reservedStock = ExecuteScalar<long>(
            conn,
            "SELECT ReservedStock FROM Products WHERE Id = @ProductId;",
            new SqliteParameter("@ProductId", createdProduct.Id));
        Assert.Equal(0, reservedStock);

        long orderStatus = ExecuteScalar<long>(
            conn,
            "SELECT OrderStatus FROM Orders WHERE OrderNumber = @OrderNumber;",
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.Equal((long)OrderStatus.Cancelled, orderStatus);

        long reservationStatus = ExecuteScalar<long>(
            conn,
            "SELECT ReservationStatus FROM Orders WHERE OrderNumber = @OrderNumber;",
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.Equal((long)ReservationStatus.Cancelled, reservationStatus);

        object? cancelledAt = ExecuteScalar<object>(
            conn,
            "SELECT CancelledAt FROM Orders WHERE OrderNumber = @OrderNumber;",
            new SqliteParameter("@OrderNumber", createdOrder.OrderNumber));
        Assert.NotNull(cancelledAt);
        Assert.False(string.IsNullOrWhiteSpace(cancelledAt.ToString()));
    }

    private static SqliteConnection OpenTestConnection(TestApiFactory factory)
    {
        Db db = factory.Services.GetRequiredService<Db>();
        SqliteConnection conn = db.CreateConnection();
        conn.Open();
        return conn;
    }

    private static string[] GetColumnNames(SqliteConnection conn, string tableName)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using SqliteDataReader reader = cmd.ExecuteReader();

        List<string> columns = [];
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
        return columns.ToArray();
    }

    private static T ExecuteScalar<T>(SqliteConnection conn, string sql, params SqliteParameter[] parameters)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (SqliteParameter parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }

        object? value = cmd.ExecuteScalar();
        return (T)Convert.ChangeType(value!, typeof(T));
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
