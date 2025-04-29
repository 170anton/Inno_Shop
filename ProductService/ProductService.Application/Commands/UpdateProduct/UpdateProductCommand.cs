using MediatR;

namespace ProductService.Application.Commands
{
    public record UpdateProductCommand(
        Guid Id,
        string? Name,
        string? Description,
        decimal? Price,
        bool? IsAvailable,
        Guid CreatedByUserId
    ) : IRequest;   
}