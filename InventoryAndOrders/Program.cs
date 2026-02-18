using Dapper;
using InventoryAndOrders.Data;
using InventoryAndOrders.Services;
using Microsoft.Data.Sqlite;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryAndOrders.Swagger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddFastEndpoints().SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.OperationProcessors.Add(new CancelOrderHeaderOperationProcessor());
    };
});

string? connectionString = builder.Configuration.GetConnectionString("inventory");
if (connectionString is null)
{
    throw new Exception("Error: Connection String is not found.");
}

string jwtKey = builder.Configuration["Jwt:Key"]!;
string jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
string jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddSingleton(new Db(connectionString));
builder.Services.AddScoped<ProductServices>();
builder.Services.AddScoped<OrderServices>();
builder.Services.AddScoped<AuthServices>();

// JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

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
    app.UseSwaggerUi(c =>
    {
        c.DocExpansion = "list";
        c.DefaultModelsExpandDepth = 0;
    });
}

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
app.Run();

public partial class Program { }
