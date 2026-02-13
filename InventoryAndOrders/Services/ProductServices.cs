using Dapper;
using InventoryAndOrders.Models;
using InventoryAndOrders.Data;
using InventoryAndOrders.DTOs;
using Microsoft.Data.Sqlite;

namespace InventoryAndOrders.Services;

public class ProductServices
{
    private readonly Db _db;

    public ProductServices(Db db)
    {
        _db = db;
    }

    // Get a list of all products
    public IEnumerable<Product> List()
    {
        using SqliteConnection conn = _db.CreateConnection();
        return conn.Query<Product>("SELECT * FROM Products WHERE IsDeleted = 0 ORDER BY Id");
    }

    // Add a new product to Db
    public Product Add(NewProductRequest req)
    {
        using SqliteConnection conn = _db.CreateConnection();
        string nowUtc = DateTimeOffset.UtcNow.ToString("O");

        string sql = @"
            INSERT INTO Products (Name, Price, TotalStock, ReservedStock, CreatedAt, LastEdited)
            VALUES (@Name, @Price, @TotalStock, 0, @NowUtc, @NowUtc);
            SELECT last_insert_rowid();
        ";

        int id = conn.ExecuteScalar<int>(
            sql,
            new { req.Name, req.Price, req.TotalStock, NowUtc = nowUtc }
        );

        return conn.QuerySingle<Product>(
            "SELECT * FROM Products WHERE Id = @Id;",
            new { Id = id }
        );
    }

    // Get the details of a single product 
    public Product? Get(int id)
    {
        using SqliteConnection conn = _db.CreateConnection();

        return conn.QuerySingleOrDefault<Product>(
            "SELECT * FROM Products WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id }
        );
    }

    // Delete a product from the database
    public bool Delete(int id)
    {
        using SqliteConnection conn = _db.CreateConnection();

        string sql = @"
            UPDATE Products
            SET IsDeleted = 1,
                LastEdited = strftime('%Y-%m-%dT%H:%M:%fZ','now')
            WHERE Id = @Id AND IsDeleted = 0;
        ";

        int rowsAffected = conn.Execute(sql, new { Id = id });
        return rowsAffected > 0;
    }

    // Updates name and/or price of a product
    public Product? Update(int id, PatchProductRequest request)
    {
        using SqliteConnection conn = _db.CreateConnection();

        string sql = @"
            UPDATE Products
            SET Name = COALESCE(@Name, Name),
                Price = COALESCE(@Price, Price),
                LastEdited = strftime('%Y-%m-%dT%H:%M:%fZ','now')
            WHERE Id = @Id 
                AND IsDeleted = 0
                AND (
                    (@Name IS NOT NULL AND @Name <> Name) OR
                    (@Price IS NOT NULL AND @Price <> Price)
                );
        ";

        conn.Execute(sql, new { Id = id, Name = request.Name, Price = request.Price });

        return conn.QuerySingleOrDefault<Product>(
            "SELECT * FROM Products WHERE Id = @Id AND IsDeleted = 0;",
            new { Id = id }
        );
    }
}
