using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Commands;
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
        private readonly IMediator _mediator;

        public ProductsController(IProductService service, IMediator mediator)
        {
            _service = service;
            _mediator = mediator;
        }

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

            var cmd = new CreateProductCommand(
                dto.Name,
                dto.Description,
                dto.Price,
                dto.IsAvailable,
                userId
            );

            Guid newId = await _mediator.Send(cmd);

            var createdProduct = await _service.GetProductByIdAsync(newId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = newId },
                createdProduct
            );
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
                return Forbid("You are not allowed to modify this product.");

            await _mediator.Send(new UpdateProductCommand(
                id,
                dto.Name,
                dto.Description,
                dto.Price,
                dto.IsAvailable,
                userId
            ));

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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var currentUserId))
                return Unauthorized();

            if (currentUserId != userId)
                return Forbid();

            await _service.SetProductsDeletionStatusAsync(userId, true);
            return NoContent();
        }

        [HttpPut("activate/{userId:guid}")]
        public async Task<IActionResult> ActivateProductsByUserId(Guid userId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var currentUserId))
                return Unauthorized();

            if (currentUserId != userId)
                return Forbid();

            await _service.SetProductsDeletionStatusAsync(userId, false);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchCriteria criteria)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var products = await _service.SearchProductsByUserAsync(userId, criteria);
            return Ok(products);
        }
    }
}
