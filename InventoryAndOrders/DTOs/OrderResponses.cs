using InventoryAndOrders.Enums;

namespace InventoryAndOrders.DTOs;

public class CreateOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public string GuestToken { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public decimal TotalPrice { get; set; }
}

public class ViewOrderResponse
{
    public string OrderNumber { get; set; }= "";
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public List<ViewOrderItem> Items { get; set; } = new();
}

public class ViewOrderItem
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
}