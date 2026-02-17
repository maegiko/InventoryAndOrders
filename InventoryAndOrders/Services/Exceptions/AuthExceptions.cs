namespace InventoryAndOrders.Services.Exceptions;

public class PasswordWeakException : Exception
{
    public PasswordWeakException()
        : base("Password is too weak.")
    {
    }
}

public class AccountExistsException : Exception
{
    public AccountExistsException()
        : base("Username/Email already in use.")
    {
    }
}