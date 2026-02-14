using FastEndpoints;
using FluentValidation;
using InventoryAndOrders.DTOs;

namespace InventoryAndOrders.Validators.Products;

public class CreateProductRequestValidator : Validator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be >= 0.");

        RuleFor(x => x.TotalStock)
            .GreaterThanOrEqualTo(0).WithMessage("TotalStock must be >= 0.");
    }
}