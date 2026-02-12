using System.Security.Cryptography.X509Certificates;
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
            IsDeleted INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
            LastEdited TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
            TotalStock INTEGER NOT NULL,
            ReservedStock INTEGER NOT NULL
        );
    ";

    conn.Execute(sql);
}

// Endpoints

/// <summary>
/// Get a list of all the available products>
/// </summary>
app.MapGet("/products", (ProductServices products) =>
{
    return Results.Ok(products.List());
});

/// <summary>
/// Add a product to the database
/// </summary>
app.MapPost("/products", (NewProductRequest req, ProductServices products) =>
{
    // Error checking
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name cannot be empty.");
    if (req.Price < 0) return Results.BadRequest("Price must be >= 0.");
    if (req.TotalStock < 0) return Results.BadRequest("TotalStock must be >= 0.");

    Product created = products.Add(req);
    return Results.Created($"/products/{created.Id}", created);
});

/// <summary>
/// View the details of a single product
/// </summary>
app.MapGet("/products/{id}", (int id, ProductServices products) =>
{
    Product? product = products.Get(id);

    if (product is null) return Results.NotFound(new { message = "Product was not found." });

    return Results.Ok(product);
});

/// <summary>
/// Mark a product as deleted
/// </summary>
app.MapDelete("/products/{id}", (int id, ProductServices products) =>
{
    bool isDeleted = products.Delete(id);

    if (!isDeleted)
    {
        return Results.NotFound(new { message = "Product was not found." });
    }
    else
    {
        return Results.NoContent();
    }
});

/// <summary>
/// Update the name and/or price of a product
/// </summary>
app.MapPatch("/products/{id}", (int id, ProductServices products, PatchProductRequest req) =>
{   
    // Error Handling
    if (req.Name is null && req.Price is null) 
        return Results.BadRequest("At least one field must be provided for an update.");
    if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name)) 
        return Results.BadRequest("Name cannot be empty.");
    if (req.Price is not null && req.Price < 0)
        return Results.BadRequest("Price must be >= 0.");

    Product? product = products.Update(id, req);

    if (product is null)
    {
        return Results.NotFound(new { message = "Product was not found." });
    }
    else
    {
        return Results.Ok(product);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.Run();
