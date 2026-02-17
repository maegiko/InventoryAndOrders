namespace InventoryAndOrders.Services.Exceptions;

public class InvalidOrderException : Exception
{
    public InvalidOrderException()
        : base("invalid Order Credentials.")
    {
    }
}