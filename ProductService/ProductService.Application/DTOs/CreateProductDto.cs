using System.ComponentModel.DataAnnotations;

namespace ProductService.Application.DTOs
{
    public class CreateProductDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public bool IsAvailable { get; set; }
    }
}