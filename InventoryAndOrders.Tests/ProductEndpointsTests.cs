using System.Net;
using System.Net.Http.Json;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Models;

namespace InventoryAndOrders.Tests;

public class ProductEndpointsTests
{
    [Fact]
    public async Task CreateProduct_Returns201_And_ProductPayload()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/products",
            ApiTestData.NewProduct(name: "Desk Lamp"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        Product? product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        Assert.True(product.Id > 0);
        Assert.Equal("Desk Lamp", product.Name);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidPayload_Returns400()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/products",
            ApiTestData.NewProduct(name: "", price: -1m, totalStock: -2));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListProducts_ReturnsCreatedProducts()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        await CreateProductAsync(client, ApiTestData.NewProduct(name: "Keyboard"));
        await CreateProductAsync(client, ApiTestData.NewProduct(name: "Mouse"));

        Product[]? products = await client.GetFromJsonAsync<Product[]>("/products");

        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Keyboard");
        Assert.Contains(products, p => p.Name == "Mouse");
    }

    [Fact]
    public async Task GetProduct_ById_Works_And_UnknownIdReturns404()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        Product created = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Chair"));

        HttpResponseMessage okResponse = await client.GetAsync($"/products/{created.Id}");
        HttpResponseMessage missingResponse = await client.GetAsync("/products/999999");

        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesName()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        Product created = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Old Name"));

        HttpResponseMessage patchResponse = await client.PatchAsJsonAsync(
            $"/products/{created.Id}",
            new { name = "New Name" });

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        Product? updated = await patchResponse.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task DeleteProduct_MarksProductDeleted()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await TestAuthHelper.AuthenticateAsStaffAsync(client);

        Product created = await CreateProductAsync(client, ApiTestData.NewProduct(name: "Delete Me"));

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/products/{created.Id}");
        HttpResponseMessage getAfterDeleteResponse = await client.GetAsync($"/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    private static async Task<Product> CreateProductAsync(HttpClient client, CreateProductRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(product);
        return product;
    }
}
