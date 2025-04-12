using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Application.Models;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;


namespace ProductService.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        
        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddProductAsync(Product product)
        {
            await _repository.AddAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteProductAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }
        public async Task SetProductsDeletionStatusAsync(Guid userId, bool isDeleted)
        {
            var products = await _repository.GetProductsByUserIdAsync(userId);
            
            foreach (var product in products)
            {
                product.IsDeleted = isDeleted;
            }
            
            await _repository.SaveChangesAsync();
        }
        
        public async Task<IEnumerable<Product>> SearchProductsAsync(ProductSearchCriteria criteria)
        {
            IQueryable<Product> query = _repository.GetProductsQuery();

            if (!string.IsNullOrWhiteSpace(criteria.Name))
            {
                query = query.Where(p => p.Name.Contains(criteria.Name));
            }

            if (criteria.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= criteria.MinPrice.Value);
            }

            if (criteria.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= criteria.MaxPrice.Value);
            }

            if (criteria.IsAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == criteria.IsAvailable.Value);
            }

            return await query.ToListAsync();
        }
    }
}
