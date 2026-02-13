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

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    Db db = scope.ServiceProvider.GetRequiredService<Db>();
    using SqliteConnection conn = db.CreateConnection();
    conn.Open();

    string sql = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Price REAL NOT NULL,
            IsDeleted INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
            LastEdited TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
            TotalStock INTEGER NOT NULL,
            ReservedStock INTEGER NOT NULL
        );
    ";

    conn.Execute(sql);
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
