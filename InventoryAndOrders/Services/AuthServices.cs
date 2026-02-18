using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services.Exceptions;
using Microsoft.Data.Sqlite;
using Zxcvbn;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;

namespace InventoryAndOrders.Services;

public class AuthServices
{
    private readonly Db _db;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _expiryHours;

    public AuthServices(Db db, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        JwtOptions jwt = jwtOptions.Value;
        _jwtKey = jwt.Key;
        _jwtIssuer = jwt.Issuer;
        _jwtAudience = jwt.Audience;
        _expiryHours = jwt.ExpiryInHours;
    }

    public RegisterResponse Register(string username, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        username = username.Trim().ToLowerInvariant();

        CheckPasswordStrength(password);

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

    private static void CheckPasswordStrength(string password)
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

    public LoginResponse Login(string username, string password)
    {
        using SqliteConnection conn = _db.CreateConnection();

        username = username.Trim().ToLowerInvariant();

        string sql = @"
            SELECT * FROM Accounts
            WHERE Username = @Username
            LIMIT 1;
        ";

        Account? account = conn.QueryFirstOrDefault<Account>(
            sql,
            new { Username = username }
        );

        if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash)) 
            throw new IncorrectDetailsException();
        
        string token = GenerateJwt(account.Id, account.Username, "staff");
        DateTime expiresAt = DateTime.UtcNow.AddHours(_expiryHours);

        return new LoginResponse
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    private string GenerateJwt(int accountId, string username, string role)
    {
        Claim[] claims = 
        {
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role)
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryInHours { get; set; } = 2;
}
