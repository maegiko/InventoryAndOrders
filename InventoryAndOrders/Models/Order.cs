using InventoryAndOrders.Enums;

namespace InventoryAndOrders.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string GuestToken { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public OrderStatus OrderStatus { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }

    public ReservationStatus ReservationStatus { get; set; }
    public DateTime? ReservedAt { get; set; }

    public CustomerInfo CustomerInfo { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();

    public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
}