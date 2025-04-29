// в проекте ProductService.Application.Commands

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Application.Commands
{
    public class UpdateProductCommandHandler
        : IRequestHandler<UpdateProductCommand, Unit>
    {
        private readonly IProductService _service;

        public UpdateProductCommandHandler(IProductService service)
            => _service = service;

        public async Task<Unit> Handle(UpdateProductCommand cmd, CancellationToken ct)
        {
            var product = await _service.GetProductByIdAsync(cmd.Id)
                          ?? throw new KeyNotFoundException("Product not found.");

            if (cmd.Name is not null) product.Name = cmd.Name;
            if (cmd.Description is not null) product.Description = cmd.Description;
            if (cmd.Price is not null) product.Price = cmd.Price.Value;
            if (cmd.IsAvailable is not null) product.IsAvailable = cmd.IsAvailable.Value;

            await _service.UpdateProductAsync(product);
            return Unit.Value;
        }
    }
}
