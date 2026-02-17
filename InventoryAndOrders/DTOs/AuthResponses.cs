namespace InventoryAndOrders.DTOs;

public class RegisterResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}