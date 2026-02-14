using InventoryAndOrders.Enums;

namespace InventoryAndOrders.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public OrderStatus Status { get; set; }
    public Address ShippingAddress { get; set; } = new();

    public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
}