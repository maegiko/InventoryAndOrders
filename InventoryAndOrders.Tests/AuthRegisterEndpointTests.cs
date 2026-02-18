using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Tests;

public class AuthRegisterEndpointTests
{
    [Fact]
    public async Task Register_WithValidPayload_Returns200_AndRegisterResponse()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        RegisterRequest request = new()
        {
            Username = "TestUser",
            Email = "TestUser@Example.com",
            Password = "ValidPass123!"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        RegisterResponse? created = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("testuser", created.Username);
        Assert.Equal("testuser@example.com", created.Email);

        string rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithDuplicateUsernameOrEmail_Returns409_AndApiError()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        RegisterRequest first = new()
        {
            Username = "dup-user",
            Email = "dup@example.com",
            Password = "FirstPass123!"
        };

        RegisterRequest second = new()
        {
            Username = "DUP-USER",
            Email = "other@example.com",
            Password = "SecondPass123!"
        };

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/auth/register", first);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/auth/register", second);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        ApiErrorResponse? error = await secondResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("already in use", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400_ValidationErrorShape()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        RegisterRequest weak = new()
        {
            Username = "weak-user",
            Email = "weak@example.com",
            Password = "123"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/register", weak);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
        Assert.Contains("Password must be at least 8 characters.", content);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        RegisterRequest invalid = new()
        {
            Username = "",
            Email = "not-an-email",
            Password = ""
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/register", invalid);
        string content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"statusCode\":400", content);
        Assert.Contains("\"errors\"", content);
    }
}
