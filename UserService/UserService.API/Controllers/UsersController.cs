using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using UserService.API.Models;
using UserService.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using UserService.Domain.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        
        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<User> users = await _userService.GetAllAsync();
            return Ok(users);
        }
        
        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            User? user = await _userService.GetByIdAsync(id);
            if(user == null)
                return NotFound();
            return Ok(user);
        }
        
        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserModel model)
        {
            User? user = await _userService.GetByIdAsync(id);
            if(user == null)
                return NotFound();
            
            user.Email = model.Email;
            user.UserName = model.Email; // Обычно имя пользователя совпадает с email
            
            IdentityResult result = await _userService.UpdateUserAsync(user);
            if(result.Succeeded)
                return Ok(user);
            else
                return BadRequest(result.Errors);
        }
        
        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityResult result = await _userService.DeleteUserAsync(id);
            if(result.Succeeded)
                return NoContent();
            else
                return BadRequest(result.Errors);
        }
    }
}
