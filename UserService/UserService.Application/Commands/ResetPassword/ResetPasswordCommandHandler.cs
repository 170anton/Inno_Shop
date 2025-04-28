using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using UserService.Domain.Entities;
using UserService.Application.Commands;
using UserService.Application.Interfaces;

namespace UserService.Application.Commands
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
    {
        private readonly IUserService _userService;

        public ResetPasswordCommandHandler(IUserService userService)
            => _userService = userService;

        public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (request.NewPassword != request.ConfirmPassword)
                throw new ApplicationException("Passwords do not match.");

            var user = await _userService.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ApplicationException("User not found.");

            var decodedToken = WebUtility.UrlDecode(request.Token);
            var result = await _userService.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
                throw new ApplicationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));

            return Unit.Value;
        }
    }
}
