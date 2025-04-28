using MediatR;
using UserService.Domain.Entities;

namespace UserService.Application.Commands
{
    public class UpdateUserCommand : IRequest<User>
    {
        public string UserId { get; init; }
        public string? Email { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
    }
}