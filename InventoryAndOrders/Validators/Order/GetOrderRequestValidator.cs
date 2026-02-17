using FastEndpoints;
using FluentValidation;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Validators.Orders;

public class GetOrderRequestValidator : Validator<GetOrderRequest>
{
    public GetOrderRequestValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order Number cannot be missing.");
        
        RuleFor(x => x.GuestToken)
            .NotEmpty().WithMessage("Guest Token cannot be missing.");
    }
}