using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class RegisterModelValidator : AbstractValidator<RegisterModel>
    {
        public RegisterModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(3).WithMessage("Password must be at least 3 characters long.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");
                
            RuleFor(x => x.Address)
                .MaximumLength(100).WithMessage("Address cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}