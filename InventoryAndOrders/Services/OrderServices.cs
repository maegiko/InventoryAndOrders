using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.Models;
using InventoryAndOrders.DTOs;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using InventoryAndOrders.Enums;
using InventoryAndOrders.Services.Exceptions;

namespace InventoryAndOrders.Services;

public class OrderServices
{
    private readonly Db _db;

    public OrderServices(Db db)
    {
        _db = db;
    }

    public CreateOrderResponse CreateOrder(CreateOrderRequest req)
    {
        using SqliteConnection conn = _db.CreateConnection();
        conn.Open();
        using SqliteTransaction transaction = conn.BeginTransaction();
        string nowUtc = DateTimeOffset.UtcNow.ToString("O");
        string guestToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        // Merge potential duplicates
        List<CreateOrderItem> mergedItems = req.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CreateOrderItem { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();
        IEnumerable<int> productIds = mergedItems.Select(i => i.ProductId);

        try
        {
            string orderSql = @"
                INSERT INTO Orders (
                    GuestToken, CreatedAt, LastEdited, 
                    OrderStatus, PaymentStatus, 
                    ReservationStatus, ReservedAt, 
                    CustomerFirstName, CustomerLastName, CustomerEmail, CustomerPhone, 
                    ShipStreet, ShipCity, ShipPostcode, ShipCountry
                )
                VALUES (
                    @GuestToken, @NowUtc, @NowUtc, 
                    @OrderStatus, @PaymentStatus, @ReservationStatus, 
                    @NowUtc, @FirstName, @LastName, 
                    @Email, @Phone, 
                    @Street, @City, @Postcode, @Country
                );
                SELECT last_insert_rowid();
            ";

            int id = conn.ExecuteScalar<int>(orderSql, new
            {
                GuestToken = guestToken,
                NowUtc = nowUtc,

                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                ReservationStatus = ReservationStatus.Active,

                req.CustomerInfo.FirstName,
                req.CustomerInfo.LastName,
                req.CustomerInfo.Email,
                req.CustomerInfo.Phone,

                req.Address.Street,
                req.Address.City,
                req.Address.Postcode,
                req.Address.Country
            }, transaction);

            string orderNumber = $"ORD-{id:D6}";

            conn.Execute(@"
                UPDATE Orders
                SET OrderNumber = @OrderNumber
                WHERE Id = @Id
            ", new { Id = id, OrderNumber = orderNumber }, transaction);

            // Insert Order Items into DB
            IReadOnlyDictionary<int, Product> products = LoadProductsForLookup(conn, transaction, productIds);
            InsertOrderItems(conn, transaction, id, mergedItems, products);

            decimal totalPrice = CalculateTotalPrice(mergedItems, products);

            string addTotalPriceSql = @"
                UPDATE Orders
                SET TotalPrice = @TotalPrice
                WHERE Id = @Id
            ";

            conn.Execute(addTotalPriceSql, new { Id = id, TotalPrice = totalPrice }, transaction);

            transaction.Commit();

            // Create response object
            return new CreateOrderResponse
            {
                OrderNumber = orderNumber,
                GuestToken = guestToken,
                OrderStatus = OrderStatus.Pending.ToString(),
                PaymentStatus = PaymentStatus.Unpaid.ToString(),
                TotalPrice = totalPrice
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void ValidateOrderItems(
        List<CreateOrderItem> items,
        IReadOnlyDictionary<int, Product> products)
    {
        foreach (CreateOrderItem item in items)
        {
            if (!products.TryGetValue(item.ProductId, out Product? product))
                throw new ProductNotFoundException(item.ProductId);

            if (product.IsDeleted == 1) throw new ProductUnavailableException(item.ProductId);

            if (product.AvailableStock < item.Quantity) throw new ProductStockException(item.ProductId);
        }
    }

    private static void InsertOrderItems(
        SqliteConnection conn,
        SqliteTransaction transaction,
        int orderId,
        List<CreateOrderItem> items,
        IReadOnlyDictionary<int, Product> products)
    {
        ValidateOrderItems(items, products);

        foreach (CreateOrderItem item in items)
        {
            // Reserve Stock
            string reserveSql = @"
                UPDATE Products
                SET ReservedStock = ReservedStock + @Quantity
                WHERE Id = @ProductId AND IsDeleted = 0 AND (TotalStock - ReservedStock) >= @Quantity;
            ";

            int rowsAffected = conn.Execute(
             reserveSql,
             new { ProductId = item.ProductId, Quantity = item.Quantity },
             transaction);

            if (rowsAffected != 1)
            {
                throw new ProductStockException(item.ProductId);
            }

            // Add OrderItems to DB
            string orderItemsSql = @"
                INSERT INTO OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity)
                VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity);
            ";

            conn.Execute(orderItemsSql, new
            {
                OrderId = orderId,
                item.ProductId,
                ProductName = products[item.ProductId].Name,
                UnitPrice = products[item.ProductId].Price,
                item.Quantity
            }, transaction);
        }
    }

    private static IReadOnlyDictionary<int, Product> LoadProductsForLookup(
        SqliteConnection conn,
        SqliteTransaction transaction,
        IEnumerable<int> productIds)
    {
        IEnumerable<Product> products = conn.Query<Product>(@"
            SELECT Id, Name, Price, TotalStock, ReservedStock, IsDeleted
            FROM Products
            WHERE Id IN @Ids;
        ", new { Ids = productIds.ToArray() }, transaction);

        return products.ToDictionary(p => p.Id);
    }

    private static decimal CalculateTotalPrice(
        List<CreateOrderItem> items,
        IReadOnlyDictionary<int, Product> products)
    {
        return items.Sum(i => products[i.ProductId].Price * i.Quantity);
    }

    public GetOrderResponse GetOrder(string orderNumber, string guestToken)
    {
        using SqliteConnection conn = _db.CreateConnection();

        Order? order = conn.QuerySingleOrDefault<Order>(
            "SELECT * FROM Orders WHERE OrderNumber = @OrderNumber AND GuestToken = @GuestToken",
            new { OrderNumber = orderNumber, GuestToken = guestToken }
        );

        if (order is null) throw new InvalidOrderException();

        string getProductsSql = @"
            SELECT ProductName, Quantity FROM OrderItems
            WHERE OrderId = @OrderId
        ";

        List<GetOrderItem> items = conn.Query<GetOrderItem>(
            getProductsSql,
            new { OrderId = order.Id }).ToList();

        return new GetOrderResponse
        {
            OrderNumber = orderNumber,
            OrderStatus = order.OrderStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            Items = items
        };
    }
}