using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Application.Commands
{
    public class CreateProductCommandHandler
        : IRequestHandler<CreateProductCommand, Guid>
    {
        private readonly IProductService _service;

        public CreateProductCommandHandler(IProductService service)
            => _service = service;

        public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
        {
            var product = new Product
            {
                Name = cmd.Name,
                Description = cmd.Description,
                Price = cmd.Price,
                IsAvailable = cmd.IsAvailable,
                CreatedByUserId = cmd.CreatedByUserId
            };

            await _service.AddProductAsync(product);
            return product.Id;
        }
    }
}
