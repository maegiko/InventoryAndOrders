using InventoryAndOrders.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InventoryAndOrders.Tests;

public sealed class TestApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"inventory-tests-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_dbPath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            Dictionary<string, string?> inMemory = new()
            {
                ["ConnectionStrings:inventory"] = ConnectionString
            };
            configBuilder.AddInMemoryCollection(inMemory);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<Db>();
            services.AddSingleton(new Db(ConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDeleteDb();
    }

    private void TryDeleteDb()
    {
        if (!File.Exists(_dbPath))
            return;

        try
        {
            File.Delete(_dbPath);
        }
        catch
        {
            // best-effort cleanup only
        }
    }
}
