using InventoryAndOrders.Models;

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

public class StaffOrderResponse
{
    public string OrderNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastEdited { get; set; }
    public string OrderStatus { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string ReservationStatus { get; set; } = "";
    public DateTime ReservedAt { get; set; }
    public CustomerInfo CustomerInfo { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public decimal TotalPrice { get; set; }
}

public class StaffOrderRow
{
    public string OrderNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastEdited { get; set; }
    public int OrderStatus { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int PaymentStatus { get; set; }
    public int ReservationStatus { get; set; }
    public DateTime ReservedAt { get; set; }
    public string CustomerFirstName { get; set; } = "";
    public string CustomerLastName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string ShipStreet { get; set; } = "";
    public string ShipCity { get; set; } = "";
    public string ShipPostcode { get; set; } = "";
    public string ShipCountry { get; set; } = "";
    public decimal TotalPrice { get; set; }
}
