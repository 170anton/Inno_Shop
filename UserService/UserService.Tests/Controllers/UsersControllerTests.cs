using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using UserService.API.Controllers;
using UserService.Application.DTOs; 
using UserService.Domain.Entities; 
using UserService.Application.Interfaces;
using MediatR;
using UserService.Application.Commands;

namespace UserService.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IProductServiceClient> _productServiceClientMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly UsersController _controller;
        
        public UsersControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _productServiceClientMock = new Mock<IProductServiceClient>();
            _mediatorMock = new Mock<IMediator>();
            
            _controller = new UsersController(_userServiceMock.Object, _productServiceClientMock.Object, _mediatorMock.Object);
            
            var httpContext = new DefaultHttpContext();
            
            httpContext.Request.Headers["Authorization"] = "Bearer test-token";
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }



        [Fact]
        public async Task GetById_UserExists_ReturnsOkWithUser()
        {
            var user = new User { Id = "1", Email = "user1@example.com", UserName = "user1@example.com", Name = "User One" };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            
            
            var result = await _controller.GetById("1");
            
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<User>(okResult.Value);
            Assert.Equal("1", returnedUser.Id);
        }

        [Fact]
        public async Task GetById_UserDoesNotExist_ReturnsNotFound()
        {
            _userServiceMock.Setup(s => s.GetByIdAsync("nonexistent"))
                .ReturnsAsync((User)null);


            var result = await _controller.GetById("nonexistent");


            Assert.IsType<NotFoundResult>(result);
        }



        [Fact]
        public async Task Update_UserExists_CallsMediatorAndReturnsOk()
        {
            var userId = "1";
            var model = new UpdateUserModel
            {
                Email = "new@example.com",
                Name = "New Name",
                Address = "New Address"
            };
            var updatedUser = new User
            {
                Id = userId,
                Email = model.Email,
                UserName = model.Email,
                Name = model.Name,
                Address = model.Address
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<UpdateUserCommand>(cmd =>
                        cmd.UserId == userId &&
                        cmd.Email == model.Email &&
                        cmd.Name == model.Name &&
                        cmd.Address == model.Address
                    ),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(updatedUser);


            var actionResult = await _controller.Update(userId, model);


            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(updatedUser, okResult.Value);
            _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_UserDoesNotExist_ReturnsNotFound()
        {
            var userId = "nonexistent";
            var model = new UpdateUserModel
            {
                Email = "new@example.com",
                Name = "New Name",
                Address = "New Address"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException());


            var result = await _controller.Update(userId, model);


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_HandlerFails_ReturnsBadRequest()
        {
            var userId = "1";
            var model = new UpdateUserModel
            {
                Email = "new@example.com",
                Name = "New Name",
                Address = "New Address"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("Some validation error"));


            var result = await _controller.Update(userId, model);


            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Some validation error", badRequest.Value);
        }



        [Fact]
        public async Task Delete_ValidId_ReturnsNoContent()
        {
            _userServiceMock.Setup(s => s.DeleteUserAsync("1"))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _controller.Delete("1");


            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_DeletionFails_ReturnsBadRequest()
        {
            var identityError = new IdentityError { Description = "Deletion failed" };
            _userServiceMock.Setup(s => s.DeleteUserAsync("1"))
                .ReturnsAsync(IdentityResult.Failed(identityError));


            var result = await _controller.Delete("1");


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = badRequestResult.Value as IEnumerable<IdentityError>;
            var errorString = string.Join(" ", errors.Select(e => e.Description));
            Assert.Contains("Deletion failed", errorString);
        }



        [Fact]
        public async Task Deactivate_UserExists_ReturnsOk()
        {
            var user = new User { Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = true };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            _userServiceMock.Setup(s => s.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            _productServiceClientMock.Setup(c => c.DeactivateProductsByUserIdAsync(user.Id, "test-token"))
                .Returns(Task.CompletedTask);


            var result = await _controller.Deactivate("1");


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User deactivated.", okResult.Value);
            
            _productServiceClientMock.Verify(c => c.DeactivateProductsByUserIdAsync(user.Id, "test-token"), Times.Once);
        }

        [Fact]
        public async Task Deactivate_UserDoesNotExist_ReturnsNotFound()
        {
            _userServiceMock.Setup(s => s.GetByIdAsync("nonexistent"))
                .ReturnsAsync((User)null);


            var result = await _controller.Deactivate("nonexistent");


            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Deactivate_MissingToken_ReturnsUnauthorized()
        {
            var user = new User 
            { 
                Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = true 
            };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            _userServiceMock.Setup(s => s.UpdateUserAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _controller.HttpContext.Request.Headers["Authorization"] = "";


            var result = await _controller.Deactivate("1");


            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("No JWT token found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Deactivate_UpdateFails_ReturnsBadRequest()
        {
            var user = new User 
            { 
                Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = true 
            };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            var identityError = new IdentityError { Description = "Update failed" };
            _userServiceMock.Setup(s => s.UpdateUserAsync(user))
                .ReturnsAsync(IdentityResult.Failed(identityError));


            var result = await _controller.Deactivate("1");


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = badRequestResult.Value as IEnumerable<IdentityError>;
            var errorString = string.Join(" ", errors.Select(e => e.Description));
            Assert.Contains("Update failed", errorString);
        }



        [Fact]
        public async Task Activate_UserExists_ReturnsOk()
        {
            var user = new User { Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = false };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            _userServiceMock.Setup(s => s.UpdateUserAsync(user)).ReturnsAsync(IdentityResult.Success);

            _productServiceClientMock.Setup(c => c.ActivateProductsByUserIdAsync(user.Id, "test-token"))
                .Returns(Task.CompletedTask);


            var result = await _controller.Activate("1");


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User activated.", okResult.Value);
            
            _productServiceClientMock.Verify(c => c.ActivateProductsByUserIdAsync(user.Id, "test-token"), Times.Once);
        }

        [Fact]
        public async Task Activate_MissingToken_ReturnsUnauthorized()
        {
            var user = new User 
            { 
                Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = false 
            };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            _userServiceMock.Setup(s => s.UpdateUserAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _controller.HttpContext.Request.Headers["Authorization"] = "";


            var result = await _controller.Activate("1");


            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("No JWT token found.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Activate_UpdateFails_ReturnsBadRequest()
        {
            var user = new User 
            { 
                Id = "1", Email = "user@example.com", UserName = "user@example.com", Name = "User", IsActivated = false 
            };
            _userServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);
            var identityError = new IdentityError { Description = "Update failed" };
            _userServiceMock.Setup(s => s.UpdateUserAsync(user))
                .ReturnsAsync(IdentityResult.Failed(identityError));


            var result = await _controller.Activate("1");


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = badRequestResult.Value as IEnumerable<IdentityError>;
            var errorString = string.Join(" ", errors.Select(e => e.Description));
            Assert.Contains("Update failed", errorString);
        }

        [Fact]
        public async Task Activate_UserNotFound_ReturnsNotFound()
        {
            var nonExistentUserId = "nonexistent-user-id";
            _userServiceMock.Setup(s => s.GetByIdAsync(nonExistentUserId))
                            .ReturnsAsync((User)null);
            

            var result = await _controller.Activate(nonExistentUserId);
            
            
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
