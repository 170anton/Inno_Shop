using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ProductService.Application.Services;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using MockQueryable.Moq;

namespace ProductService.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _repositoryMock;
        private readonly Application.Services.ProductService _productService;

        public ProductServiceTests()
        {
            _repositoryMock = new Mock<IProductRepository>();
            _productService = new Application.Services.ProductService(_repositoryMock.Object);
        }

        #region GetAllProductsAsync

        [Fact]
        public async Task GetAllProductsAsync_Valid_ReturnsAllProducts()
        {
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product 1", Description = "Desc 1", Price = 10, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
                new Product { Id = Guid.NewGuid(), Name = "Product 2", Description = "Desc 2", Price = 20, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }
            };
            _repositoryMock.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(products);


            var result = await _productService.GetAllProductsAsync();


            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllProductsAsync_RepositoryThrowsException_PropagatesException()
        {
            _repositoryMock.Setup(r => r.GetAllAsync())
                           .ThrowsAsync(new Exception("Test exception"));


            await Assert.ThrowsAsync<Exception>(() => _productService.GetAllProductsAsync());
        }

        #endregion

        #region GetProductByIdAsync

        [Fact]
        public async Task GetProductByIdAsync_ProductExists_ReturnsProduct()
        {
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Test Product", Description = "Desc", Price = 15, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repositoryMock.Setup(r => r.GetByIdAsync(productId))
                           .ReturnsAsync(product);


            var result = await _productService.GetProductByIdAsync(productId);


            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductDoesNotExist_ReturnsNull()
        {
            var productId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(productId))
                           .ReturnsAsync((Product)null);


            var result = await _productService.GetProductByIdAsync(productId);


            Assert.Null(result);
        }

        #endregion

        #region AddProductAsync

        [Fact]
        public async Task AddProductAsync_ValidProduct_CompletesSuccessfully()
        {
            var product = new Product { Id = Guid.NewGuid(), Name = "New Product", Description = "New Desc", Price = 25, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repositoryMock.Setup(r => r.AddAsync(product))
                           .Returns(Task.CompletedTask);

            
            await _productService.AddProductAsync(product);
        }

        [Fact]
        public async Task AddProductAsync_RepositoryThrowsException_PropagatesException()
        {
            var product = new Product { Id = Guid.NewGuid(), Name = "New Product", Description = "New Desc", Price = 25, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repositoryMock.Setup(r => r.AddAsync(product))
                           .ThrowsAsync(new Exception("Add failed"));


            await Assert.ThrowsAsync<Exception>(() => _productService.AddProductAsync(product));
        }

        #endregion

        #region UpdateProductAsync

        [Fact]
        public async Task UpdateProductAsync_ValidProduct_CompletesSuccessfully()
        {
            var product = new Product { Id = Guid.NewGuid(), Name = "Updated Product", Description = "Updated Desc", Price = 30, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repositoryMock.Setup(r => r.UpdateAsync(product))
                           .Returns(Task.CompletedTask);


            await _productService.UpdateProductAsync(product);
        }

        [Fact]
        public async Task UpdateProductAsync_RepositoryThrowsException_PropagatesException()
        {
            var product = new Product { Id = Guid.NewGuid(), Name = "Updated Product", Description = "Updated Desc", Price = 30, IsAvailable = true, CreatedByUserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
            _repositoryMock.Setup(r => r.UpdateAsync(product))
                           .ThrowsAsync(new Exception("Update failed"));


            await Assert.ThrowsAsync<Exception>(() => _productService.UpdateProductAsync(product));
        }

        #endregion

        #region DeleteProductAsync

        [Fact]
        public async Task DeleteProductAsync_ValidId_CompletesSuccessfully()
        {
            var productId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.DeleteAsync(productId))
                           .Returns(Task.CompletedTask);


            await _productService.DeleteProductAsync(productId);
        }

        [Fact]
        public async Task DeleteProductAsync_RepositoryThrowsException_PropagatesException()
        {
            var productId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.DeleteAsync(productId))
                           .ThrowsAsync(new Exception("Delete failed"));


            await Assert.ThrowsAsync<Exception>(() => _productService.DeleteProductAsync(productId));
        }

        #endregion

        #region SetProductsDeletionStatusAsync

        [Fact]
        public async Task SetProductsDeletionStatusAsync_ValidUserId_SetsIsDeletedCorrectly()
        {
            var userId = Guid.NewGuid();
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product A", IsDeleted = false },
                new Product { Id = Guid.NewGuid(), Name = "Product B", IsDeleted = false }
            };

            _repositoryMock.Setup(r => r.GetProductsByUserIdAsync(userId))
                           .ReturnsAsync(products);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                           .Returns(Task.CompletedTask);


            await _productService.SetProductsDeletionStatusAsync(userId, true);


            Assert.All(products, p => Assert.True(p.IsDeleted));
        }

        [Fact]
        public async Task SetProductsDeletionStatusAsync_SaveChangesThrowsException_PropagatesException()
        {
            var userId = Guid.NewGuid();
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product A", IsDeleted = false }
            };

            _repositoryMock.Setup(r => r.GetProductsByUserIdAsync(userId))
                           .ReturnsAsync(products);
            _repositoryMock.Setup(r => r.SaveChangesAsync())
                           .ThrowsAsync(new Exception("Save failed"));


            await Assert.ThrowsAsync<Exception>(() => _productService.SetProductsDeletionStatusAsync(userId, true));
        }

        #endregion


        #region SearchProductsAsync

        [Fact]
        public async Task SearchProductsAsync_ValidCriteria_ReturnsMatchingProducts()
        {
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Test Product", Description = "Desc", Price = 50, IsAvailable = true },
                new Product { Id = Guid.NewGuid(), Name = "Another Product", Description = "Desc", Price = 80, IsAvailable = false }
            };

            var mockQuery = products.AsQueryable().BuildMockDbSet();
            _repositoryMock.Setup(r => r.GetProductsQuery())
                           .Returns(mockQuery.Object);

            var criteria = new ProductSearchCriteria
            {
                Name = "Test",
                MinPrice = 10,
                MaxPrice = 100,
                IsAvailable = null
            };


            var result = await _productService.SearchProductsAsync(criteria);


            Assert.Single(result);
        }

        [Fact]
        public async Task SearchProductsAsync_RepositoryThrowsException_PropagatesException()
        {
            _repositoryMock.Setup(r => r.GetProductsQuery())
                           .Throws(new Exception("Query failed"));

            var criteria = new ProductSearchCriteria
            {
                Name = "Test"
            };


            await Assert.ThrowsAsync<Exception>(() => _productService.SearchProductsAsync(criteria));
        }

        #endregion
    }
}
