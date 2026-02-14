using FastEndpoints;
using FluentValidation;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Validators.Products;

public class UpdateProductRequestValidator : Validator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Price is not null)
            .WithMessage("At least one field must be provided for an update.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be >= 0.")
            .When(x => x.Price is not null);
    }
}