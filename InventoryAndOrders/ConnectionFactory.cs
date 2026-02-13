using Microsoft.Data.Sqlite;

namespace InventoryAndOrders.Data;

public class Db
{
    private readonly string _connectionString;

    public Db(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqliteConnection CreateConnection() => new SqliteConnection(_connectionString);
}