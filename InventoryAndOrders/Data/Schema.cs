using Dapper;
using Microsoft.Data.Sqlite;

namespace InventoryAndOrders.Data;

public static class Schema
{
    public static void EnsureCreated(SqliteConnection conn)
    {
        string productsSql = @"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                LastEdited TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                TotalStock INTEGER NOT NULL,
                ReservedStock INTEGER NOT NULL
            );
        ";

        conn.Execute(productsSql);

        string ordersSql = @"
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderNumber TEXT UNIQUE,
                GuestToken TEXT NOT NULL UNIQUE,

                CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                LastEdited TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),

                OrderStatus INTEGER NOT NULL,
                CancelledAt TEXT NULL,

                PaymentStatus INTEGER NOT NULL,
                PaidAt TEXT NULL,

                ReservationStatus INTEGER NOT NULL,
                ReservedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                TotalPrice REAL NOT NULL DEFAULT 0,
                CustomerFirstName TEXT NOT NULL,

                CustomerLastName TEXT NOT NULL,
                CustomerEmail TEXT NOT NULL,
                CustomerPhone TEXT NOT NULL,

                ShipStreet TEXT NOT NULL,
                ShipCity TEXT NOT NULL,
                ShipPostcode TEXT NOT NULL,
                ShipCountry TEXT NOT NULL
            );
        ";

        conn.Execute(ordersSql);

        string orderItemsSql = @"
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                UnitPrice REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                UNIQUE(OrderId, ProductId)
            );
        ";

        conn.Execute(orderItemsSql);
    }
}
