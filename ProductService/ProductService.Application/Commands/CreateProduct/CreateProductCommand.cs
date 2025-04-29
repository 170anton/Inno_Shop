using MediatR;
using ProductService.Domain.Entities;

namespace ProductService.Application.Commands
{
    public record CreateProductCommand(
        string Name,
        string? Description,
        decimal Price,
        bool IsAvailable,
        Guid CreatedByUserId
    ) : IRequest<Guid>; 
}