using FastEndpoints;
using FluentValidation;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Validators.Orders;

public class StaffGetOrderRequestValidator : Validator<StaffGetOrderRequest>
{
    public StaffGetOrderRequestValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order Number cannot be missing.");
    }
}