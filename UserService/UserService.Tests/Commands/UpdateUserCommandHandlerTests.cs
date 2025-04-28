using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using UserService.Application.Commands;
using UserService.Domain.Entities;
using UserService.Application.Interfaces;
using UserService.Application.Handlers;

namespace UserService.Tests.Commands
{
    public class UpdateUserCommandHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly UpdateUserCommandHandler _handler;

        public UpdateUserCommandHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _handler = new UpdateUserCommandHandler(_userServiceMock.Object);
        }

        [Fact]
        public async Task Handle_UserExists_UpdatesFieldsAndReturnsUser()
        {
            var userId = "user1";
            var existing = new User
            {
                Id = userId,
                Email = "old@example.com",
                UserName = "old@example.com",
                Name = "OldName",
                Address = "OldAddr"
            };
            _userServiceMock.Setup(x => x.GetByIdAsync(userId))
                            .ReturnsAsync(existing);
            _userServiceMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                            .ReturnsAsync(IdentityResult.Success);

            var cmd = new UpdateUserCommand
            {
                UserId = userId,
                Email = "new@example.com",
                Name = "NewName",
                Address = "NewAddr"
            };


            var result = await _handler.Handle(cmd, CancellationToken.None);


            _userServiceMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _userServiceMock.Verify(x => x.UpdateUserAsync(
                It.Is<User>(u =>
                    u.Id == userId &&
                    u.Email == "new@example.com" &&
                    u.UserName == "new@example.com" &&
                    u.Name == "NewName" &&
                    u.Address == "NewAddr"
                )), Times.Once);

            Assert.Equal("new@example.com", result.Email);
            Assert.Equal("NewName", result.Name);
            Assert.Equal("NewAddr", result.Address);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
        {
            var userId = "doesnotexist";
            _userServiceMock.Setup(x => x.GetByIdAsync(userId))
                            .ReturnsAsync((User)null!);

            var cmd = new UpdateUserCommand
            {
                UserId = userId,
                Email = "irrelevant@example.com"
            };


            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(cmd, CancellationToken.None)
            );
        }

        [Fact]
        public async Task Handle_UpdateFails_ThrowsApplicationExceptionWithErrors()
        {
            var userId = "user1";
            var existing = new User { Id = userId, Email = "old@example.com" };
            _userServiceMock.Setup(x => x.GetByIdAsync(userId))
                            .ReturnsAsync(existing);

            var errors = new[]
            {
                new IdentityError { Description = "err1" },
                new IdentityError { Description = "err2" }
            };
            _userServiceMock.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                            .ReturnsAsync(IdentityResult.Failed(errors));

            var cmd = new UpdateUserCommand
            {
                UserId = userId
            };


            var ex = await Assert.ThrowsAsync<ApplicationException>(
                () => _handler.Handle(cmd, CancellationToken.None)
            );
            Assert.Contains("err1", ex.Message);
            Assert.Contains("err2", ex.Message);
        }
    }
}
