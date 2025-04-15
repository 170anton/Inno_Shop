using System;
using FluentValidation.TestHelper;
using UserService.Application.DTOs;
using UserService.Application.Validators;
using Xunit;

namespace UserService.Tests.Validators
{
    public class RegisterModelValidatorTests
    {
        private readonly RegisterModelValidator _validator;

        public RegisterModelValidatorTests()
        {
            _validator = new RegisterModelValidator();
        }

        [Fact]
        public void RegisterModelValidator_ValidModel_NoValidationErrors()
        {
            var model = new RegisterModel 
            {
                Email = "test@example.com",
                Password = "Password123",
                Name = "Test User",
                Address = "Test Address"
            };


            var result = _validator.TestValidate(model);


            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void RegisterModelValidator_EmptyEmailAndName_ReturnsValidationErrors()
        {
            var model = new RegisterModel 
            {
                Email = "",
                Password = "Password123",
                Name = "",
                Address = "Test Address"
            };


            var result = _validator.TestValidate(model);


            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }
    }

    public class ResetPasswordModelValidatorTests
    {
        private readonly ResetPasswordModelValidator _validator;

        public ResetPasswordModelValidatorTests()
        {
            _validator = new ResetPasswordModelValidator();
        }

        [Fact]
        public void ResetPasswordModelValidator_ValidModel_NoValidationErrors()
        {
            var model = new ResetPasswordModel
            {
                UserId = "123",
                Token = "valid-token",
                NewPassword = "NewPassword123",
                ConfirmPassword = "NewPassword123"
            };


            var result = _validator.TestValidate(model);


            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ResetPasswordModelValidator_PasswordMismatch_ReturnsValidationError()
        {
            var model = new ResetPasswordModel
            {
                UserId = "123",
                Token = "valid-token",
                NewPassword = "NewPassword123",
                ConfirmPassword = "DifferentPassword"
            };


            var result = _validator.TestValidate(model);


            result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
                  .WithErrorMessage("Passwords do not match.");
        }
    }

    public class UpdateUserModelValidatorTests
    {
        private readonly UpdateUserModelValidator _validator;

        public UpdateUserModelValidatorTests()
        {
            _validator = new UpdateUserModelValidator();
        }

        [Fact]
        public void UpdateUserModelValidator_ValidModel_NoValidationErrors()
        {
            var model = new UpdateUserModel
            {
                Email = "update@example.com",
                Name = "Updated User",
                Address = "Updated Address"
            };


            var result = _validator.TestValidate(model);


            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateUserModelValidator_InvalidEmail_ReturnsValidationError()
        {
            var model = new UpdateUserModel
            {
                Email = "not-an-email",
                Name = "Updated User",
                Address = "Updated Address"
            };


            var result = _validator.TestValidate(model);


            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("A valid email is required.");
        }
    }

    public class ForgotPasswordModelValidatorTests
    {
        private readonly ForgotPasswordModelValidator _validator;

        public ForgotPasswordModelValidatorTests()
        {
            _validator = new ForgotPasswordModelValidator();
        }

        [Fact]
        public void ForgotPasswordModelValidator_ValidEmail_NoValidationErrors()
        {
            var model = new ForgotPasswordModel { Email = "forgot@example.com" };


            var result = _validator.TestValidate(model);


            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ForgotPasswordModelValidator_InvalidEmail_ReturnsValidationError()
        {
            var model = new ForgotPasswordModel { Email = "invalid-email" };


            var result = _validator.TestValidate(model);


            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("A valid email is required.");
        }
    }
}
