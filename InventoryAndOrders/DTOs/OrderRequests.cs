using InventoryAndOrders.Models;

namespace InventoryAndOrders.DTOs;

public class CreateOrderRequest
{
    public CustomerInfo CustomerInfo { get; set; } = new();
    public Address Address { get; set; } = new();
    public List<CreateOrderItem> Items { get; set; } = new();
}

public class CreateOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}