using FluentValidation;

namespace ProductService.Application.Commands
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
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
