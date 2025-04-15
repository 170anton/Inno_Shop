using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
    {
        public UpdateUserModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email cannot be empty.")
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => x.Email != null);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .When(x => x.Name != null);

            RuleFor(x => x.Address)
                .MaximumLength(100).WithMessage("Address cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}
