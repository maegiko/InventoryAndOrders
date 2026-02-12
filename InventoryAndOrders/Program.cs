using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.Models;
using InventoryAndOrders.Services;
using Microsoft.Data.Sqlite;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
            TotalStock INTEGER NOT NULL,
            ReservedStock INTEGER NOT NULL
        );
    ";

    conn.Execute(sql);
}

// Endpoints
app.MapGet("/products", (ProductServices products) =>
{
   return Results.Ok(products.List());
});

app.MapPost("/products", (NewProductRequest req, ProductServices products) =>
{
    // Error checking
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name cannot be empty.");
    if (req.Price < 0) return Results.BadRequest("Price must be >= 0.");
    if (req.TotalStock < 0) return Results.BadRequest("TotalStock must be >= 0.");

    Product created = products.Add(req);
    return Results.Created($"/products/{created.Id}", created);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.Run();
