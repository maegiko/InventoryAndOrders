using InventoryAndOrders.Data;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

    [Fact]
    public void Login_WithValidCredentials_ReturnsTokenWithExpectedClaims()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();

        RegisterResponse created = auth.Register("service-login-user", "service-login@example.com", "ValidPass123!");
        LoginResponse login = auth.Login("SERVICE-LOGIN-USER", "ValidPass123!");

        Assert.True(login.Success);
        Assert.Equal("Login successful.", login.Message);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.True(login.ExpiresAt > DateTime.UtcNow);

        JwtSecurityTokenHandler tokenHandler = new();
        JwtSecurityToken token = tokenHandler.ReadJwtToken(login.Token);

        Assert.Equal("InventoryAndOrders", token.Issuer);
        Assert.Contains("InventoryAndOrders.Client", token.Audiences);
        Assert.Equal(created.Id.ToString(), token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("service-login-user", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("staff", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void Login_WithWrongPassword_ThrowsIncorrectDetailsException()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();
        auth.Register("service-wrong-pass", "service-wrong-pass@example.com", "ValidPass123!");

        Assert.Throws<IncorrectDetailsException>(() =>
            auth.Login("service-wrong-pass", "BadPassword123!"));
    }

    [Fact]
    public void Login_WithUnknownUsername_ThrowsIncorrectDetailsException()
    {
        using TestApiFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        AuthServices auth = scope.ServiceProvider.GetRequiredService<AuthServices>();

        Assert.Throws<IncorrectDetailsException>(() =>
            auth.Login("not-found-user", "AnyPassword123!"));
    }
}
