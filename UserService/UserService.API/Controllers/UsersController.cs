using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using UserService.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using UserService.Domain.Entities;
using UserService.Application.DTOs;
using System.Net.Http.Headers;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IProductServiceClient _productServiceClient;
        
        public UsersController(IUserService userService, IProductServiceClient productServiceClient)
        {
            _userService = userService;
            _productServiceClient = productServiceClient;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<User> users = await _userService.GetAllAsync();
            return Ok(users);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            User? user = await _userService.GetByIdAsync(id);
            if(user == null)
                return NotFound();
            return Ok(user);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserModel model)
        {
            User? user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                user.Email = model.Email;
                user.UserName = model.Email;
            }
            
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                user.Name = model.Name;
            }
            
            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                user.Address = model.Address;
            }

            IdentityResult result = await _userService.UpdateUserAsync(user);
            if (result.Succeeded)
                return Ok(user);
            else
                return BadRequest(result.Errors);
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityResult result = await _userService.DeleteUserAsync(id);
            if(result.Succeeded)
                return NoContent();
            else
                return BadRequest(result.Errors);
        }


        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsActivated = false;
            var result = await _userService.UpdateUserAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("No JWT token found.");
            }

            await _productServiceClient.DeactivateProductsByUserIdAsync(user.Id, token);

            return Ok("User deactivated.");
        }

        // PUT: api/users/{id}/activate
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsActivated = true;
            var result = await _userService.UpdateUserAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("No JWT token found.");
            }

            await _productServiceClient.ActivateProductsByUserIdAsync(user.Id, token);

            return Ok("User activated.");
        }
    }
}
