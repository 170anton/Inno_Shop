using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Application.Services.UserService _userService;

        public UserServiceTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _userService = new Application.Services.UserService(_userManagerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            var users = new List<User>
            {
                new User { Id = "1", Email = "user1@test.com", UserName = "user1@test.com" },
                new User { Id = "2", Email = "user2@test.com", UserName = "user2@test.com" }
            };

            _userManagerMock.Setup(m => m.Users).Returns(users.AsQueryable());


            var result = await _userService.GetAllAsync();


            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_ReturnsUser()
        {
            var user = new User { Id = "1", Email = "user1@test.com", UserName = "user1@test.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);


            var result = await _userService.GetByIdAsync("1");


            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent")).ReturnsAsync((User)null);


            var result = await _userService.GetByIdAsync("nonexistent");


            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterUserAsync_ReturnsSuccess()
        {
            var user = new User 
            { 
                Id = "1", 
                Email = "test@example.com", 
                UserName = "test@example.com", 
                Name = "Test User"
            };
            string password = "TestPassword";

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _userService.RegisterUserAsync(user, password);


            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsSuccess()
        {
            var user = new User 
            { 
                Id = "1", 
                Email = "old@example.com", 
                UserName = "old@example.com", 
                Name = "Old Name" 
            };

            _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _userService.UpdateUserAsync(user);


            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_ReturnsSuccess()
        {
            var user = new User 
            { 
                Id = "1", 
                Email = "test@example.com", 
                UserName = "test@example.com", 
                Name = "Test User" 
            };

            _userManagerMock.Setup(um => um.FindByIdAsync("1"))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _userService.DeleteUserAsync("1");


            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ReturnsFailedResult()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("nonexistent")).ReturnsAsync((User)null);


            var result = await _userService.DeleteUserAsync("nonexistent");


            Assert.False(result.Succeeded);
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }
    }
}
