using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Interfaces;
using ProductService.Application.Models;
using ProductService.Domain.Entities;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        
        public ProductsController(IProductService service)
        {
            _service = service;
        }
        
        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllProductsAsync();
            return Ok(products);
        }
        
        // GET: api/products/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
        }
        
        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            // Дополнительная валидация может быть добавлена здесь
            await _service.AddProductAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        
        // PUT: api/products/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Product product)
        {
            if (id != product.Id)
                return BadRequest("ID mismatch");

            await _service.UpdateProductAsync(product);
            return NoContent();
        }
        
        // DELETE: api/products/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteProductAsync(id);
            return NoContent();
        }

        [HttpPut("deactivate/{userId}")]
        public async Task<IActionResult> DeactivateProductsByUserId(Guid userId)
        {
            await _service.SetProductsDeletionStatusAsync(userId, true);
            return NoContent();
        }
        
        // PUT: api/products/activate/{userId}
        [HttpPut("activate/{userId}")]
        public async Task<IActionResult> ActivateProductsByUserId(Guid userId)
        {
            await _service.SetProductsDeletionStatusAsync(userId, false);
            return NoContent();
        }

        // GET: api/products/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchCriteria criteria)
        {
            var products = await _service.SearchProductsAsync(criteria);
            return Ok(products);
        }
    }
}
