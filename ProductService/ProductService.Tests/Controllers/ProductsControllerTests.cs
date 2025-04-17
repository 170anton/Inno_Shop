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
        public async Task GetAll_WithValidUser_ReturnsOkWithOwnProducts()
        {
            var userGuid = Guid.NewGuid();
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "P1", Price = 1, IsAvailable = true, CreatedByUserId = userGuid, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "P2", Price = 2, IsAvailable = true, CreatedByUserId = userGuid, CreatedAt = DateTime.UtcNow }
            };

            _productServiceMock
                .Setup(s => s.GetProductsByUserIdAsync(userGuid))
                .ReturnsAsync(products);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userGuid.ToString()) };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };


            var result = await _controller.GetAll();


            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetAll_NoUserClaim_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };


            var result = await _controller.GetAll();


            Assert.IsType<UnauthorizedResult>(result);
        }


        [Fact]
        public async Task GetById_ProductBelongsToUser_ReturnsOk()
        {
            var productId = Guid.NewGuid();
            var userGuid    = Guid.NewGuid();
            var product     = new Product {
                Id = productId,
                Name = "Test",
                Description = "",
                Price = 10,
                IsAvailable = true,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userGuid.ToString()) };
            _controller.ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            };


            var result = await _controller.GetById(productId);


            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(product, ok.Value);
        }

        [Fact]
        public async Task GetById_ProductNotOwnedOrMissing_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();
            var userGuid  = Guid.NewGuid();

            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync((Product)null);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userGuid.ToString()) };
            _controller.ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
            };


            var result = await _controller.GetById(productId);


            Assert.IsType<NotFoundResult>(result);
        }
        [Fact]


        public async Task Create_ValidDto_ReturnsCreatedAtAction()
        {
            var dto = new CreateProductDto
            {
                Name = "New Product",
                Description = "New Desc",
                Price = 25m,
                IsAvailable = true
            };
            Product capturedProduct = null!;
            _productServiceMock
                .Setup(s => s.AddProductAsync(It.IsAny<Product>()))
                .Callback<Product>(p => capturedProduct = p)
                .Returns(Task.CompletedTask);

            var validUserGuid = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, validUserGuid) };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            _controller.Url = new FakeUrlHelper();


            var result = await _controller.Create(dto);


            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), created.ActionName);

            var returned = Assert.IsType<Product>(created.Value);
            Assert.Equal(capturedProduct.Id, returned.Id);

            Assert.Equal(dto.Name, capturedProduct.Name);
            Assert.Equal(dto.Description, capturedProduct.Description);
            Assert.Equal(dto.Price, capturedProduct.Price);
            Assert.Equal(dto.IsAvailable, capturedProduct.IsAvailable);
            Assert.Equal(Guid.Parse(validUserGuid), capturedProduct.CreatedByUserId);
        }

        [Fact]
        public async Task Create_MissingUserClaim_ReturnsUnauthorized()
        {
            var dto = new CreateProductDto
            {
                Name = "New Product",
                Description = "New Desc",
                Price = 25m,
                IsAvailable = true
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }




        [Fact]
        public async Task Update_ValidDto_ReturnsNoContentAndAppliesChanges()
        {
            // Arrange
            var userGuid = Guid.NewGuid();
            var id = Guid.NewGuid();
            var existing = new Product
            {
                Id = id,
                Name = "OldName",
                Description = "OldDesc",
                Price = 10m,
                IsAvailable = false,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync(existing);

            Product captured = null!;
            _productServiceMock
                .Setup(s => s.UpdateProductAsync(It.IsAny<Product>()))
                .Callback<Product>(p => captured = p)
                .Returns(Task.CompletedTask);

            SetUser(userGuid.ToString());

            var dto = new UpdateProductDto
            {
                Name = "NewName",
                // Description остаётся null, так что не тронется
                Price = 20m,
                // IsAvailable остаётся null
            };

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("NewName", captured.Name);
            Assert.Equal("OldDesc", captured.Description);      // не был перезаписан
            Assert.Equal(20m, captured.Price);
            Assert.False(captured.IsAvailable);                  // не был перезаписан
            Assert.Equal(userGuid, captured.CreatedByUserId);    // не поменялся
        }

        [Fact]
        public async Task Update_ProductNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync((Product)null);

            // need to have *some* user to pass the user-null check, но до него not found
            SetUser(Guid.NewGuid().ToString());

            // Act
            var result = await _controller.Update(id, new UpdateProductDto());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_NoUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new Product { Id = id, CreatedByUserId = Guid.NewGuid() };
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync(existing);

            // НЕ вызываем SetUser => User будет пустой
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.Update(id, new UpdateProductDto());

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Update_UserMismatch_ReturnsForbiddenWithMessage()
        {
            // Arrange
            var id = Guid.NewGuid();
            // existing.CreatedByUserId – один GUID
            var existing = new Product { Id = id, CreatedByUserId = Guid.NewGuid() };
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync(existing);

            // а в токене – другой
            SetUser(Guid.NewGuid().ToString());

            // Act
            var result = await _controller.Update(id, new UpdateProductDto());

            // Assert
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
            Assert.Equal("You are not allowed to modify this product.", obj.Value);
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
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            var otherUserId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, otherUserId) };
            _controller.ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"))
                }
            };
            
            var result = await _controller.Delete(productId);


            var objResult = Assert.IsType<ForbidResult>(result);
            // Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
            // Assert.Equal("You are not allowed to delete this product.", objResult.Value);
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





        private void SetUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
