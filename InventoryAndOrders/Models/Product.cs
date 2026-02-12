namespace InventoryAndOrders.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int IsDeleted { get; set; } = 0;
    public int TotalStock { get; set; }
    public int ReservedStock { get; set; }

    public int AvailableStock => TotalStock - ReservedStock;
}