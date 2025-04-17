using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;


namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        public ProductsController(IProductService service) => _service = service;
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var products = await _service.GetProductsByUserIdAsync(userId);
            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var existing = await _service.GetProductByIdAsync(id);
            if (existing == null)
                return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            if (existing.CreatedByUserId != userId)
                return Forbid();

            return Ok(existing);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price  = dto.Price,
                IsAvailable = dto.IsAvailable,
                CreatedByUserId = userId
            };

            await _service.AddProductAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            var existing = await _service.GetProductByIdAsync(id);
            if (existing == null)
                return NotFound();

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            if (existing.CreatedByUserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden,
                                  "You are not allowed to modify this product.");

            if (dto.Name is not null) existing.Name        = dto.Name;
            if (dto.Description is not null) existing.Description = dto.Description;
            if (dto.Price is not null) existing.Price       = dto.Price.Value;
            if (dto.IsAvailable is not null) existing.IsAvailable = dto.IsAvailable.Value;

            await _service.UpdateProductAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var existing = await _service.GetProductByIdAsync(id);
            if (existing == null)
                return NotFound();

            if (existing.CreatedByUserId != userId)
                return Forbid();

            await _service.DeleteProductAsync(id);
            return NoContent();
        }

        [HttpPut("deactivate/{userId:guid}")]
        public async Task<IActionResult> DeactivateProductsByUserId(Guid userId)
        {
            await _service.SetProductsDeletionStatusAsync(userId, true);
            return NoContent();
        }

        [HttpPut("activate/{userId:guid}")]
        public async Task<IActionResult> ActivateProductsByUserId(Guid userId)
        {
            await _service.SetProductsDeletionStatusAsync(userId, false);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchCriteria criteria)
        {
            var products = await _service.SearchProductsAsync(criteria);
            return Ok(products);
        }
    }
}
