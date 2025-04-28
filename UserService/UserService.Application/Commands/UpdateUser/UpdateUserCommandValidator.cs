using FluentValidation;

namespace UserService.Application.Commands
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email cannot be empty.")
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => x.Email != null);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .MaximumLength(30).WithMessage("Name cannot exceed 30 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.Address)
                .MaximumLength(100).WithMessage("Address cannot exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}