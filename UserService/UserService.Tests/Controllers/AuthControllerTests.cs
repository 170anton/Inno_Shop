using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using UserService.API.Controllers;
using UserService.Application.DTOs;     
using UserService.Domain.Entities;
using UserService.Application.Interfaces;
using MediatR;
using UserService.Application.Commands;

namespace UserService.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);

            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<IConfiguration>();
            _tokenServiceMock = new Mock<ITokenService>();
            _mediatorMock = new Mock<IMediator>();
             
            _configurationMock.Setup(c => c["AppSettings:ClientUrl"])
                .Returns("http://localhost:4200");
            
            var jwtSectionMock = new Mock<IConfigurationSection>();
            jwtSectionMock.Setup(x => x["Key"])
                .Returns("242g8rfr3es8tg9ag89asr9jas49a6t5h7as3h67a5grs6g");
            jwtSectionMock.Setup(x => x["Issuer"])
                .Returns("yourdomain.com");
            jwtSectionMock.Setup(x => x["Audience"])
                .Returns("yourdomain.com");
            jwtSectionMock.Setup(x => x["ExpireMinutes"])
                .Returns("30");
            _configurationMock.Setup(x => x.GetSection("Jwt")).Returns(jwtSectionMock.Object);

            _controller = new AuthController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _emailSenderMock.Object,
                _configurationMock.Object,
                _tokenServiceMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public async Task Register_ValidModel_CallsMediatorAndReturnsOk()
        {
            var model = new RegisterModel {
                Email = "a@b.com",
                Password = "P@ssw0rd!",
                Name = "User",
                Address = "Addr"
            };
            var expected = "Registration successful. Check email.";
            _mediatorMock
                .Setup(m => m.Send(It.Is<RegisterUserCommand>(cmd =>
                    cmd.Email == model.Email &&
                    cmd.Password == model.Password &&
                    cmd.Name == model.Name &&
                    cmd.Address == model.Address
                ), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _controller.Register(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
            _mediatorMock.Verify(m => m.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Register_HandlerThrows_PropagatesException()
        {
            var model = new RegisterModel {
                Email = "fail@b.com",
                Password = "x",
                Name = "User",
                Address = null
            };
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("Something went wrong"));

            await Assert.ThrowsAsync<ApplicationException>(() => _controller.Register(model));
        }



        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            var loginModel = new LoginModel
            {
                Email = "test@example.com",
                Password = "Test123"
            };

            var user = new User
            {
                Id = "123",
                Email = "test@example.com",
                UserName = "test@example.com"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginModel.Email))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, loginModel.Password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _tokenServiceMock.Setup(ts => ts.GenerateJwtToken(It.IsAny<User>()))
                .Returns("generated-token");


            var result = await _controller.Login(loginModel);


            var okResult = Assert.IsType<OkObjectResult>(result);
            var payload = okResult.Value;

            var tokenProperty = payload.GetType().GetProperty("token");
            Assert.NotNull(tokenProperty);
            
            var tokenValue = tokenProperty.GetValue(payload)?.ToString();
            Assert.Equal("generated-token", tokenValue);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var loginModel = new LoginModel
            {
                Email = "notfound@example.com",
                Password = "Test123"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginModel.Email))
                .ReturnsAsync((User)null);


            var result = await _controller.Login(loginModel);


            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            var model = new LoginModel
            {
                Email = "invalid-email",
                Password = "123"
            };

            _controller.ModelState.AddModelError("Email", "Email format is incorrect");


            var result = await _controller.Login(model);


            Assert.IsType<BadRequestObjectResult>(result);
        }



        [Fact]
        public async Task ConfirmEmail_ValidParameters_ReturnsOk()
        {
            string userId = "123";
            string rawToken = "test-token";
            string encodedToken = System.Net.WebUtility.UrlEncode(rawToken);
            var user = new User { Id = userId, Email = "test@example.com" };
            
            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, rawToken))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _controller.ConfirmEmail(userId, encodedToken);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Email confirmed successfully.", okResult.Value);
        }

        [Fact]
        public async Task ConfirmEmail_UserNotFound_ReturnsNotFound()
        {
            string userId = "nonexistent";
            string token = System.Net.WebUtility.UrlEncode("some-token");
            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync((User)null);


            var result = await _controller.ConfirmEmail(userId, token);


            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("User with ID", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
        {
            string userId = "123";
            string rawToken = "invalid-token";
            string encodedToken = System.Net.WebUtility.UrlEncode(rawToken);
            var user = new User { Id = userId, Email = "test@example.com" };

            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, rawToken))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));


            var result = await _controller.ConfirmEmail(userId, encodedToken);


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error confirming your email.", badRequestResult.Value);
        }

        [Fact]
        public async Task ConfirmEmail_MissingParameters_ReturnsBadRequest()
        {
            var result = await _controller.ConfirmEmail("", "");


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User Id and token are required.", badRequestResult.Value);
        }



        [Fact]
        public async Task ForgotPassword_ValidModel_CallsMediatorAndReturnsOk()
        {
            var model = new ForgotPasswordModel { Email = "a@b.com" };
            _mediatorMock
                .Setup(m => m.Send(It.Is<ForgotPasswordCommand>(cmd => cmd.Email == model.Email), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ForgotPassword(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("password reset link has been sent", ok.Value.ToString());
            _mediatorMock.Verify(m => m.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task ResetPassword_ValidModel_CallsMediatorAndReturnsOk()
        {
            var model = new ResetPasswordModel {
                UserId = "123", Token = "tok", NewPassword = "new", ConfirmPassword = "new"
            };
            _mediatorMock
                .Setup(m => m.Send(It.Is<ResetPasswordCommand>(cmd =>
                    cmd.UserId == model.UserId &&
                    cmd.Token  == model.Token &&
                    cmd.NewPassword == model.NewPassword
                ), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ResetPassword(model);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Password has been reset successfully.", ok.Value);
            _mediatorMock.Verify(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }



        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }
    }
}
