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

namespace UserService.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);

            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<IConfiguration>();
            _tokenServiceMock = new Mock<ITokenService>();
             
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
                _tokenServiceMock.Object);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOk()
        {
            var registerModel = new RegisterModel
            {
                Email = "test@example.com",
                Password = "Test123",
                Name = "Test User",
                Address = "Test Address"
            };

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("test-token");



            var result = await _controller.Register(registerModel);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Registration successful. Please check your email to confirm your account.", okResult.Value);
        }

        [Fact]
        public async Task Register_CreationFails_ReturnsBadRequest()
        {
            var registerModel = new RegisterModel
            {
                Email = "test@example.com",
                Password = "Password123",
                Name = "Test User",
                Address = "Test Address"
            };

            var identityError = new IdentityError { Description = "Creation failed" };
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(identityError));


            var result = await _controller.Register(registerModel);


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = badRequestResult.Value as IEnumerable<IdentityError>;
            var errorString = string.Join(" ", errors.Select(e => e.Description));
            Assert.Contains("Creation failed", errorString);
        }


        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequest()
        {
            var invalidModel = new RegisterModel
            {
                Email = "",   
                Password = "Test123",
                Name = "",        
                Address = null
            };

            _controller.ModelState.AddModelError("Email", "Email is required");
            _controller.ModelState.AddModelError("Name", "Name is required");


            var result = await _controller.Register(invalidModel);


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
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
        public async Task ForgotPassword_UserFound_SendsEmailAndReturnsOk()
        {
            var forgotModel = new ForgotPasswordModel { Email = "test@example.com" };
            var user = new User { Id = "123", Email = "test@example.com" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(forgotModel.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token");
            _emailSenderMock.Setup(es => es.SendEmailAsync(user.Email, "Reset your password", It.IsAny<string>()))
                .Returns(Task.CompletedTask);


            var result = await _controller.ForgotPassword(forgotModel);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("If an account with that email exists", okResult.Value.ToString());
        }

        [Fact]
        public async Task ForgotPassword_UserNotFound_ReturnsOkWithGenericMessage()
        {
            var forgotModel = new ForgotPasswordModel { Email = "notfound@example.com" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(forgotModel.Email))
                .ReturnsAsync((User)null);


            var result = await _controller.ForgotPassword(forgotModel);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("If an account with that email exists", okResult.Value.ToString());
        }

        [Fact]
        public async Task ForgotPassword_InvalidModel_ReturnsBadRequest()
        {
            var model = new ForgotPasswordModel
            {
                Email = ""
            };
            _controller.ModelState.AddModelError("Email", "Email is required");


            var result = await _controller.ForgotPassword(model);


            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task ResetPassword_ValidModel_ReturnsOk()
        {
            var resetModel = new ResetPasswordModel
            {
                UserId = "123",
                Token = System.Net.WebUtility.UrlEncode("test-reset-token"),
                NewPassword = "NewPassword",
                ConfirmPassword = "NewPassword"
            };
            var user = new User { Id = "123", Email = "test@example.com" };
            _userManagerMock.Setup(um => um.FindByIdAsync(resetModel.UserId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ResetPasswordAsync(user, "test-reset-token", resetModel.NewPassword))
                .ReturnsAsync(IdentityResult.Success);


            var result = await _controller.ResetPassword(resetModel);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Password has been reset successfully.", okResult.Value);
        }

        [Fact]
        public async Task ResetPassword_PasswordsMismatch_ReturnsBadRequest()
        {
            var resetModel = new ResetPasswordModel
            {
                UserId = "123",
                Token = System.Net.WebUtility.UrlEncode("test-reset-token"),
                NewPassword = "NewPassword",
                ConfirmPassword = "DifferentPassword"
            };

            var result = await _controller.ResetPassword(resetModel);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Passwords do not match.", badRequestResult.Value);
        }

        [Fact]
        public async Task ResetPassword_UserNotFound_ReturnsNotFound()
        {
            var resetModel = new ResetPasswordModel
            {
                UserId = "nonexistent",
                Token = System.Net.WebUtility.UrlEncode("test-reset-token"),
                NewPassword = "NewPassword",
                ConfirmPassword = "NewPassword"
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(resetModel.UserId))
                .ReturnsAsync((User)null);


            var result = await _controller.ResetPassword(resetModel);


            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            var resetModel = new ResetPasswordModel
            {
                UserId = "123",
                Token = System.Net.WebUtility.UrlEncode("invalid-token"),
                NewPassword = "NewPassword",
                ConfirmPassword = "NewPassword"
            };
            var user = new User { Id = "123", Email = "test@example.com" };

            _userManagerMock.Setup(um => um.FindByIdAsync(resetModel.UserId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ResetPasswordAsync(user, "invalid-token", resetModel.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));


            var result = await _controller.ResetPassword(resetModel);


            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            
            var errors = badRequestResult.Value as IEnumerable<IdentityError>;
            var errorString = string.Join(" ", errors.Select(e => e.Description));
            Assert.Contains("Invalid token", errorString);
        }

        [Fact]
        public async Task ResetPassword_InvalidModel_ReturnsBadRequest()
        {
            var model = new ResetPasswordModel
            {
                UserId = "123",
                Token = "",
                NewPassword = "NewPassword123",
                ConfirmPassword = "NewPassword123"
            };
            _controller.ModelState.AddModelError("Token", "Token is required");


            var result = await _controller.ResetPassword(model);


            Assert.IsType<BadRequestObjectResult>(result);
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
