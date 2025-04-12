using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public class RegisterModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
