using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(Guid id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(Guid id);
        Task SetProductsDeletionStatusAsync(Guid userId, bool isDeleted);
        Task<IEnumerable<Product>> SearchProductsAsync(ProductSearchCriteria criteria);

    }
}
