using FastEndpoints;

namespace InventoryAndOrders.DTOs;

public class NewProductRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int TotalStock { get; set; }
}

public class PatchProductRequest
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