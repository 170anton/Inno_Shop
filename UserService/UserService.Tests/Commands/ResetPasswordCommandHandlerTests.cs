using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using UserService.Application.Commands;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests.Commands
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly ResetPasswordCommandHandler _handler;

        public ResetPasswordCommandHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _handler = new ResetPasswordCommandHandler(_userServiceMock.Object);
        }

        [Fact]
        public async Task Handle_PasswordsMismatch_ThrowsApplicationException()
        {
            var cmd = new ResetPasswordCommand
            {
                UserId = "1",
                Token = "t",
                NewPassword = "abc",
                ConfirmPassword = "def"
            };

            var ex = await Assert.ThrowsAsync<ApplicationException>(()
                => _handler.Handle(cmd, CancellationToken.None));

            Assert.Equal("Passwords do not match.", ex.Message);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsApplicationException()
        {
            var cmd = new ResetPasswordCommand
            {
                UserId = "nonexistent",
                Token = "t",
                NewPassword = "abc",
                ConfirmPassword = "abc"
            };

            _userServiceMock
                .Setup(s => s.GetByIdAsync(cmd.UserId))
                .ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<ApplicationException>(()
                => _handler.Handle(cmd, CancellationToken.None));

            Assert.Equal("User not found.", ex.Message);
        }

        [Fact]
        public async Task Handle_ResetFails_ThrowsApplicationExceptionWithErrors()
        {
            var cmd = new ResetPasswordCommand
            {
                UserId = "1",
                Token = "tok",
                NewPassword = "pass",
                ConfirmPassword = "pass"
            };
            var user = new User { Id = "1" };
            var errors = new[]
            {
                new IdentityError { Description = "err1" },
                new IdentityError { Description = "err2" }
            };

            _userServiceMock
                .Setup(s => s.GetByIdAsync(cmd.UserId))
                .ReturnsAsync(user);
            _userServiceMock
                .Setup(s => s.ResetPasswordAsync(user, "tok", cmd.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(errors));

            var ex = await Assert.ThrowsAsync<ApplicationException>(()
                => _handler.Handle(cmd, CancellationToken.None));

            var msg = ex.Message;
            Assert.Contains("err1", msg);
            Assert.Contains("err2", msg);
            Assert.Contains("; ", msg);
        }

        [Fact]
        public async Task Handle_Success_ReturnsUnit()
        {
            var cmd = new ResetPasswordCommand
            {
                UserId = "1",
                Token = "tok",
                NewPassword = "pass",
                ConfirmPassword = "pass"
            };
            var user = new User { Id = "1" };

            _userServiceMock
                .Setup(s => s.GetByIdAsync(cmd.UserId))
                .ReturnsAsync(user);
            _userServiceMock
                .Setup(s => s.ResetPasswordAsync(user, "tok", cmd.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.Equal(Unit.Value, result);
        }
    }
}
