using MediatR;

namespace UserService.Application.Commands
{
    public class ResetPasswordCommand : IRequest<Unit>
    {
        public string UserId { get; init; }
        public string Token { get; init; }
        public string NewPassword { get; init; }
        public string ConfirmPassword { get; init; }
    }
}