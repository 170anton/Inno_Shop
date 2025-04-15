using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name, 
                Address = model.Address ?? string.Empty
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = System.Net.WebUtility.UrlEncode(token);

            var clientUrl = _configuration["AppSettings:ClientUrl"] ?? "http://localhost:4200";
            var confirmationUrl = $"{clientUrl}/confirmemail?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{confirmationUrl}'>Confirm Email</a>");

            return Ok("Registration successful. Please check your email to confirm your account.");
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized("Invalid credentials");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!signInResult.Succeeded)
                return Unauthorized("Invalid credentials");

            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new { token });
        }

        // GET: api/auth/confirmemail?userId=...&token=...
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest("User Id and token are required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"User with ID '{userId}' not found.");

            string decodedToken = System.Net.WebUtility.UrlDecode(token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
                return Ok("Email confirmed successfully.");
            else
                return BadRequest("Error confirming your email.");
        }


        // POST: api/auth/forgotpassword
        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Ok("If an account with that email exists, a password reset link has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Net.WebUtility.UrlEncode(token);

            var clientUrl = _configuration["AppSettings:ClientUrl"] ?? "http://localhost:4200";
            var resetUrl = $"{clientUrl}/resetpassword?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(user.Email, "Reset your password",
                $"Please reset your password by clicking here: <a href='{resetUrl}'>Reset Password</a>");

            return Ok("If an account with that email exists, a password reset link has been sent.");
        }

        // POST: api/auth/resetpassword
        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound("User not found.");

            string decodedToken = System.Net.WebUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (result.Succeeded)
                return Ok("Password has been reset successfully.");
            else
                return BadRequest(result.Errors);
        }

        // private string GenerateJwtToken(User user)
        // {
        //     var jwtSettings = _configuration.GetSection("Jwt");
        //     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        //     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //     var claims = new[]
        //     {
        //         new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        //         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //         new Claim(ClaimTypes.NameIdentifier, user.Id)
        //     };

        //     var token = new JwtSecurityToken(
        //         issuer: jwtSettings["Issuer"],
        //         audience: jwtSettings["Audience"],
        //         claims: claims,
        //         expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"])),
        //         signingCredentials: creds);

        //     return new JwtSecurityTokenHandler().WriteToken(token);
        // }
    }
}
