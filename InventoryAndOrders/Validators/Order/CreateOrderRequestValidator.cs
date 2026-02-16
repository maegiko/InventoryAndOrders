using FastEndpoints;
using FluentValidation;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Validators.Orders;

public class CreateOrderRequestValidator : Validator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerInfo.FirstName)
            .NotEmpty().WithMessage("First Name cannot be empty.");

        RuleFor(x => x.CustomerInfo.LastName)
            .NotEmpty().WithMessage("Last Name cannot be empty.");

        RuleFor(x => x.CustomerInfo.Email)
            .NotEmpty().WithMessage("Email cannot be empty.");

        RuleFor(x => x.CustomerInfo.Phone)
            .NotEmpty().WithMessage("Phone cannot be empty.");

        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Street cannot be empty.");

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("City cannot be empty.");

        RuleFor(x => x.Address.Postcode)
            .NotEmpty().WithMessage("Postcode cannot be empty.");

        RuleFor(x => x.Address.Country)
            .NotEmpty().WithMessage("Country cannot be empty.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain items.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemValidator());
    }
}

public class CreateOrderItemValidator : Validator<CreateOrderItem>
{
    public CreateOrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .WithMessage("ProductId must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");
    }
}