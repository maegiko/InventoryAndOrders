using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services.Exceptions;
using Microsoft.Data.Sqlite;
using Zxcvbn;

namespace InventoryAndOrders.Services;

public class AuthServices
{
    private readonly Db _db;

    public AuthServices(Db db)
    {
        _db = db;
    }

    public RegisterResponse Register(string username, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        username = username.Trim().ToLowerInvariant();

        ValidatePassword(password);

        using SqliteConnection conn = _db.CreateConnection();

        string sql = @"
            SELECT Username, Email FROM Accounts
            WHERE Username = @Username OR Email = @Email
            LIMIT 1;
        ";

        var existing = conn.QueryFirstOrDefault<(string Username, string Email)>(
            sql,
            new { Username = username, Email = email }
        );

        if (existing != default) throw new AccountExistsException();

        string nowUtc = DateTimeOffset.UtcNow.ToString("O");

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        string createAccountSQL = @"
            INSERT INTO Accounts (
                Username,
                Email,
                PasswordHash,
                CreatedAt
            )
            VALUES (
                @Username,
                @Email,
                @PasswordHash,
                @NowUtc
            );
            SELECT last_insert_rowid();
        ";

        int id = conn.ExecuteScalar<int>(
            createAccountSQL,
            new { Username = username, Email = email, PasswordHash = passwordHash, NowUtc = nowUtc }
        );

        return conn.QuerySingle<RegisterResponse>(
            "SELECT Id, Username, Email, CreatedAt FROM Accounts WHERE Id = @Id;",
            new { Id = id }
        );
    }

    private static void ValidatePassword(string password)
    {
        Result result = Core.EvaluatePassword(password);
        if (result.Score >= 2) return;

        string message = "Password is too weak.";
        if (result.Feedback is not null)
        {
            List<string> feedback = [];

            if (!string.IsNullOrWhiteSpace(result.Feedback.Warning))
            {
                feedback.Add(result.Feedback.Warning.Trim());
            }

            if (result.Feedback.Suggestions is { Count: > 0 })
            {
                string suggestions = string.Join(" ", result.Feedback.Suggestions.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(suggestions))
                {
                    feedback.Add(suggestions);
                }
            }

            if (feedback.Count > 0)
            {
                message = $"{message} {string.Join(" ", feedback)}";
            }
        }

        throw new PasswordWeakException(message);
    }
}
