using Dapper;
using Microsoft.Data.Sqlite;

namespace InventoryAndOrders.Data;

public class Db
{
    private readonly string _connectionString;

    public Db(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqliteConnection CreateConnection()
    {
        SqliteConnection conn = new SqliteConnection(_connectionString);
        conn.Open();

        conn.Execute("PRAGMA foreign_keys = ON;");

        return conn;
    }
}