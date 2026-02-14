using FastEndpoints;

namespace InventoryAndOrders.DTOs;

public class CreateProductRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int TotalStock { get; set; }
}

public class UpdateProductRequest
{
    [BindFrom("id")]
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
}

public class GetProductByIdRequest
{
    [BindFrom("id")]
    public int Id { get; set; }
}