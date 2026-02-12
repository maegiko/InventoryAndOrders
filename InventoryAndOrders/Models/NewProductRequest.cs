namespace InventoryAndOrders.Models;

public class NewProductRequest
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int TotalStock { get; set; }
}