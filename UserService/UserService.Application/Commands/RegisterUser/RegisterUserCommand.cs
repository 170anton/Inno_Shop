using MediatR;

namespace UserService.Application.Commands
{
    public class RegisterUserCommand : IRequest<string>
    {
        public string Email { get; init; }
        public string Password { get; init; }
        public string Name { get; init; }
        public string? Address { get; init; }
    }
}