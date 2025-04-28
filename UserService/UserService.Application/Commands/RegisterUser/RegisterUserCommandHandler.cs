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
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, string>
    {
        private readonly IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public RegisterUserCommandHandler(
            IUserService userService,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userService = userService;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<string> Handle(RegisterUserCommand cmd, CancellationToken ct)
        {
            var user = new User {
                UserName = cmd.Email,
                Email = cmd.Email,
                Name = cmd.Name,
                Address = cmd.Address ?? string.Empty
            };

            var result = await _userService.RegisterUserAsync(user, cmd.Password);
            if (!result.Succeeded)
                throw new ValidationException(result.Errors
                    .Select(e => new ValidationFailure(e.Code, e.Description)));

            var token  = await _userService.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebUtility.UrlEncode(token);
            var clientUrl = _configuration["AppSettings:ClientUrl"] ?? "http://localhost:4200";
            var link    = $"{clientUrl}/confirmemail?userId={user.Id}&token={encoded}";
            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                $"Click to confirm: <a href='{link}'>Confirm</a>"
            );

            return "Registration successful. Please check your email to confirm your account.";
        }
    }
}
