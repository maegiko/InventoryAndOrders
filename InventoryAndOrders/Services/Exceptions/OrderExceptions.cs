namespace InventoryAndOrders.Services.Exceptions;

public class InvalidOrderException : Exception
{
    public InvalidOrderException()
        : base("Invalid Order Credentials.")
    {
    }
}

public class OrderStatusException : Exception
{
    public string OrderNumber { get; }
    public OrderStatusException(string orderNumber)
        : base($"Order: {orderNumber} is unable to be cancelled.")
    {
        OrderNumber = orderNumber;
    }
}

public class OrderCancelException : Exception
{
    public OrderCancelException()
        : base("Unable to cancel order due to inventory conflict.")
    {
    }
}