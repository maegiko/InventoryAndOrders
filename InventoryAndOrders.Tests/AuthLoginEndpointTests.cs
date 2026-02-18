using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Tests;

public class AuthLoginEndpointTests
{
    [Fact]
    public async Task Login_WithValidCredentials_Returns200_AndLoginResponse()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        await RegisterAsync(client, "login-user", "login@example.com", "ValidPass123!");

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "login-user",
            Password = "ValidPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        LoginResponse? payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Login successful.", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.True(payload.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_AndApiError()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        await RegisterAsync(client, "wrong-pass-user", "wrongpass@example.com", "ValidPass123!");

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "wrong-pass-user",
            Password = "not-the-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        ApiErrorResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("invalid username or password", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithUnknownAccount_Returns401_AndApiError()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "missing-user",
            Password = "ValidPass123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        ApiErrorResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("invalid username or password", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_Returns400_ValidationErrorShape()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "",
            Password = ""
        });

        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
        Assert.Contains("Username cannot be empty.", content);
        Assert.Contains("Password cannot be empty.", content);
    }

    [Fact]
    public async Task Login_WithDifferentUsernameCasing_StillAuthenticates()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        await RegisterAsync(client, "CaseUser", "case@example.com", "ValidPass123!");

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = "CASEUSER",
            Password = "ValidPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task RegisterAsync(HttpClient client, string username, string email, string password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
