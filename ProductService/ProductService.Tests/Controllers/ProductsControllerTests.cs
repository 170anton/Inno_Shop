using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ProductService.API.Controllers;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using MediatR;
using ProductService.Application.Commands;

namespace ProductService.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _productServiceMock = new Mock<IProductService>();
            _mediatorMock  = new Mock<IMediator>();
            _controller = new ProductsController(_productServiceMock.Object, _mediatorMock.Object);

            var userId = Guid.NewGuid().ToString();
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
        }


        [Fact]
        public async Task GetAll_WithValidUser_ReturnsOkWithOwnProducts()
        {
            var userGuid = Guid.Parse(_controller.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "P1", Price = 1, CreatedByUserId = userGuid, CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "P2", Price = 2, CreatedByUserId = userGuid, CreatedAt = DateTime.UtcNow }
            };

            _productServiceMock
                .Setup(s => s.GetProductsByUserIdAsync(userGuid))
                .ReturnsAsync(products);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetAll_NoUserClaim_ReturnsUnauthorized()
        {
            SetUserContext(null);

            var result = await _controller.GetAll();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetById_OwnedProduct_ReturnsOk()
        {
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Test",
                Price = 10,
                IsAvailable = true,
                CreatedByUserId = userId
            };
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId)).ReturnsAsync(product);

            SetUserContext(userId.ToString());


            var result = await _controller.GetById(productId);


            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(product, ok.Value);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            _productServiceMock.Setup(s => s.GetProductByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product)null);
            SetUserContext(Guid.NewGuid().ToString());

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ForbiddenIfNotOwner()
        {
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(productId))
                        .ReturnsAsync(new Product { Id = productId, CreatedByUserId = Guid.NewGuid() });
            SetUserContext(Guid.NewGuid().ToString());

            var result = await _controller.GetById(productId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_ValidDto_CallsMediatorAndReturnsCreated()
        {
            var userGuid = Guid.Parse(_controller.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var dto = new CreateProductDto { Name = "New", Description = "Desc", Price = 5m, IsAvailable = true };

            var createdProduct = new Product {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                IsAvailable = dto.IsAvailable,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<CreateProductCommand>(cmd =>
                        cmd.Name == dto.Name &&
                        cmd.Description == dto.Description &&
                        cmd.Price == dto.Price &&
                        cmd.IsAvailable == dto.IsAvailable &&
                        cmd.CreatedByUserId == userGuid
                    ),
                    It.IsAny<CancellationToken>()
                ))
                .Returns(Task.FromResult(createdProduct.Id));

            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(createdProduct.Id))
                .ReturnsAsync(createdProduct);


            var result = await _controller.Create(dto);


            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), created.ActionName);

            var returned = Assert.IsType<Product>(created.Value);
            Assert.Equal(createdProduct.Id, returned.Id);

            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Create_UnauthorizedIfNoUser()
        {
            var dto = new CreateProductDto { Name = "P", Description = "D", Price = 1, IsAvailable = true };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };


            var result = await _controller.Create(dto);


            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Update_ValidDto_CallsMediatorAndReturnsNoContent()
        {
            var userGuid = Guid.Parse(_controller.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var productId = Guid.NewGuid();
            var existing = new Product
            {
                Id = productId,
                Name  = "Old",
                Description = "OldDesc",
                Price = 1m,
                IsAvailable = false,
                CreatedByUserId = userGuid,
                CreatedAt = DateTime.UtcNow
            };
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId))
                .ReturnsAsync(existing);

            var dto = new UpdateProductDto
            {
                Name = "Updated",
                Description = null,
                Price = 10m,
                IsAvailable = true
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<UpdateProductCommand>(cmd =>
                        cmd.Id == productId &&
                        cmd.Name == dto.Name &&
                        cmd.Price == dto.Price &&
                        cmd.IsAvailable == dto.IsAvailable
                    ),
                    It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(Unit.Value);


            var result = await _controller.Update(productId, dto);


            Assert.IsType<NoContentResult>(result);
            _mediatorMock.Verify(m => m.Send(
                It.IsAny<UpdateProductCommand>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync((Product)null);

            SetUserContext(Guid.NewGuid().ToString());


            var result = await _controller.Update(id, new UpdateProductDto());


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_UnauthorizedIfNoUser()
    {
        var id = Guid.NewGuid();
        _productServiceMock
            .Setup(s => s.GetProductByIdAsync(id))
            .ReturnsAsync(new Product { Id = id, CreatedByUserId = Guid.NewGuid() });

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };


        var result = await _controller.Update(id, new UpdateProductDto());


        Assert.IsType<UnauthorizedResult>(result);
    }

        [Fact]
        public async Task Update_ForbiddenIfNotOwner()
        {
            var id = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(id))
                .ReturnsAsync(new Product { Id = id, CreatedByUserId = ownerId });

            SetUserContext(Guid.NewGuid().ToString());


            var result = await _controller.Update(id, new UpdateProductDto());


            var forbid = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_ValidAndOwner_ReturnsNoContent()
        {
            var owner = Guid.NewGuid();
            var prodId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(prodId))
                        .ReturnsAsync(new Product { Id = prodId, CreatedByUserId = owner });
            _productServiceMock.Setup(s => s.DeleteProductAsync(prodId))
                        .Returns(Task.CompletedTask);
            SetUserContext(owner.ToString());

            var result = await _controller.Delete(prodId);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_UnauthorizedIfNoUser()
        {
            SetUserContext(null);
            var result = await _controller.Delete(Guid.NewGuid());
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            _productServiceMock.Setup(s => s.GetProductByIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((Product)null);
            SetUserContext(Guid.NewGuid().ToString());

            var result = await _controller.Delete(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ForbiddenIfNotOwner()
        {
            var owner = Guid.NewGuid();
            var prodId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.GetProductByIdAsync(prodId))
                        .ReturnsAsync(new Product { Id = prodId, CreatedByUserId = owner });
            SetUserContext(Guid.NewGuid().ToString());

            var result = await _controller.Delete(prodId);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeactivateProductsByUserId_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, true))
                        .Returns(Task.CompletedTask);

            var result = await _controller.DeactivateProductsByUserId(userId);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ActivateProductsByUserId_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(s => s.SetProductsDeletionStatusAsync(userId, false))
                        .Returns(Task.CompletedTask);

            var result = await _controller.ActivateProductsByUserId(userId);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task SearchProducts_ReturnsOkWithResults()
        {
            var criteria = new ProductSearchCriteria { Name = "foo" };
            var products = new[] { new Product { Name = "foo" } };
            _productServiceMock.Setup(s => s.SearchProductsAsync(criteria)).ReturnsAsync(products);

            var result = await _controller.SearchProducts(criteria);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
            Assert.Single(list);
        }




        private void SetUserContext(string? userId)
        {
            var ctx = new DefaultHttpContext();
            if (userId != null)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            }
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
        }
    }
}
