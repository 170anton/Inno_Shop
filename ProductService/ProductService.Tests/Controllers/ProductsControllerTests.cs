using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ProductService.API.Controllers;
using ProductService.Domain.Entities;
using ProductService.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProductService.Tests.Helpers;
using ProductService.Application.DTOs;

namespace ProductService.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _productServiceMock = new Mock<IProductService>();

            _controller = new ProductsController(_productServiceMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithProducts()
        {
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product 1", Description = "Desc 1", Price = 10, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "Product 2", Description = "Desc 2", Price = 20, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
            };
            _productServiceMock.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);


            var result = await _controller.GetAll();


            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProducts = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
            Assert.Equal(2, returnedProducts.Count());
        }

        [Fact]
        public async Task GetAll_ServiceThrowsException_ThrowsException()
        {
            _productServiceMock.Setup(s => s.GetAllProductsAsync())
                .ThrowsAsync(new Exception("Test exception"));

            await Assert.ThrowsAsync<Exception>(() => _controller.GetAll());
        }

        [Fact]
        public async Task GetById_ProductExists_ReturnsOkWithProduct()
        {
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Test Product", Description = "Desc", Price = 15, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId)).ReturnsAsync(product);


            var result = await _controller.GetById(productId);


            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProduct = Assert.IsType<Product>(okResult.Value);
            Assert.Equal(productId, returnedProduct.Id);
        }

        [Fact]
        public async Task GetById_ProductNotFound_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync((Product)null);


            var result = await _controller.GetById(productId);


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ValidProduct_ReturnsCreatedAtAction()
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "New Product",
                Description = "New Desc",
                Price = 25,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(), 
                CreatedAt = DateTime.UtcNow
            };

            _productServiceMock.Setup(s => s.AddProductAsync(product))
                .Returns(Task.CompletedTask);
            var validUserGuid = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, validUserGuid)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext 
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _controller.Url = new FakeUrlHelper();
            

            var result = await _controller.Create(product);


            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
            var returnedProduct = Assert.IsType<Product>(createdResult.Value);
            Assert.Equal(product.Id, returnedProduct.Id);
        }

        [Fact]
        public async Task Create_MissingUserClaim_ReturnsUnauthorized()
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "New Product",
                Description = "New Desc",
                Price = 25,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() 
            };


            var result = await _controller.Create(product);


            Assert.IsType<UnauthorizedResult>(result);
        }


        [Fact]
        public async Task Update_ValidProduct_ReturnsNoContent()
        {
            var productId = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Existing Product",
                Description = "Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };

            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            _productServiceMock.Setup(s => s.UpdateProductAsync(product))
                .Returns(Task.CompletedTask);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userGuid.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } 
            };


            var result = await _controller.Update(productId, product);


            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_ProductNotFound_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync((Product)null);

            var updateProduct = new Product
            {
                Id = productId,
                Name = "Updated Product",
                Description = "Updated Desc",
                Price = 30,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, updateProduct.CreatedByUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };


            var result = await _controller.Update(productId, updateProduct);


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_IdMismatch_ReturnsBadRequest()
        {
            var routeId = Guid.NewGuid();
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Product",
                Description = "Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };


            var result = await _controller.Update(routeId, product);


            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("ID mismatch", badRequest.Value);
        }

        [Fact]
        public async Task Update_MissingUserClaim_ReturnsUnauthorized()
        {
            var productId = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Product",
                Description = "Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };


            var result = await _controller.Update(productId, product);


            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Update_UserMismatch_ReturnsForbid()
        {
            var productId = Guid.NewGuid();
            var productOwner = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Product",
                Description = "Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = productOwner,
                CreatedAt = DateTime.UtcNow
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")) }
            };

            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                               .ReturnsAsync(product);


            var result = await _controller.Update(productId, product);


            Assert.IsType<ForbidResult>(result);
        }


        [Fact]
        public async Task Delete_ValidIdAndMatchingUser_ReturnsNoContent()
        {
            var productId = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Description = "Test Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };

            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(product);
            _productServiceMock.Setup(s => s.DeleteProductAsync(productId))
                .Returns(Task.CompletedTask);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userGuid.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = principal } 
            };


            var result = await _controller.Delete(productId);


            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_NoUserClaim_ReturnsUnauthorized()
        {
            var productId = Guid.NewGuid();
            _controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext()
            };


            var result = await _controller.Delete(productId);


            var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_ProductNotFound_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync((Product)null);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            _controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")) } 
            };


            var result = await _controller.Delete(productId);


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_UserMismatch_ReturnsForbid()
        {
            var productId = Guid.NewGuid();
            var productOwner = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Test Product",
                Description = "Test Desc",
                Price = 20,
                IsAvailable = true,
                CreatedByUserId = productOwner,
                CreatedAt = DateTime.UtcNow
            };

            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")) }
            };


            var result = await _controller.Delete(productId);


            var forbidResult = Assert.IsType<ForbidResult>(result);
        }


        [Fact]
        public async Task DeactivateProductsByUserId_ValidUserId_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, true))
                .Returns(Task.CompletedTask);


            var result = await _controller.DeactivateProductsByUserId(userId);


            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeactivateProductsByUserId_ServiceThrowsException_ThrowsException()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, true))
                .ThrowsAsync(new Exception("Test exception"));


            await Assert.ThrowsAsync<Exception>(() => _controller.DeactivateProductsByUserId(userId));
        }


        [Fact]
        public async Task ActivateProductsByUserId_ValidUserId_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, false))
                .Returns(Task.CompletedTask);


            var result = await _controller.ActivateProductsByUserId(userId);


            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ActivateProductsByUserId_ServiceThrowsException_ThrowsException()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, false))
                .ThrowsAsync(new Exception("Test exception"));


            await Assert.ThrowsAsync<Exception>(() => _controller.ActivateProductsByUserId(userId));
        }

        [Fact]
        public async Task SearchProducts_ValidCriteria_ReturnsOkWithProducts()
        {
            var criteria = new ProductSearchCriteria
            {
                Name = "Test",
                MinPrice = 10,
                MaxPrice = 100,
                IsAvailable = null
            };

            var products = new List<Product>
            {
                new Product 
                {
                    Id = Guid.NewGuid(), 
                    Name = "Test Product", 
                    Description = "Test Desc", 
                    Price = 50, 
                    IsAvailable = true, 
                    CreatedByUserId = Guid.NewGuid(), 
                    CreatedAt = DateTime.UtcNow
                }
            };

            _productServiceMock.Setup(s => s.SearchProductsAsync(criteria))
                               .ReturnsAsync(products);


            var result = await _controller.SearchProducts(criteria);


            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProducts = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
            Assert.Single(returnedProducts);
        }

        [Fact]
        public async Task SearchProducts_ServiceThrowsException_ThrowsException()
        {
            var criteria = new ProductSearchCriteria
            {
                Name = "Test"
            };

            _productServiceMock.Setup(s => s.SearchProductsAsync(criteria))
                               .ThrowsAsync(new Exception("Service error"));


            await Assert.ThrowsAsync<Exception>(() => _controller.SearchProducts(criteria));
        }
    }
}
