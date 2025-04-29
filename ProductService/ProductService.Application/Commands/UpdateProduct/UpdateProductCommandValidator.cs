using FluentValidation;

namespace ProductService.Application.Commands
{
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .When(x => x.Name is not null);

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be positive.")
                .When(x => x.Price.HasValue);

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("Product must have a valid user id.");
        }
    }
}
