namespace InventoryAndOrders.DTOs;

public class CreateOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public string GuestToken { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public decimal TotalPrice { get; set; }
}

public class GetOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public List<GetOrderItem> Items { get; set; } = new();
}

public class GetOrderItem
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
}

public class CancelOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public string OrderStatus { get; set; } = "";
}