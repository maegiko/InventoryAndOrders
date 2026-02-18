using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Tests;

internal static class TestAuthHelper
{
    public static async Task AuthenticateAsStaffAsync(HttpClient client)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string username = $"staff-{suffix}";
        string email = $"staff-{suffix}@example.com";
        const string password = "ValidPass123!";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        LoginResponse? login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
    }
}
