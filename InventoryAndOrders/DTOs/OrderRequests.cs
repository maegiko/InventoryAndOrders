using InventoryAndOrders.Models;
using FastEndpoints;

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

public class GetOrderRequest
{
    [BindFrom("orderNumber")]
    public string OrderNumber { get; set; } = "";
    [FromHeader("X-Guest-Token")]
    public string GuestToken { get; set; } = "";
}