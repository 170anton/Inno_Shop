using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetProductsByUserIdAsync(Guid userId);
        Task<Product?> GetByIdAsync(Guid id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Guid id);
        Task SaveChangesAsync();
        IQueryable<Product> GetProductsQuery();
    }
}
