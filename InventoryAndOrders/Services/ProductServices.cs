using Dapper;
using InventoryAndOrders.Models;
using InventoryAndOrders.Data;
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

        string sql = @"
            INSERT INTO Products (Name, Price, TotalStock, ReservedStock)
            VALUES (@Name, @Price, @TotalStock, 0);
            SELECT last_insert_rowid();
        ";

        long id = conn.ExecuteScalar<long>(sql, req);

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
            SET IsDeleted = 1
            WHERE Id = @Id AND IsDeleted = 0;
        ";

        int rowsAffected = conn.Execute(sql, new { Id = id});
        return rowsAffected > 0;
    }
}