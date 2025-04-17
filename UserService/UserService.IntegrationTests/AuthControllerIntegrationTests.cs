using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.IntegrationTests
{
    public class AuthControllerIntegrationTests :
        IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsSuccessMessage()
        {
            var model = new
            {
                Email = "inttest@example.com",
                Password = "Password123!",
                Name = "Integration",
                Address = "Test"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );


            var response = await _client.PostAsync("/api/auth/register", content);


            response.EnsureSuccessStatusCode(); 
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("Registration successful", text);
        }

        [Fact]
        public async Task Register_MissingEmail_ReturnsBadRequest()
        {
            var badModel = new
            {
                Password = "Password123!",
                Name = "No Email",
                Address = "Addr"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(badModel),
                Encoding.UTF8,
                "application/json"
            );


            var response = await _client.PostAsync("/api/auth/register", content);


            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email is required", body);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var model = new
            {
                Email = "dupe@example.com",
                Password = "Password123!",
                Name = "Dupe",
                Address = "Addr"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );

            var first = await _client.PostAsync("/api/auth/register", content);
            first.EnsureSuccessStatusCode();


            var second = await _client.PostAsync("/api/auth/register", content);


            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var body = await second.Content.ReadAsStringAsync();
            Assert.Contains("taken", body, StringComparison.OrdinalIgnoreCase);
        }



        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            var registerModel = new
            {
                Email = "loginok@example.com",
                Password = "Password123!",
                Name = "Login OK",
                Address = "Addr"
            };
            var registerContent = new StringContent(
                JsonSerializer.Serialize(registerModel),
                Encoding.UTF8,
                "application/json"
            );
            var regResponse = await _client.PostAsync("/api/auth/register", registerContent);
            regResponse.EnsureSuccessStatusCode();


            var loginModel = new
            {
                Email = registerModel.Email,
                Password = registerModel.Password
            };
            var loginContent = new StringContent(
                JsonSerializer.Serialize(loginModel),
                Encoding.UTF8,
                "application/json"
            );
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);


            loginResponse.EnsureSuccessStatusCode();
            var json = await loginResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"token\"", json);
            Assert.Contains("test-token", json);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            var registerModel = new
            {
                Email = "loginfail@example.com",
                Password = "Password123!",
                Name = "Login Fail",
                Address = "Addr"
            };
            var regContent = new StringContent(
                JsonSerializer.Serialize(registerModel),
                Encoding.UTF8,
                "application/json"
            );
            var regResp = await _client.PostAsync("/api/auth/register", regContent);
            regResp.EnsureSuccessStatusCode();


            var loginModel = new
            {
                Email = registerModel.Email,
                Password = "WrongPassword"
            };
            var loginContent = new StringContent(
                JsonSerializer.Serialize(loginModel),
                Encoding.UTF8,
                "application/json"
            );
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);


            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var model = new
            {
                Email = "doesnotexist@example.com",
                Password = "wrong"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/auth/login", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


        [Fact]
        public async Task ConfirmEmail_MissingParameters_ReturnsBadRequest()
        {
            var resp = await _client.GetAsync("/api/auth/confirmemail?userId=&token=");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }


        [Fact]
        public async Task ForgotPassword_ExistingEmail_ReturnsOk()
        {
            var register = new RegisterModel
            {
                Email = "fp_ok@example.com",
                Password = "Password!1",
                Name = "FPok",
                Address = "Addr"
            };
            var regResp = await _client.PostAsJsonAsync("/api/auth/register", register);
            regResp.EnsureSuccessStatusCode();

            var forgot = new ForgotPasswordModel { Email = register.Email };
            var resp = await _client.PostAsJsonAsync("/api/auth/forgotpassword", forgot);

            resp.EnsureSuccessStatusCode();
            var text = await resp.Content.ReadAsStringAsync();
            Assert.Contains("password reset link has been sent", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ForgotPassword_InvalidModel_ReturnsBadRequest()
        {
            var resp = await _client.PostAsJsonAsync("/api/auth/forgotpassword", new { Email = "" });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }


        [Fact]
        public async Task ResetPassword_PasswordsMismatch_ReturnsBadRequest()
        {
            var reset = new ResetPasswordModel
            {
                UserId = "doesnotmatter",
                Token = "token",
                NewPassword = "abc",
                ConfirmPassword = "xyz"
            };
            var resp = await _client.PostAsJsonAsync("/api/auth/resetpassword", reset);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
