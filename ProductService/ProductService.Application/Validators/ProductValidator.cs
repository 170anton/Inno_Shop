using FluentValidation;
using ProductService.Domain.Entities;

namespace ProductService.Application.Validators
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be positive.");

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("Product must have a valid user id.");
        }
    }
}