using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.Services;
using Microsoft.Data.Sqlite;
using FastEndpoints;
using FastEndpoints.Swagger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddFastEndpoints().SwaggerDocument();

string? connectionString = builder.Configuration.GetConnectionString("inventory");
if (connectionString is null)
{
    throw new Exception("Error: Connection String is not found.");
}

builder.Services.AddSingleton(new Db(connectionString));
builder.Services.AddScoped<ProductServices>();
builder.Services.AddScoped<OrderServices>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    Db db = scope.ServiceProvider.GetRequiredService<Db>();
    using SqliteConnection conn = db.CreateConnection();
    conn.Open();

    conn.Execute("PRAGMA foreign_keys = ON;");

    Schema.EnsureCreated(conn);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

// app.UseHttpsRedirection();
app.UseFastEndpoints();
app.Run();

public partial class Program { }
