using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;
using Xunit;
using UserService.Application.Commands;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using MediatR;

namespace UserService.Tests.Commands
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly ForgotPasswordCommandHandler _handler;

        public ForgotPasswordCommandHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock
                .Setup(c => c["AppSettings:ClientUrl"])
                .Returns("http://localhost:4200");

            _handler = new ForgotPasswordCommandHandler(
                _userServiceMock.Object,
                _emailSenderMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task Handle_UserNotFound_DoesNotSendEmail()
        {
            var command = new ForgotPasswordCommand { Email = "notfound@example.com" };
            _userServiceMock
                .Setup(s => s.FindByEmailAsync(command.Email))
                .ReturnsAsync((User?)null);


            var result = await _handler.Handle(command, CancellationToken.None);


            _emailSenderMock.Verify(
                e => e.SendEmailAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()
                ),
                Times.Never
            );
            Assert.Equal(Unit.Value, result);
        }

        [Fact]
        public async Task Handle_UserFound_SendsResetEmail()
        {
            var command = new ForgotPasswordCommand { Email = "user@example.com" };
            var user = new User { Id = "1", Email = command.Email };

            _userServiceMock
                .Setup(s => s.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userServiceMock
                .Setup(s => s.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("token123");
            _emailSenderMock
                .Setup(e => e.SendEmailAsync(
                    user.Email,
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ))
                .Returns(Task.CompletedTask);


            await _handler.Handle(command, CancellationToken.None);


            _userServiceMock.Verify(
                s => s.GeneratePasswordResetTokenAsync(user),
                Times.Once
            );
            _emailSenderMock.Verify(
                e => e.SendEmailAsync(
                    user.Email,
                    "Reset your password",
                    It.Is<string>(body =>
                        body.Contains("token123") &&
                        body.Contains($"resetpassword?userId={user.Id}")
                    )
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_GenerateTokenFails_ThrowsException()
        {
            var command = new ForgotPasswordCommand { Email = "user@example.com" };
            var user = new User { Id = "1", Email = command.Email };

            _userServiceMock
                .Setup(s => s.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userServiceMock
                .Setup(s => s.GeneratePasswordResetTokenAsync(user))
                .ThrowsAsync(new Exception("failure"));


            await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None)
            );
        }
    }
}
