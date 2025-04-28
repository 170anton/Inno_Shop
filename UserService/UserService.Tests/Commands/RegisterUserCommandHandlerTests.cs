using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests.Commands
{
    public class RegisterUserCommandHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock
                .Setup(c => c["AppSettings:ClientUrl"])
                .Returns("https://app.test");

            _handler = new RegisterUserCommandHandler(
                _userServiceMock.Object,
                _emailSenderMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessMessage_AndSendsEmail()
        {
            var cmd = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "P@ssw0rd!",
                Name = "Test User",
                Address = "Test Address"
            };

            _userServiceMock
                .Setup(u => u.RegisterUserAsync(It.IsAny<User>(), cmd.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userServiceMock
                .Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("raw-token");


            var result = await _handler.Handle(cmd, CancellationToken.None);


            Assert.Equal(
                "Registration successful. Please check your email to confirm your account.",
                result
            );

            _userServiceMock.Verify(u =>
                u.RegisterUserAsync(
                    It.Is<User>(usr =>
                        usr.Email == cmd.Email &&
                        usr.UserName == cmd.Email &&
                        usr.Name == cmd.Name &&
                        usr.Address == cmd.Address),
                    cmd.Password
                ), Times.Once);

            _emailSenderMock.Verify(es =>
                es.SendEmailAsync(
                    cmd.Email,
                    "Confirm your email",
                    It.Is<string>(body =>
                        body.Contains("https://app.test/confirmemail") &&
                        body.Contains("token=raw-token")
                    )
                ), Times.Once);
        }

        [Fact]
        public async Task Handle_RegisterFails_ThrowsValidationException_AndDoesNotSendEmail()
        {
            var cmd = new RegisterUserCommand
            {
                Email = "fail@example.com",
                Password = "weak",
                Name = "Name",
                Address = null
            };

            var errors = new[]
            {
                new IdentityError { Description = "Password too weak" },
                new IdentityError { Description = "Email already taken" }
            };

            _userServiceMock
                .Setup(u => u.RegisterUserAsync(It.IsAny<User>(), cmd.Password))
                .ReturnsAsync(IdentityResult.Failed(errors));


            var ex = await Assert.ThrowsAsync<ValidationException>(async () =>
                await _handler.Handle(cmd, CancellationToken.None)
            );

            Assert.Contains("Password too weak", ex.Errors.Select(e => e.ErrorMessage));
            Assert.Contains("Email already taken", ex.Errors.Select(e => e.ErrorMessage));

            _emailSenderMock.Verify(es =>
                es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
