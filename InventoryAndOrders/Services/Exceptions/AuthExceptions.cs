namespace InventoryAndOrders.Services.Exceptions;

public class PasswordWeakException : Exception
{
    public PasswordWeakException(string? message = null)
        : base(string.IsNullOrWhiteSpace(message) ? "Password is too weak." : message)
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

public class IncorrectDetailsException : Exception
{
    public IncorrectDetailsException()
        : base("Invalid username or password.")
    {
    }
}
