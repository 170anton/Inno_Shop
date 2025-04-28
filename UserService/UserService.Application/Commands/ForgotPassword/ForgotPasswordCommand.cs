using MediatR;

namespace UserService.Application.Commands
{
    public class ForgotPasswordCommand : IRequest<Unit>
    {
        public string Email { get; init; }
    }
}