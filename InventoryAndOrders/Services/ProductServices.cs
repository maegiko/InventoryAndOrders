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
        return conn.Query<Product>("SELECT * FROM Products ORDER BY Id");
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

    public Product? Get(int id)
    {
        using SqliteConnection conn = _db.CreateConnection();

        return conn.QuerySingleOrDefault<Product>(
            "SELECT * FROM Products WHERE Id = @Id;",
            new { Id = id }
        );
    }
}