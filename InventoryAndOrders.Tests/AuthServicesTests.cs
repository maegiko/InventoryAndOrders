using InventoryAndOrders.Data;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAndOrders.Tests;

public class AuthServicesTests
{
    [Fact]
    public void Register_WithValidInput_PersistsNormalizedAccount_AndHashedPassword()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();
        Db db = scope.ServiceProvider.GetRequiredService<Db>();

        string rawPassword = "ValidPass123!";
        var created = auth.Register("  MixedCaseUser  ", "  MIXED@Example.com  ", rawPassword);

        Assert.True(created.Id > 0);
        Assert.Equal("mixedcaseuser", created.Username);
        Assert.Equal("mixed@example.com", created.Email);

        using SqliteConnection conn = db.CreateConnection();
        conn.Open();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Username, Email, PasswordHash
            FROM Accounts
            WHERE Id = @Id;
        """;
        cmd.Parameters.AddWithValue("@Id", created.Id);

        using SqliteDataReader reader = cmd.ExecuteReader();
        Assert.True(reader.Read());

        string dbUsername = reader.GetString(0);
        string dbEmail = reader.GetString(1);
        string dbPasswordHash = reader.GetString(2);

        Assert.Equal("mixedcaseuser", dbUsername);
        Assert.Equal("mixed@example.com", dbEmail);
        Assert.NotEqual(rawPassword, dbPasswordHash);
        Assert.StartsWith("$2", dbPasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public void Register_WithDuplicateNormalizedIdentity_ThrowsAccountExistsException()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();

        auth.Register("dup-user", "dup@example.com", "ValidPass123!");

        Assert.Throws<AccountExistsException>(() =>
            auth.Register("DUP-USER", "other@example.com", "AnotherPass123!"));
    }

    [Fact]
    public void Register_WithWeakPassword_ThrowsPasswordWeakException()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();

        PasswordWeakException ex = Assert.Throws<PasswordWeakException>(() =>
            auth.Register("weak-user", "weak@example.com", "password"));

        Assert.Contains("Password is too weak.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
