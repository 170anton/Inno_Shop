using FluentValidation;
using UserService.Application.DTOs;

namespace UserService.Application.Validators
{
    public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
    {
        public UpdateUserModelValidator()
        {

            // RuleFor(x => x.Email)
            //     .NotEmpty().WithMessage("Email is required.")
            //     .EmailAddress().WithMessage("A valid email is required.");

            // RuleFor(x => x.Name)
            //     .NotEmpty().WithMessage("Name is required.");
            
            // RuleFor(x => x.Address)
            //     .NotEmpty().WithMessage("Address is required.");
        }
    }
}
