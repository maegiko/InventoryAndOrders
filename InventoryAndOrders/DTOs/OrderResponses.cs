using InventoryAndOrders.Enums;

namespace InventoryAndOrders.DTOs;

public class CreateOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public string GuestToken { get; set; } = "";
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal TotalPrice { get; set; }
}