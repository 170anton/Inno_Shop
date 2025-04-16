using System;
using FluentValidation.TestHelper;
using ProductService.Application.Validators;
using ProductService.Domain.Entities;
using Xunit;

namespace ProductService.Tests.Validators
{
    public class ProductValidatorTests
    {
        private readonly ProductValidator _validator;

        public ProductValidatorTests()
        {
            _validator = new ProductValidator();
        }

        [Fact]
        public void ProductValidator_ValidProduct_NoValidationErrors()
        {
            var product = new Product 
            { 
                Id = Guid.NewGuid(),
                Name = "Valid Product",
                Description = "A valid product description.",
                Price = 10,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };


            var result = _validator.TestValidate(product);


            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ProductValidator_EmptyName_ReturnsValidationError()
        {
            var product = new Product 
            { 
                Id = Guid.NewGuid(),
                Name = "", // пустое имя
                Description = "A description.",
                Price = 10,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };


            var result = _validator.TestValidate(product);


            result.ShouldHaveValidationErrorFor(p => p.Name)
                  .WithErrorMessage("Product name is required.");
        }

        [Fact]
        public void ProductValidator_NegativePrice_ReturnsValidationError()
        {
            var product = new Product 
            { 
                Id = Guid.NewGuid(),
                Name = "Valid Product",
                Description = "A description.",
                Price = -5,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };


            var result = _validator.TestValidate(product);


            result.ShouldHaveValidationErrorFor(p => p.Price)
                  .WithErrorMessage("Price must be positive.");
        }

        [Fact]
        public void ProductValidator_EmptyCreatedByUserId_ReturnsValidationError()
        {
            var product = new Product 
            { 
                Id = Guid.NewGuid(),
                Name = "Valid Product",
                Description = "A description.",
                Price = 10,
                IsAvailable = true,
                CreatedByUserId = Guid.Empty,
                CreatedAt = DateTime.UtcNow
            };


            var result = _validator.TestValidate(product);


            result.ShouldHaveValidationErrorFor(p => p.CreatedByUserId)
                  .WithErrorMessage("Product must have a valid user id.");
        }
    }
}
