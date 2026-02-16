namespace InventoryAndOrders.Services.Exceptions;

public class ProductNotFoundException : Exception
{
    public int ProductId { get; }

    public ProductNotFoundException(int productId) : base($"Product {productId} was not found.")
    {
        ProductId = productId;
    }
}

public class ProductUnavailableException : Exception
{
    public int ProductId { get; }

    public ProductUnavailableException(int productId) : base($"Product {productId} is unavailable.")
    {
        ProductId = productId;
    }
}

public class ProductStockException : Exception
{
    public int ProductId { get; }

    public ProductStockException(int productId) : base($"Insufficient stock for product {productId}")
    {
        ProductId = productId;
    }
}