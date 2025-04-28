using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using UserService.Application.Commands;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Application.Commands
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public ForgotPasswordCommandHandler(
            IUserService userService,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userService = userService;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userService.FindByEmailAsync(request.Email);
            if (user == null)
                return Unit.Value;

            var token = await _userService.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var clientUrl = _configuration["AppSettings:ClientUrl"] ?? "http://localhost:4200";
            var resetUrl = $"{clientUrl}/resetpassword?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset your password",
                $"Please reset your password by clicking here: <a href='{resetUrl}'>Reset Password</a>"
            );

            return Unit.Value;
        }
    }
}
